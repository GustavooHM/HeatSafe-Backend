using HeatSafe.Api.Domain;
using HeatSafe.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HeatSafe.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(HeatSafeDbContext db, ICalculadoraIndiceCalor calculator, IAvaliadorRisco riskEvaluator, CancellationToken cancellationToken = default)
    {
        if (await db.Dispositivos.AnyAsync(cancellationToken)) return;

        var now = DateTime.UtcNow;
        var idoso = new Idoso
        {
            Id = 1, Nome = "Dona Maria (demonstração)", Idade = 78,
            PossuiVulnerabilidade = true,
            DescricaoVulnerabilidade = "Cadastro fictício para demonstração; não representa pessoa real.",
            DataCadastroUtc = now.AddDays(-30), Ativo = true
        };
        var familiar = new Familiar
        {
            Id = 2, Nome = "Ana Paula (demonstração)", ContatoMascarado = "contato fictício",
            DataCadastroUtc = now.AddDays(-30), Ativo = true
        };
        var agente = new AgenteSaude
        {
            Id = 3, Nome = "Rosa (demonstração)", UnidadeSaude = "UBS Demonstrativa",
            DataCadastroUtc = now.AddDays(-30), Ativo = true
        };
        db.Pessoas.AddRange(idoso, familiar, agente);
        db.FamiliaresIdosos.Add(new FamiliarIdoso { Familiar = familiar, Idoso = idoso });

        var residencia = new Residencia
        {
            Id = 1, Bairro = "Bairro fictício", Cidade = "São Paulo", Estado = "SP",
            IdentificacaoEndereco = "Residência demonstrativa 001 (endereço fictício)",
            Idoso = idoso, UltimaMovimentacaoUtc = now.AddMinutes(-30),
            UltimaHidratacaoUtc = now.AddMinutes(-45), Ativo = true
        };
        var dispositivo = new Dispositivo
        {
            Id = 1, Codigo = "HS-001", Residencia = residencia, Ativo = true,
            UltimaComunicacaoUtc = now
        };
        db.Residencias.Add(residencia);
        db.Dispositivos.Add(dispositivo);

        var heat = calculator.CalcularCelsius(29.0, 55.0);
        db.Leituras.Add(new Leitura
        {
            Dispositivo = dispositivo, TemperaturaC = 29.0, Umidade = 55.0,
            IndiceCalorInformadoC = heat, IndiceCalorCalculadoC = heat,
            NivelRisco = riskEvaluator.Classificar(heat), MovimentoDetectado = true,
            DataHoraUtc = now.AddMinutes(-5), RecebidoEmUtc = now
        });
        db.RegistrosHidratacao.Add(new RegistroHidratacao
        {
            Idoso = idoso, Dispositivo = dispositivo, DataHoraUtc = now.AddMinutes(-45)
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
