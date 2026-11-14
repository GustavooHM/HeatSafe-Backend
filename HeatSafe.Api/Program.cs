using System.Text;
using System.Text.Json.Serialization;
using HeatSafe.Api.BackgroundServices;
using HeatSafe.Api.Configuration;
using HeatSafe.Api.Data;
using HeatSafe.Api.Domain;
using HeatSafe.Api.ExternalServices;
using HeatSafe.Api.Security;
using HeatSafe.Api.Repositories;
using HeatSafe.Api.Repositories.Interfaces;
using HeatSafe.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HeatSafe API",
        Version = "v1",
        Description = "Backend da CitizenTech HeatSafe — monitoramento de calor residencial, alertas e priorização de visitas."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT obtido em POST /auth/token."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var connectionString = builder.Configuration.GetConnectionString("HeatSafe") ?? "Data Source=heatsafe.db";
builder.Services.AddDbContext<HeatSafeDbContext>(options => options.UseSqlite(connectionString));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<ForecastOptions>(builder.Configuration.GetSection("Forecast"));
builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection("Monitoring"));
builder.Services.Configure<PriorityWeights>(builder.Configuration.GetSection("PriorityWeights"));
if (builder.Environment.IsDevelopment())
    builder.Services.Configure<DemoAuthOptions>(builder.Configuration.GetSection("DemoAuth"));
else
    builder.Services.Configure<DemoAuthOptions>(_ => { });

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Configure Jwt:SigningKey com pelo menos 32 bytes por variável de ambiente/secret manager.");
    jwtOptions.SigningKey = "HEATSAFE-DEV-ONLY-CHANGE-THIS-KEY-32-BYTES-MINIMUM";
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICalculadoraIndiceCalor, CalculadoraIndiceCalor>();
builder.Services.AddScoped<IAvaliadorRisco, AvaliadorRisco>();
builder.Services.AddScoped<INotificador, LoggingNotificationService>();
builder.Services.AddScoped<PriorityScoringService>();
builder.Services.AddScoped<RiskMonitoringService>();
builder.Services.AddScoped<DeviceIntegrationService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IIdosoRepository, IdosoRepository>();
builder.Services.AddScoped<IResidenciaRepository, ResidenciaRepository>();
builder.Services.AddScoped<ILeituraRepository, LeituraRepository>();
builder.Services.AddScoped<IAlertaRepository, AlertaRepository>();
builder.Services.AddScoped<IVisitaRepository, VisitaRepository>();
builder.Services.AddScoped<IRegistroHidratacaoRepository, RegistroHidratacaoRepository>();
builder.Services.AddScoped<IDispositivoRepository, DispositivoRepository>();
builder.Services.AddScoped<IFamiliarRepository, FamiliarRepository>();
builder.Services.AddScoped<IAgenteSaudeRepository, AgenteSaudeRepository>();

builder.Services.AddHttpClient<IPrevisaoClimaticaClient, PrevisaoClimaticaClient>((services, client) =>
{
    var forecast = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ForecastOptions>>().Value;
    client.BaseAddress = new Uri(forecast.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(forecast.TimeoutSeconds, 1, 60));
});

builder.Services.AddHostedService<RiskMonitoringBackgroundService>();
builder.Services.AddHostedService<ForecastBackgroundService>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "HeatSafe API v1"));
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", application = "HeatSafe.Api", utc = DateTime.UtcNow }))
    .AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HeatSafeDbContext>();
    await db.Database.MigrateAsync();
    var calculator = scope.ServiceProvider.GetRequiredService<ICalculadoraIndiceCalor>();
    var evaluator = scope.ServiceProvider.GetRequiredService<IAvaliadorRisco>();
    await SeedData.InitializeAsync(db, calculator, evaluator);
}

await app.RunAsync();

public partial class Program { }
