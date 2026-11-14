namespace HeatSafe.Api.Domain;

public interface ICalculadoraIndiceCalor
{
    double CalcularCelsius(double temperaturaC, double umidade);
}

public interface IAvaliadorRisco
{
    NivelRisco Classificar(double indiceCalorC);
}

public interface INotificador
{
    Task NotificarFamiliaresAsync(int alertaId, string mensagem, CancellationToken cancellationToken);
    Task NotificarUbsAsync(int alertaId, string mensagem, CancellationToken cancellationToken);
}

public interface IPrevisaoClimaticaClient
{
    Task<PrevisaoClimaticaResposta?> ConsultarAmanhaAsync(string estacao, CancellationToken cancellationToken);
}

public sealed record PrevisaoClimaticaResposta(DateTime Data, bool Perigoso, double Probabilidade);
