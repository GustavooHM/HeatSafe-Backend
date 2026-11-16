using HeatSafe.Api.Security;

namespace HeatSafe.Tests.Security;

public sealed class DeviceApiKeyAuthenticationHandlerTests
{
    private static readonly Dictionary<string, string> Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HS-001"] = "heatsafe-demo-2026",
        ["HS-002"] = "outra-chave-2026",
        ["HS-003"] = ""
    };

    [Theory]
    [InlineData("heatsafe-demo-2026", "HS-001")]
    [InlineData("  outra-chave-2026 ", "HS-002")]
    public void FindDevice_DeveIdentificarODispositivoDaChave(string presented, string expected)
    {
        Assert.Equal(expected, DeviceApiKeyAuthenticationHandler.FindDevice(Keys, presented));
    }

    [Theory]
    [InlineData("chave-errada")]
    [InlineData("HEATSAFE-DEMO-2026")]
    [InlineData("")]
    public void FindDevice_DeveRecusarChaveInvalidaOuVazia(string presented)
    {
        Assert.Null(DeviceApiKeyAuthenticationHandler.FindDevice(Keys, presented));
    }
}
