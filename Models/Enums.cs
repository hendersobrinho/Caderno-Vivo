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
