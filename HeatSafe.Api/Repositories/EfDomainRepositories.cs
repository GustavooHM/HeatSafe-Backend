using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using HeatSafe.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HeatSafe.Api.Repositories;

public sealed class IdosoRepository(HeatSafeDbContext db) : IIdosoRepository
{
    public Task<Idoso?> ObterAtivoAsync(int id, CancellationToken ct) =>
        db.Idosos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
    public async Task<IReadOnlyList<Idoso>> ListarAtivosAsync(CancellationToken ct) =>
        await db.Idosos.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync(ct);
}

public sealed class ResidenciaRepository(HeatSafeDbContext db) : IResidenciaRepository
{
    public Task<Residencia?> ObterAtivaComIdosoAsync(int id, CancellationToken ct) =>
        db.Residencias.AsNoTracking().Include(x => x.Idoso).FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
    public async Task<IReadOnlyList<Residencia>> ListarAtivasAsync(CancellationToken ct) =>
        await db.Residencias.AsNoTracking().Include(x => x.Idoso).Where(x => x.Ativo).OrderBy(x => x.Bairro).ToListAsync(ct);
}

public sealed class LeituraRepository(HeatSafeDbContext db) : ILeituraRepository
{
    public Task<Leitura?> ObterUltimaPorResidenciaAsync(int residenciaId, CancellationToken ct) =>
        db.Leituras.AsNoTracking().Where(x => x.Dispositivo.ResidenciaId == residenciaId)
            .OrderByDescending(x => x.DataHoraUtc).FirstOrDefaultAsync(ct);
    public async Task<IReadOnlyList<Leitura>> ListarPorDispositivoAsync(int dispositivoId, int limite, CancellationToken ct) =>
        await db.Leituras.AsNoTracking().Where(x => x.DispositivoId == dispositivoId)
            .OrderByDescending(x => x.DataHoraUtc).Take(Math.Clamp(limite, 1, 500)).ToListAsync(ct);
}

public sealed class AlertaRepository(HeatSafeDbContext db) : IAlertaRepository
{
    public async Task<IReadOnlyList<Alerta>> ListarAbertosPorIdosoAsync(int idosoId, CancellationToken ct) =>
        await db.Alertas.AsNoTracking().Where(x => x.IdosoId == idosoId &&
            (x.Status == StatusAlerta.ATIVO || x.Status == StatusAlerta.ESCALONADO))
            .OrderByDescending(x => x.CriadoEmUtc).ToListAsync(ct);
}

public sealed class VisitaRepository(HeatSafeDbContext db) : IVisitaRepository
{
    public async Task<IReadOnlyList<Visita>> ListarPendentesPorPrioridadeAsync(CancellationToken ct) =>
        await db.Visitas.AsNoTracking().Include(x => x.Idoso).Include(x => x.Residencia)
            .Where(x => x.Status == StatusVisita.PENDENTE || x.Status == StatusVisita.EM_ANDAMENTO)
            .OrderByDescending(x => x.PontuacaoPrioridade).ToListAsync(ct);
}

public sealed class RegistroHidratacaoRepository(HeatSafeDbContext db) : IRegistroHidratacaoRepository
{
    public Task<RegistroHidratacao?> ObterUltimoPorIdosoAsync(int idosoId, CancellationToken ct) =>
        db.RegistrosHidratacao.AsNoTracking().Where(x => x.IdosoId == idosoId)
            .OrderByDescending(x => x.DataHoraUtc).FirstOrDefaultAsync(ct);
}

public sealed class DispositivoRepository(HeatSafeDbContext db) : IDispositivoRepository
{
    public Task<Dispositivo?> ObterAtivoPorCodigoAsync(string codigo, CancellationToken ct) =>
        db.Dispositivos.AsNoTracking().Include(x => x.Residencia)
            .FirstOrDefaultAsync(x => x.Codigo == codigo && x.Ativo && x.Residencia.Ativo, ct);
}

public sealed class FamiliarRepository(HeatSafeDbContext db) : IFamiliarRepository
{
    public Task<Familiar?> ObterAtivoComVinculosAsync(int id, CancellationToken ct) =>
        db.Familiares.AsNoTracking().Include(x => x.IdososVinculados)
            .FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
}

public sealed class AgenteSaudeRepository(HeatSafeDbContext db) : IAgenteSaudeRepository
{
    public Task<AgenteSaude?> ObterAtivoAsync(int id, CancellationToken ct) =>
        db.AgentesSaude.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Ativo, ct);
    public async Task<IReadOnlyList<AgenteSaude>> ListarAtivosAsync(CancellationToken ct) =>
        await db.AgentesSaude.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync(ct);
}
