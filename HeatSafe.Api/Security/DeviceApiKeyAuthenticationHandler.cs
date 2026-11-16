using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using HeatSafe.Api.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HeatSafe.Api.Security;

/// <summary>
/// Autenticação do ESP32 pelo cabeçalho X-Api-Key, como o firmware do HeatSafe envia.
/// Gera as mesmas claims do token JWT de dispositivo (papel Dispositivo + device_code).
/// </summary>
public sealed class DeviceApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<DeviceAuthOptions> deviceAuth)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DeviceApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values) || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
            return Task.FromResult(AuthenticateResult.NoResult());

        var deviceCode = FindDevice(deviceAuth.CurrentValue.ApiKeys, values.First()!);
        if (deviceCode is null)
            return Task.FromResult(AuthenticateResult.Fail("Chave de dispositivo inválida."));

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, deviceCode),
            new Claim(ClaimTypes.Role, "Dispositivo"),
            new Claim("device_code", deviceCode)
        ], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    /// <summary>Devolve o código do dispositivo dono da chave, comparando em tempo constante.</summary>
    public static string? FindDevice(IReadOnlyDictionary<string, string> apiKeys, string presentedKey)
    {
        var presented = Encoding.UTF8.GetBytes(presentedKey.Trim());
        foreach (var (device, key) in apiKeys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (CryptographicOperations.FixedTimeEquals(presented, Encoding.UTF8.GetBytes(key)))
                return device;
        }
        return null;
    }
}
