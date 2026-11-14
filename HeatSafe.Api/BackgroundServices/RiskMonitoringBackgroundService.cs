using HeatSafe.Api.Configuration;
using HeatSafe.Api.Services;
using Microsoft.Extensions.Options;

namespace HeatSafe.Api.BackgroundServices;

public sealed class RiskMonitoringBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options,
    ILogger<RiskMonitoringBackgroundService> logger) : BackgroundService
{
    private readonly int _interval = Math.Clamp(options.Value.IntervalSeconds, 10, 3600);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_interval));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<RiskMonitoringService>();
                await service.ProcessAllActiveResidencesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha em uma execução do monitoramento HeatSafe. A próxima execução será tentada normalmente.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
