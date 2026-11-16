namespace HeatSafe.Api.Domain;

public enum NivelRisco
{
    NORMAL,
    ATENCAO,
    CAUTELA_EXTREMA,
    PERIGO,
    PERIGO_EXTREMO
}

public enum TipoEvento
{
    AGUA,
    MOVIMENTO,
    ALARME_SEM_RESPOSTA,
    /// <summary>O aparelho lembrou o idoso de beber água. Só é registrado (indicador de hidratação), não conta como água bebida.</summary>
    LEMBRETE_AGUA
}

public enum TipoAlerta
{
    CAUTELA_SEM_MOVIMENTO,
    CAUTELA_SEM_HIDRATACAO,
    PERIGO_SEM_MOVIMENTO,
    PERIGO_EXTREMO,
    ALARME_SEM_RESPOSTA
}

public enum StatusAlerta
{
    ATIVO,
    CONFIRMADO,
    ESCALONADO,
    RESOLVIDO
}

public enum StatusVisita
{
    PENDENTE,
    EM_ANDAMENTO,
    CONCLUIDA,
    CANCELADA
}
