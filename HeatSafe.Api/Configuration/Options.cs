namespace HeatSafe.Api.Configuration;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "HeatSafe";
    public string Audience { get; set; } = "HeatSafe.Clients";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class ForecastOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8080/";
    public string Station { get; set; } = "A701";
    public int TimeoutSeconds { get; set; } = 5;
}

/// <summary>Chaves de API dos dispositivos ESP32 (cabeçalho X-Api-Key). Chave do dicionário = código do dispositivo.</summary>
public sealed class DeviceAuthOptions
{
    public Dictionary<string, string> ApiKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MonitoringOptions
{
    public int IntervalSeconds { get; set; } = 60;
    public int DangerConfirmationMinutes { get; set; } = 20;
    public int ForecastHour { get; set; } = 19;
}

public sealed class PriorityWeights
{
    public double Heat { get; set; } = 0.40;
    public double Inactivity { get; set; } = 0.25;
    public double Hydration { get; set; } = 0.15;
    public double Vulnerability { get; set; } = 0.10;
    public double UnconfirmedAlert { get; set; } = 0.10;
}

public sealed class DemoAuthOptions
{
    public List<DemoUser> Users { get; set; } = [];
}

public sealed class DemoUser
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string SubjectId { get; set; } = string.Empty;
    public string? DeviceCode { get; set; }
}
