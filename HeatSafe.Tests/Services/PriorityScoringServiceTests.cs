using HeatSafe.Api.Configuration;
using HeatSafe.Api.Services;
using Microsoft.Extensions.Options;

namespace HeatSafe.Tests.Services;

public sealed class PriorityScoringServiceTests
{
    [Fact]
    public void Calculate_SituacaoMaisGraveDeveReceberPontuacaoMaior()
    {
        var service = new PriorityScoringService(Options.Create(new PriorityWeights()));
        var now = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

        var low = service.Calculate(27.0, now.AddMinutes(-10), now.AddMinutes(-10), 65, false, false, now);
        var high = service.Calculate(52.0, now.AddHours(-4), now.AddHours(-4), 85, true, true, now);

        Assert.InRange(low.Score, 0, 100);
        Assert.InRange(high.Score, 0, 100);
        Assert.True(high.Score > low.Score);
        Assert.Contains("alerta familiar", high.Reason.ToLowerInvariant());
    }

    [Fact]
    public void Calculate_DeveRejeitarPesosQueNaoSomamUm()
    {
        var invalid = new PriorityWeights { Heat = 0.5, Inactivity = 0.5, Hydration = 0.5, Vulnerability = 0, UnconfirmedAlert = 0 };
        var service = new PriorityScoringService(Options.Create(invalid));
        var now = DateTime.UtcNow;
        Assert.Throws<InvalidOperationException>(() => service.Calculate(40, now, now, 70, false, false, now));
    }
}
