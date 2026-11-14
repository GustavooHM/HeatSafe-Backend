using HeatSafe.Api.Configuration;
using HeatSafe.Api.Domain;
using Microsoft.Extensions.Options;

namespace HeatSafe.Api.Services;

public sealed record PriorityResult(double Score, string Reason);

public sealed class PriorityScoringService(IOptions<PriorityWeights> options)
{
    private readonly PriorityWeights _weights = options.Value;

    public PriorityResult Calculate(double heatIndexC, DateTime? lastMovementUtc, DateTime? lastHydrationUtc,
        int age, bool vulnerable, bool hasUnconfirmedAlert, DateTime nowUtc)
    {
        var weights = new[] { _weights.Heat, _weights.Inactivity, _weights.Hydration, _weights.Vulnerability, _weights.UnconfirmedAlert };
        if (weights.Any(x => !double.IsFinite(x) || x < 0) || Math.Abs(weights.Sum() - 1.0) > 0.0001)
            throw new InvalidOperationException("Os pesos de prioridade devem ser não negativos e somar 1.0.");

        var heat = Clamp((heatIndexC - 27.0) / (54.0 - 27.0) * 100.0);
        var inactivityHours = lastMovementUtc.HasValue ? Math.Max(0, (nowUtc - lastMovementUtc.Value).TotalHours) : 4.0;
        var hydrationHours = lastHydrationUtc.HasValue ? Math.Max(0, (nowUtc - lastHydrationUtc.Value).TotalHours) : 4.0;
        var inactivity = Clamp(inactivityHours / 4.0 * 100.0);
        var hydration = Clamp(hydrationHours / 4.0 * 100.0);
        var vulnerability = vulnerable ? 100.0 : age >= 85 ? 80.0 : age >= 75 ? 60.0 : age >= 65 ? 35.0 : 10.0;
        var unconfirmed = hasUnconfirmedAlert ? 100.0 : 0.0;
        var score = heat * _weights.Heat + inactivity * _weights.Inactivity + hydration * _weights.Hydration
            + vulnerability * _weights.Vulnerability + unconfirmed * _weights.UnconfirmedAlert;

        var reasons = new List<string>();
        if (heat >= 70) reasons.Add("índice de calor elevado");
        if (inactivity >= 50) reasons.Add("período prolongado sem movimentação");
        if (hydration >= 50) reasons.Add("período prolongado sem registro de hidratação");
        if (vulnerable || age >= 75) reasons.Add("vulnerabilidade cadastrada");
        if (hasUnconfirmedAlert) reasons.Add("alerta familiar ainda não confirmado");
        if (reasons.Count == 0) reasons.Add("monitoramento preventivo");

        return new PriorityResult(Math.Round(Clamp(score), 1), string.Join("; ", reasons) + ".");
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);
}
