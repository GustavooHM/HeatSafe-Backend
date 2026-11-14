using HeatSafe.Api.Configuration;
using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using HeatSafe.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HeatSafe.Api.BackgroundServices;

public sealed class ForecastBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ForecastOptions> forecastOptions,
    IOptions<MonitoringOptions> monitoringOptions,
    TimeProvider timeProvider,
    ILogger<ForecastBackgroundService> logger) : BackgroundService
{
    private DateTime? _lastProcessedLocalDate;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var zone = DateTimeNormalizer.GetSaoPauloTimeZone();
                var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zone).DateTime;
                var localDate = localNow.Date;
                if (localNow.Hour >= monitoringOptions.Value.ForecastHour && _lastProcessedLocalDate != localDate)
                {
                    await ProcessForecastAsync(localDate, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Erro na rotina diária de previsão climática."); }

            try { if (!await timer.WaitForNextTickAsync(stoppingToken)) break; }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private async Task ProcessForecastAsync(DateTime todayLocal, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IPrevisaoClimaticaClient>();
        var db = scope.ServiceProvider.GetRequiredService<HeatSafeDbContext>();
        var forecast = await client.ConsultarAmanhaAsync(forecastOptions.Value.Station, cancellationToken);
        if (forecast is null) return; // Não desliga vigilância nem inventa dados em caso de falha.

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existing = await db.PrevisoesClimaticas.FirstOrDefaultAsync(x =>
            x.Estacao == forecastOptions.Value.Station && x.DataPrevista == forecast.Data.Date, cancellationToken);
        if (existing is null)
        {
            db.PrevisoesClimaticas.Add(new PrevisaoClimatica
            {
                Estacao = forecastOptions.Value.Station, DataPrevista = forecast.Data.Date,
                Perigoso = forecast.Perigoso, Probabilidade = forecast.Probabilidade, ConsultadaEmUtc = now
            });
        }
        else
        {
            existing.Perigoso = forecast.Perigoso;
            existing.Probabilidade = forecast.Probabilidade;
            existing.ConsultadaEmUtc = now;
        }

        var nextLocalDay = todayLocal.AddDays(1);
        var localEnd = nextLocalDay.AddDays(1).AddTicks(-1);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified), DateTimeNormalizer.GetSaoPauloTimeZone());
        var residences = await db.Residencias.Where(x => x.Ativo).ToListAsync(cancellationToken);
        foreach (var residence in residences)
            residence.VigilanciaAteUtc = forecast.Perigoso ? endUtc : null;

        await db.SaveChangesAsync(cancellationToken);
        _lastProcessedLocalDate = todayLocal.Date;
        logger.LogInformation("Previsão A701 registrada para {Data}: perigoso={Perigoso}, probabilidade={Probabilidade:P0}",
            forecast.Data.ToString("yyyy-MM-dd"), forecast.Perigoso, forecast.Probabilidade);
    }
}
