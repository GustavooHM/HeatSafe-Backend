
using HeatSafe.Api.Configuration;
using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HeatSafe.Api.Services;

public sealed class RiskMonitoringService(
    HeatSafeDbContext db,
    INotificador notifier,
    PriorityScoringService scoring,
    IOptions<MonitoringOptions> monitoringOptions,
    TimeProvider timeProvider,
    ILogger<RiskMonitoringService> logger)
{
    private readonly MonitoringOptions _monitoring = monitoringOptions.Value;

    public async Task ProcessAllActiveResidencesAsync(
        CancellationToken cancellationToken)
    {
        var ids = await db.Residencias
            .AsNoTracking()
            .Where(x => x.Ativo)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessResidenceAsync(id, cancellationToken);
        }

        await EscalateExpiredAlertsAsync(cancellationToken);
        await RefreshPendingVisitScoresAsync(cancellationToken);
    }

    public async Task ProcessResidenceAsync(
        int residenciaId,
        CancellationToken cancellationToken)
    {
        var residence = await db.Residencias
            .Include(x => x.Idoso)
            .FirstOrDefaultAsync(
                x => x.Id == residenciaId && x.Ativo,
                cancellationToken);

        if (residence is null || !residence.Idoso.Ativo)
            return;

        var latest = await db.Leituras
            .AsNoTracking()
            .Where(x => x.Dispositivo.ResidenciaId == residenciaId)
            .OrderByDescending(x => x.DataHoraUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var noMovementHours = residence.UltimaMovimentacaoUtc.HasValue
            ? Math.Max(
                0,
                (now - residence.UltimaMovimentacaoUtc.Value).TotalHours)
            : double.MaxValue;

        var noHydrationHours = residence.UltimaHidratacaoUtc.HasValue
            ? Math.Max(
                0,
                (now - residence.UltimaHidratacaoUtc.Value).TotalHours)
            : double.MaxValue;

        TipoAlerta? candidate = null;
        string reason = string.Empty;
        var immediateUbs = false;

        if (latest.NivelRisco == NivelRisco.PERIGO_EXTREMO)
        {
            candidate = TipoAlerta.PERIGO_EXTREMO;
            reason = "Índice de calor classificado como perigo extremo.";
            immediateUbs = true;
        }
        else if (latest.NivelRisco == NivelRisco.PERIGO &&
                 noMovementHours > 2)
        {
            candidate = TipoAlerta.PERIGO_SEM_MOVIMENTO;

            var elapsed = residence.UltimaMovimentacaoUtc.HasValue
                ? $"{noMovementHours:F1} horas"
                : "sem registro de movimentação";

            reason =
                $"Índice de calor em perigo e ausência de movimentação há {elapsed}.";
        }
        else if (latest.NivelRisco == NivelRisco.CAUTELA_EXTREMA &&
                 noMovementHours > 3)
        {
            candidate = TipoAlerta.CAUTELA_SEM_MOVIMENTO;

            var elapsed = residence.UltimaMovimentacaoUtc.HasValue
                ? $"{noMovementHours:F1} horas"
                : "sem registro de movimentação";

            reason =
                $"Cautela extrema e ausência de movimentação há {elapsed}.";
        }
        else if (latest.NivelRisco == NivelRisco.CAUTELA_EXTREMA &&
                 noHydrationHours > 3)
        {
            candidate = TipoAlerta.CAUTELA_SEM_HIDRATACAO;

            var elapsed = residence.UltimaHidratacaoUtc.HasValue
                ? $"{noHydrationHours:F1} horas"
                : "sem registro de hidratação";

            reason =
                $"Cautela extrema e ausência de registro de hidratação há {elapsed}.";
        }

        if (candidate.HasValue)
        {
            await EnsureAlertAsync(
                residence,
                latest,
                candidate.Value,
                reason,
                immediateUbs,
                now,
                cancellationToken);
        }
        else
        {
            // Encerra alertas não escalonados quando os critérios de risco deixam de existir.
            var recoveredAlerts = await db.Alertas
                .Where(x =>
                    x.IdosoId == residence.IdosoId &&
                    (x.Status == StatusAlerta.ATIVO ||
                     x.Status == StatusAlerta.CONFIRMADO))
                .ToListAsync(cancellationToken);

            foreach (var alert in recoveredAlerts)
            {
                alert.Status = StatusAlerta.RESOLVIDO;
                alert.ResolvidoEmUtc = now;
            }

            if (recoveredAlerts.Count > 0)
                await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ForceAlarmWithoutResponseAsync(
        int residenciaId,
        DateTime eventTimeUtc,
        CancellationToken cancellationToken)
    {
        var residence = await db.Residencias
            .Include(x => x.Idoso)
            .FirstOrDefaultAsync(
                x => x.Id == residenciaId && x.Ativo,
                cancellationToken);

        if (residence is null)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var latest = await db.Leituras
            .AsNoTracking()
            .Where(x => x.Dispositivo.ResidenciaId == residenciaId)
            .OrderByDescending(x => x.DataHoraUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var level = latest?.NivelRisco ?? NivelRisco.PERIGO;
        var heat = latest?.IndiceCalorCalculadoC ?? 41.0;

        var existing = await db.Alertas
            .FirstOrDefaultAsync(
                x => x.IdosoId == residence.IdosoId &&
                     (x.Status == StatusAlerta.ATIVO ||
                      x.Status == StatusAlerta.ESCALONADO),
                cancellationToken);

        if (existing is not null)
        {
            await EscalateAsync(
                existing,
                now,
                "Evento ALARME_SEM_RESPOSTA recebido do dispositivo.",
                cancellationToken);

            return;
        }

        var alert = new Alerta
        {
            IdosoId = residence.IdosoId,
            ResidenciaId = residence.Id,
            IndiceCalorC = heat,
            NivelRisco = level,
            Tipo = TipoAlerta.ALARME_SEM_RESPOSTA,
            Status = StatusAlerta.ATIVO,
            CriadoEmUtc = eventTimeUtc,
            Motivo = "O dispositivo registrou o evento ALARME_SEM_RESPOSTA."
        };

        db.Alertas.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        await notifier.NotificarFamiliaresAsync(
            alert.Id,
            alert.Motivo,
            cancellationToken);

        await EscalateAsync(
            alert,
            now,
            alert.Motivo,
            cancellationToken);
    }

    private async Task EnsureAlertAsync(
        Residencia residence,
        Leitura latest,
        TipoAlerta type,
        string reason,
        bool immediateUbs,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existingOpen = await db.Alertas
            .Where(x =>
                x.IdosoId == residence.IdosoId &&
                (x.Status == StatusAlerta.ATIVO ||
                 x.Status == StatusAlerta.ESCALONADO ||
                 x.Status == StatusAlerta.CONFIRMADO))
            .OrderByDescending(x => x.CriadoEmUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingOpen is not null)
        {
            // Se a família confirmou um alerta anterior, mas o risco evoluiu
            // para PERIGO_EXTREMO, cria-se uma nova ocorrência.
            var needsNewExtremeOccurrence =
                immediateUbs &&
                existingOpen.Status == StatusAlerta.CONFIRMADO &&
                existingOpen.NivelRisco != NivelRisco.PERIGO_EXTREMO;

            if (!needsNewExtremeOccurrence)
            {
                if (immediateUbs &&
                    existingOpen.Status != StatusAlerta.ESCALONADO &&
                    existingOpen.Status != StatusAlerta.CONFIRMADO)
                {
                    await EscalateAsync(
                        existingOpen,
                        now,
                        reason,
                        cancellationToken);
                }

                return;
            }
        }

        var alert = new Alerta
        {
            IdosoId = residence.IdosoId,
            ResidenciaId = residence.Id,
            IndiceCalorC = latest.IndiceCalorCalculadoC,
            NivelRisco = latest.NivelRisco,
            Tipo = type,
            Status = StatusAlerta.ATIVO,
            CriadoEmUtc = now,
            Motivo = reason
        };

        db.Alertas.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        await notifier.NotificarFamiliaresAsync(
            alert.Id,
            reason,
            cancellationToken);

        logger.LogWarning(
            "Alerta {AlertaId} criado para residência {ResidenciaId}: {Tipo}",
            alert.Id,
            residence.Id,
            type);

        if (immediateUbs)
        {
            await EscalateAsync(
                alert,
                now,
                reason,
                cancellationToken);
        }
    }

    public async Task EscalateExpiredAlertsAsync(
        CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime
            .AddMinutes(-_monitoring.DangerConfirmationMinutes);

        var expired = await db.Alertas
            .Where(x =>
                x.Status == StatusAlerta.ATIVO &&
                x.NivelRisco == NivelRisco.PERIGO &&
                x.Tipo == TipoAlerta.PERIGO_SEM_MOVIMENTO &&
                x.CriadoEmUtc <= cutoff)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var alert in expired)
        {
            await EscalateAsync(
                alert,
                now,
                "Alerta não confirmado pela família dentro de 20 minutos.",
                cancellationToken);
        }
    }

    private async Task EscalateAsync(
        Alerta alert,
        DateTime now,
        string reason,
        CancellationToken cancellationToken)
    {
        if (alert.Status == StatusAlerta.CONFIRMADO ||
            alert.Status == StatusAlerta.RESOLVIDO)
        {
            return;
        }

        var wasAlreadyEscalated =
            alert.Status == StatusAlerta.ESCALONADO;

        if (!wasAlreadyEscalated)
        {
            alert.Status = StatusAlerta.ESCALONADO;
            alert.EscalonadoEmUtc = now;

            await db.SaveChangesAsync(cancellationToken);
        }

        var visitExists = await db.Visitas
            .AnyAsync(
                x => x.AlertaId == alert.Id,
                cancellationToken);

        if (!visitExists)
        {
            var residence = await db.Residencias
                .Include(x => x.Idoso)
                .FirstAsync(
                    x => x.Id == alert.ResidenciaId,
                    cancellationToken);

            var agentId = await db.AgentesSaude
                .Where(x => x.Ativo)
                .OrderBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var hasUnconfirmed =
                alert.Status == StatusAlerta.ESCALONADO;

            var score = scoring.Calculate(
                alert.IndiceCalorC,
                residence.UltimaMovimentacaoUtc,
                residence.UltimaHidratacaoUtc,
                residence.Idoso.Idade,
                residence.Idoso.PossuiVulnerabilidade,
                hasUnconfirmed,
                now);

            db.Visitas.Add(new Visita
            {
                IdosoId = alert.IdosoId,
                ResidenciaId = alert.ResidenciaId,
                AgenteSaudeId = agentId,
                AlertaId = alert.Id,
                PontuacaoPrioridade = score.Score,
                MotivoPrioridade = string.IsNullOrWhiteSpace(reason)
                    ? score.Reason
                    : $"{reason} {score.Reason}",
                Status = StatusVisita.PENDENTE,
                CriadaEmUtc = now
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        if (!wasAlreadyEscalated)
        {
            await notifier.NotificarUbsAsync(
                alert.Id,
                reason,
                cancellationToken);
        }

        logger.LogWarning(
            "Alerta {AlertaId} escalonado para a UBS",
            alert.Id);
    }

    private async Task RefreshPendingVisitScoresAsync(
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var visits = await db.Visitas
            .Include(x => x.Idoso)
            .Include(x => x.Residencia)
            .Where(x => x.Status == StatusVisita.PENDENTE)
            .ToListAsync(cancellationToken);

        foreach (var visit in visits)
        {
            var alertUnconfirmed = await db.Alertas
                .AnyAsync(
                    x => x.Id == visit.AlertaId &&
                         x.Status == StatusAlerta.ESCALONADO,
                    cancellationToken);

            var latest = await db.Leituras
                .AsNoTracking()
                .Where(x =>
                    x.Dispositivo.ResidenciaId == visit.ResidenciaId)
                .OrderByDescending(x => x.DataHoraUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is null)
                continue;

            var result = scoring.Calculate(
                latest.IndiceCalorCalculadoC,
                visit.Residencia.UltimaMovimentacaoUtc,
                visit.Residencia.UltimaHidratacaoUtc,
                visit.Idoso.Idade,
                visit.Idoso.PossuiVulnerabilidade,
                alertUnconfirmed,
                now);

            visit.PontuacaoPrioridade = result.Score;
            visit.MotivoPrioridade = result.Reason;
            visit.AtualizadaEmUtc = now;
        }

        if (visits.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
