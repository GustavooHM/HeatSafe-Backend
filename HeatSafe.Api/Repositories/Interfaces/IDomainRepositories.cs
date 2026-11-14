using HeatSafe.Api.Domain;

namespace HeatSafe.Api.Repositories.Interfaces;

public interface IIdosoRepository
{
    Task<Idoso?> ObterAtivoAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Idoso>> ListarAtivosAsync(CancellationToken cancellationToken);
}

public interface IResidenciaRepository
{
    Task<Residencia?> ObterAtivaComIdosoAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Residencia>> ListarAtivasAsync(CancellationToken cancellationToken);
}

public interface ILeituraRepository
{
    Task<Leitura?> ObterUltimaPorResidenciaAsync(int residenciaId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Leitura>> ListarPorDispositivoAsync(int dispositivoId, int limite, CancellationToken cancellationToken);
}

public interface IAlertaRepository
{
    Task<IReadOnlyList<Alerta>> ListarAbertosPorIdosoAsync(int idosoId, CancellationToken cancellationToken);
}

public interface IVisitaRepository
{
    Task<IReadOnlyList<Visita>> ListarPendentesPorPrioridadeAsync(CancellationToken cancellationToken);
}

public interface IRegistroHidratacaoRepository
{
    Task<RegistroHidratacao?> ObterUltimoPorIdosoAsync(int idosoId, CancellationToken cancellationToken);
}

public interface IDispositivoRepository
{
    Task<Dispositivo?> ObterAtivoPorCodigoAsync(string codigo, CancellationToken cancellationToken);
}

public interface IFamiliarRepository
{
    Task<Familiar?> ObterAtivoComVinculosAsync(int id, CancellationToken cancellationToken);
}

public interface IAgenteSaudeRepository
{
    Task<AgenteSaude?> ObterAtivoAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgenteSaude>> ListarAtivosAsync(CancellationToken cancellationToken);
}
