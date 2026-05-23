namespace CadernoVivo.Models;

public enum StatusTarefa
{
    Pendente = 0,
    EmAndamento = 1,
    Parcial = 2,
    Concluida = 3,
    NaoEncaixou = 4
}

public enum StatusArtigo
{
    Ideia = 0,
    EmDesenvolvimento = 1,
    Revisao = 2,
    Publicado = 3,
    Pausado = 4
}

public enum TipoMaterial
{
    Livro = 0,
    PDF = 1,
    Video = 2,
    Link = 3,
    Outro = 4
}

public enum StatusBloco
{
    Agendado    = 0,
    EmAndamento = 1,
    Concluido   = 2,
    Parcial     = 3,
    NaoFeito    = 4,
    Reagendado  = 5,
    HoraExtra   = 6
}

public enum PrioridadeProjeto
{
    Baixa  = 0,
    Media  = 1,
    Alta   = 2,
    Urgente = 3
}

public enum StatusProjeto
{
    AFazer      = 0,
    EmAndamento = 1,
    Pausado     = 2,
    Concluido   = 3
}

public enum StatusLembrete
{
    Aberto = 0,
    Concluido = 1,
    Arquivado = 2
}

public enum EscopoLembrete
{
    Avulso = 0,
    Faculdade = 1,
    Projeto = 2,
    Artigo = 3
}
