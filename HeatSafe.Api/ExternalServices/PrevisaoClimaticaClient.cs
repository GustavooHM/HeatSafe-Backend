using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HeatSafe.Api.Configuration;
using HeatSafe.Api.Domain;
using HeatSafe.Api.Services;
using Microsoft.Extensions.Options;

namespace HeatSafe.Api.ExternalServices;

public sealed class PrevisaoClimaticaClient(HttpClient httpClient, ILogger<PrevisaoClimaticaClient> logger) : IPrevisaoClimaticaClient
{
    public async Task<PrevisaoClimaticaResposta?> ConsultarAmanhaAsync(string estacao, CancellationToken cancellationToken)
    {
        try
        {
            var path = $"previsao/amanha?estacao={Uri.EscapeDataString(estacao)}";
            var response = await httpClient.GetFromJsonAsync<ForecastResponse>(path, cancellationToken);
            if (response is null || !DateTime.TryParse(response.Data, out var date) ||
                !double.IsFinite(response.Probabilidade) || response.Probabilidade is < 0 or > 1)
                return null;
            return new PrevisaoClimaticaResposta(date.Date, response.Perigoso, response.Probabilidade);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Serviço de previsão indisponível. Nenhuma previsão foi presumida.");
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timeout ao consultar serviço de previsão climática.");
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Resposta inválida recebida do serviço de previsão.");
            return null;
        }
    }

    private sealed class ForecastResponse
    {
        [JsonPropertyName("data")] public string Data { get; set; } = string.Empty;
        [JsonPropertyName("perigoso")] public bool Perigoso { get; set; }
        [JsonPropertyName("probabilidade")] public double Probabilidade { get; set; }
    }
}
