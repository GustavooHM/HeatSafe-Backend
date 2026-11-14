using HeatSafe.Api.Domain;
using Microsoft.Extensions.Logging;

namespace HeatSafe.Api.Services;

/// <summary>Adaptador demonstrativo: registra que a notificação foi solicitada, sem afirmar entrega real.</summary>
public sealed class LoggingNotificationService(ILogger<LoggingNotificationService> logger) : INotificador
{
    public Task NotificarFamiliaresAsync(int alertaId, string mensagem, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEMO-NOTIFICACAO-FAMILIA] Alerta {AlertaId}: {Mensagem}", alertaId, mensagem);
        return Task.CompletedTask;
    }

    public Task NotificarUbsAsync(int alertaId, string mensagem, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEMO-NOTIFICACAO-UBS] Alerta {AlertaId}: {Mensagem}", alertaId, mensagem);
        return Task.CompletedTask;
    }
}
