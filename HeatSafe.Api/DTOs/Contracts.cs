using System.ComponentModel.DataAnnotations;
using HeatSafe.Api.Domain;

namespace HeatSafe.Api.DTOs;

public sealed class TokenRequest
{
    [Required, StringLength(80, MinimumLength = 3)] public string Username { get; set; } = string.Empty;
    [Required, StringLength(120, MinimumLength = 6)] public string Password { get; set; } = string.Empty;
}
public sealed record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, string Role);

public sealed class LeituraRequest
{
    [Required, StringLength(40)] public string DispositivoId { get; set; } = string.Empty;
    [Range(-20, 60)] public double Temperatura { get; set; }
    [Range(0, 100)] public double Umidade { get; set; }
    public double? IndiceCalor { get; set; }
    [StringLength(30)] public string? Nivel { get; set; }
    public bool Movimento { get; set; }
    [Required] public string DataHora { get; set; } = string.Empty;
}
public sealed class EventoRequest
{
    [Required, StringLength(40)] public string DispositivoId { get; set; } = string.Empty;
    [Required, StringLength(40)] public string Tipo { get; set; } = string.Empty;
    [Required] public string DataHora { get; set; } = string.Empty;
}
public sealed class ConfirmarAlertaRequest
{
    [Range(1, int.MaxValue)] public int FamiliarId { get; set; }
    [StringLength(500)] public string? Observacao { get; set; }
}
public sealed record LeituraResponse(int Id, double IndiceCalorCalculado, double? IndiceCalorInformado,
    NivelRisco Nivel, string? Divergencia);
public sealed record EventoResponse(bool Registrado, string Mensagem);
public sealed record ResidenciaStatusResponse(double IndiceCalor, NivelRisco Nivel, DateTime? UltimoMovimento,
    DateTime? UltimaAgua, bool AlertaAtivo, bool ModoVigilancia, DateTime? VigilanciaAteUtc);
public sealed record AlertaResponse(int Id, string Idoso, double IndiceCalor, NivelRisco Nivel,
    TipoAlerta Tipo, StatusAlerta Status, DateTime CriadoEmUtc, string Motivo);
public sealed record VisitaPrioritariaResponse(int Id, string Idoso, string Endereco, NivelRisco Nivel,
    double Pontuacao, string Motivo, DateTime CriadaEmUtc, StatusVisita Status);

public sealed class IdosoRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Nome { get; set; } = string.Empty;
    [Range(60, 120)] public int Idade { get; set; }
    public bool PossuiVulnerabilidade { get; set; }
    [StringLength(500)] public string? DescricaoVulnerabilidade { get; set; }
}
public sealed class FamiliarRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Nome { get; set; } = string.Empty;
    [StringLength(80)] public string? ContatoMascarado { get; set; }
    public List<int> IdososIds { get; set; } = [];
}
public sealed class AgenteSaudeRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Nome { get; set; } = string.Empty;
    [Required, StringLength(160)] public string UnidadeSaude { get; set; } = "UBS Demonstrativa";
}
public sealed class ResidenciaRequest
{
    [Required, StringLength(100)] public string Bairro { get; set; } = string.Empty;
    [StringLength(100)] public string Cidade { get; set; } = "São Paulo";
    [StringLength(2)] public string Estado { get; set; } = "SP";
    [Required, StringLength(180)] public string IdentificacaoEndereco { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int IdosoId { get; set; }
}
public sealed class DispositivoRequest
{
    [Required, StringLength(40, MinimumLength = 3)] public string Codigo { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int ResidenciaId { get; set; }
}
public sealed class AtualizarStatusVisitaRequest
{
    [Required] public StatusVisita Status { get; set; }
}
