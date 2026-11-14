namespace HeatSafe.Api.Domain;

public abstract class Pessoa
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataCadastroUtc { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;
}

public sealed class Idoso : Pessoa
{
    public int Idade { get; set; }
    public bool PossuiVulnerabilidade { get; set; }
    public string? DescricaoVulnerabilidade { get; set; }
    public Residencia? Residencia { get; set; }
    public ICollection<FamiliarIdoso> FamiliaresVinculados { get; set; } = new List<FamiliarIdoso>();
    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
    public ICollection<Visita> Visitas { get; set; } = new List<Visita>();
    public ICollection<RegistroHidratacao> Hidratacoes { get; set; } = new List<RegistroHidratacao>();
}

public sealed class Familiar : Pessoa
{
    public string? ContatoMascarado { get; set; }
    public ICollection<FamiliarIdoso> IdososVinculados { get; set; } = new List<FamiliarIdoso>();
    public ICollection<Alerta> AlertasConfirmados { get; set; } = new List<Alerta>();
}

public sealed class AgenteSaude : Pessoa
{
    public string UnidadeSaude { get; set; } = "UBS Demonstrativa";
    public ICollection<Visita> Visitas { get; set; } = new List<Visita>();
}

public sealed class FamiliarIdoso
{
    public int FamiliarId { get; set; }
    public Familiar Familiar { get; set; } = null!;
    public int IdosoId { get; set; }
    public Idoso Idoso { get; set; } = null!;
}

public sealed class Residencia
{
    public int Id { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = "São Paulo";
    public string Estado { get; set; } = "SP";
    public string IdentificacaoEndereco { get; set; } = string.Empty;
    public int IdosoId { get; set; }
    public Idoso Idoso { get; set; } = null!;
    public Dispositivo? Dispositivo { get; set; }
    public DateTime? UltimaMovimentacaoUtc { get; set; }
    public DateTime? UltimaHidratacaoUtc { get; set; }
    public DateTime? VigilanciaAteUtc { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class Dispositivo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int ResidenciaId { get; set; }
    public Residencia Residencia { get; set; } = null!;
    public bool Ativo { get; set; } = true;
    public DateTime? UltimaComunicacaoUtc { get; set; }
    public ICollection<Leitura> Leituras { get; set; } = new List<Leitura>();
    public ICollection<EventoDispositivo> Eventos { get; set; } = new List<EventoDispositivo>();
}

public sealed class Leitura
{
    public int Id { get; set; }
    public int DispositivoId { get; set; }
    public Dispositivo Dispositivo { get; set; } = null!;
    public double TemperaturaC { get; set; }
    public double Umidade { get; set; }
    public double? IndiceCalorInformadoC { get; set; }
    public string? NivelInformado { get; set; }
    public double IndiceCalorCalculadoC { get; set; }
    public NivelRisco NivelRisco { get; set; }
    public bool MovimentoDetectado { get; set; }
    public DateTime DataHoraUtc { get; set; }
    public DateTime RecebidoEmUtc { get; set; }
}

public sealed class RegistroHidratacao
{
    public int Id { get; set; }
    public int IdosoId { get; set; }
    public Idoso Idoso { get; set; } = null!;
    public int DispositivoId { get; set; }
    public Dispositivo Dispositivo { get; set; } = null!;
    public DateTime DataHoraUtc { get; set; }
}

public sealed class EventoDispositivo
{
    public int Id { get; set; }
    public int DispositivoId { get; set; }
    public Dispositivo Dispositivo { get; set; } = null!;
    public TipoEvento Tipo { get; set; }
    public DateTime DataHoraUtc { get; set; }
    public string ChaveUnica { get; set; } = string.Empty;
}

public sealed class Alerta
{
    public int Id { get; set; }
    public int IdosoId { get; set; }
    public Idoso Idoso { get; set; } = null!;
    public int ResidenciaId { get; set; }
    public Residencia Residencia { get; set; } = null!;
    public double IndiceCalorC { get; set; }
    public NivelRisco NivelRisco { get; set; }
    public TipoAlerta Tipo { get; set; }
    public StatusAlerta Status { get; set; } = StatusAlerta.ATIVO;
    public DateTime CriadoEmUtc { get; set; }
    public DateTime? ConfirmadoEmUtc { get; set; }
    public int? FamiliarConfirmacaoId { get; set; }
    public Familiar? FamiliarConfirmacao { get; set; }
    public DateTime? EscalonadoEmUtc { get; set; }
    public DateTime? ResolvidoEmUtc { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public ICollection<Visita> Visitas { get; set; } = new List<Visita>();
}

public sealed class Visita
{
    public int Id { get; set; }
    public int IdosoId { get; set; }
    public Idoso Idoso { get; set; } = null!;
    public int ResidenciaId { get; set; }
    public Residencia Residencia { get; set; } = null!;
    public int? AgenteSaudeId { get; set; }
    public AgenteSaude? AgenteSaude { get; set; }
    public int AlertaId { get; set; }
    public Alerta Alerta { get; set; } = null!;
    public double PontuacaoPrioridade { get; set; }
    public string MotivoPrioridade { get; set; } = string.Empty;
    public StatusVisita Status { get; set; } = StatusVisita.PENDENTE;
    public DateTime CriadaEmUtc { get; set; }
    public DateTime? AtualizadaEmUtc { get; set; }
}

public sealed class PrevisaoClimatica
{
    public int Id { get; set; }
    public DateTime DataPrevista { get; set; }
    public bool Perigoso { get; set; }
    public double Probabilidade { get; set; }
    public DateTime ConsultadaEmUtc { get; set; }
    public string Estacao { get; set; } = "A701";
}
