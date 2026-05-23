using CadernoVivo.Models;

namespace CadernoVivo.Helpers;

public static class DiasHelper
{
    private static readonly string[] Dias =
        ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"];

    public static string NomeDia(int dia) =>
        dia >= 0 && dia < 7 ? Dias[dia] : "?";

    public static string LabelStatus(StatusTarefa status) => status switch
    {
        StatusTarefa.Pendente     => "Pendente",
        StatusTarefa.EmAndamento  => "Em Andamento",
        StatusTarefa.Parcial      => "Parcial",
        StatusTarefa.Concluida    => "Concluída",
        StatusTarefa.NaoEncaixou  => "Não Encaixou",
        _                         => "?"
    };

    public static string ClasseStatus(StatusTarefa status) => status switch
    {
        StatusTarefa.Pendente     => "secondary",
        StatusTarefa.EmAndamento  => "primary",
        StatusTarefa.Parcial      => "warning",
        StatusTarefa.Concluida    => "success",
        StatusTarefa.NaoEncaixou  => "danger",
        _                         => "secondary"
    };

    public static string LabelStatusArtigo(StatusArtigo status) => status switch
    {
        StatusArtigo.Ideia            => "Ideia",
        StatusArtigo.EmDesenvolvimento => "Em Desenvolvimento",
        StatusArtigo.Revisao          => "Em Revisão",
        StatusArtigo.Publicado        => "Publicado",
        StatusArtigo.Pausado          => "Pausado",
        _                             => "?"
    };

    public static string ClasseStatusArtigo(StatusArtigo status) => status switch
    {
        StatusArtigo.Ideia            => "secondary",
        StatusArtigo.EmDesenvolvimento => "primary",
        StatusArtigo.Revisao          => "info",
        StatusArtigo.Publicado        => "success",
        StatusArtigo.Pausado          => "warning",
        _                             => "secondary"
    };

    public static string LabelTipoMaterial(TipoMaterial tipo) => tipo switch
    {
        TipoMaterial.Livro  => "Livro",
        TipoMaterial.PDF    => "PDF",
        TipoMaterial.Video  => "Vídeo",
        TipoMaterial.Link   => "Link",
        TipoMaterial.Outro  => "Outro",
        _                   => "?"
    };

    public static bool GerarPendencia(StatusTarefa status) =>
        status is StatusTarefa.Parcial or StatusTarefa.NaoEncaixou;
}
