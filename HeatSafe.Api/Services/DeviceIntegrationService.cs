using System.Security.Cryptography;
using System.Text;
using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HeatSafe.Api.Services;

public sealed record LeituraProcessada(int Id, double IndiceCalorCalculado, double? IndiceCalorInformado,
    NivelRisco Nivel, string? DivergenciaNivelDispositivo);

public sealed class DeviceIntegrationService(
    HeatSafeDbContext db,
    ICalculadoraIndiceCalor calculator,
    IAvaliadorRisco riskEvaluator,
    RiskMonitoringService monitoring,
    TimeProvider timeProvider)
{
    public async Task<LeituraProcessada> RegistrarLeituraAsync(string codigoDispositivo,
        double temperaturaC, double umidade, double? indiceCalorInformadoC, string? nivelInformado,
        bool movimento, string dataHora, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoDispositivo)) throw new ArgumentException("Dispositivo obrigatório.");
        if (temperaturaC is < -20 or > 60 || !double.IsFinite(temperaturaC)) throw new ArgumentOutOfRangeException(nameof(temperaturaC));
        if (umidade is < 0 or > 100 || !double.IsFinite(umidade)) throw new ArgumentOutOfRangeException(nameof(umidade));
        if (indiceCalorInformadoC.HasValue && !double.IsFinite(indiceCalorInformadoC.Value)) throw new ArgumentOutOfRangeException(nameof(indiceCalorInformadoC));
        if (!DateTimeNormalizer.TryToUtc(dataHora, out var eventUtc)) throw new ArgumentException("DataHora inválida. Use ISO 8601.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (eventUtc > now.AddMinutes(5)) throw new ArgumentException("DataHora não pode estar mais de 5 minutos no futuro.");

        var device = await db.Dispositivos.Include(x => x.Residencia)
            .FirstOrDefaultAsync(x => x.Codigo == codigoDispositivo && x.Ativo && x.Residencia.Ativo, cancellationToken);
        if (device is null) throw new KeyNotFoundException("Dispositivo não encontrado ou inativo.");

        var calculated = calculator.CalcularCelsius(temperaturaC, umidade);
        var level = riskEvaluator.Classificar(calculated);
        var reading = new Leitura
        {
            DispositivoId = device.Id, TemperaturaC = temperaturaC, Umidade = umidade,
            IndiceCalorInformadoC = indiceCalorInformadoC, NivelInformado = string.IsNullOrWhiteSpace(nivelInformado) ? null : nivelInformado.Trim().ToUpperInvariant(),
            IndiceCalorCalculadoC = calculated, NivelRisco = level, MovimentoDetectado = movimento,
            DataHoraUtc = eventUtc, RecebidoEmUtc = now
        };
        db.Leituras.Add(reading);
        device.UltimaComunicacaoUtc = now;
        if (movimento && (!device.Residencia.UltimaMovimentacaoUtc.HasValue || eventUtc > device.Residencia.UltimaMovimentacaoUtc.Value))
            device.Residencia.UltimaMovimentacaoUtc = eventUtc;
        await db.SaveChangesAsync(cancellationToken);
        await monitoring.ProcessResidenceAsync(device.ResidenciaId, cancellationToken);

        string? discrepancy = null;
        if (indiceCalorInformadoC.HasValue && Math.Abs(indiceCalorInformadoC.Value - calculated) > 3.0)
            discrepancy = "O índice informado pelo dispositivo difere mais de 3 °C do cálculo do servidor.";
        if (!string.IsNullOrWhiteSpace(nivelInformado) && !string.Equals(nivelInformado.Trim(), level.ToString(), StringComparison.OrdinalIgnoreCase))
            discrepancy = string.IsNullOrWhiteSpace(discrepancy) ? "A classificação informada pelo dispositivo difere da classificação do servidor." : discrepancy + " A classificação informada também diverge.";

        return new LeituraProcessada(reading.Id, calculated, indiceCalorInformadoC, level, discrepancy);
    }

    public async Task<bool> RegistrarEventoAsync(string codigoDispositivo, string tipo, string dataHora,
        string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TipoEvento>(tipo?.Trim(), true, out var parsedType))
            throw new ArgumentException("Tipo de evento inválido. Valores aceitos: AGUA, MOVIMENTO, ALARME_SEM_RESPOSTA.");
        if (!DateTimeNormalizer.TryToUtc(dataHora, out var eventUtc)) throw new ArgumentException("DataHora inválida. Use ISO 8601.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (eventUtc > now.AddMinutes(5)) throw new ArgumentException("DataHora não pode estar mais de 5 minutos no futuro.");

        var device = await db.Dispositivos.Include(x => x.Residencia)
            .FirstOrDefaultAsync(x => x.Codigo == codigoDispositivo && x.Ativo && x.Residencia.Ativo, cancellationToken);
        if (device is null) throw new KeyNotFoundException("Dispositivo não encontrado ou inativo.");

        var rawKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? $"{codigoDispositivo}|{parsedType}|{eventUtc:O}"
            : $"{codigoDispositivo}|{idempotencyKey.Trim()}";
        var uniqueKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        if (await db.EventosDispositivo.AnyAsync(x => x.ChaveUnica == uniqueKey, cancellationToken)) return false;

        db.EventosDispositivo.Add(new EventoDispositivo
        {
            DispositivoId = device.Id, Tipo = parsedType, DataHoraUtc = eventUtc, ChaveUnica = uniqueKey
        });
        device.UltimaComunicacaoUtc = now;
        switch (parsedType)
        {
            case TipoEvento.AGUA:
                db.RegistrosHidratacao.Add(new RegistroHidratacao
                {
                    IdosoId = device.Residencia.IdosoId, DispositivoId = device.Id, DataHoraUtc = eventUtc
                });
                if (!device.Residencia.UltimaHidratacaoUtc.HasValue || eventUtc > device.Residencia.UltimaHidratacaoUtc.Value)
                    device.Residencia.UltimaHidratacaoUtc = eventUtc;
                break;
            case TipoEvento.MOVIMENTO:
                if (!device.Residencia.UltimaMovimentacaoUtc.HasValue || eventUtc > device.Residencia.UltimaMovimentacaoUtc.Value)
                    device.Residencia.UltimaMovimentacaoUtc = eventUtc;
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        if (parsedType == TipoEvento.ALARME_SEM_RESPOSTA)
            await monitoring.ForceAlarmWithoutResponseAsync(device.ResidenciaId, eventUtc, cancellationToken);
        else
            await monitoring.ProcessResidenceAsync(device.ResidenciaId, cancellationToken);
        return true;
    }
}
