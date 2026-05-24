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

    public static string LabelStatusBloco(StatusBloco status) => status switch
    {
        StatusBloco.Agendado    => "Agendado",
        StatusBloco.EmAndamento => "Em Andamento",
        StatusBloco.Concluido   => "Concluído",
        StatusBloco.Parcial     => "Parcial",
        StatusBloco.NaoFeito    => "Não Feito",
        StatusBloco.Reagendado  => "Reagendado",
        StatusBloco.HoraExtra   => "Hora Extra",
        StatusBloco.Pausado     => "Pausado",
        _                       => "?"
    };

    public static string ClasseStatusBloco(StatusBloco status) => status switch
    {
        StatusBloco.Agendado    => "secondary",
        StatusBloco.EmAndamento => "primary",
        StatusBloco.Concluido   => "success",
        StatusBloco.Parcial     => "warning",
        StatusBloco.NaoFeito    => "danger",
        StatusBloco.Reagendado  => "info",
        StatusBloco.HoraExtra   => "dark",
        StatusBloco.Pausado     => "warning",
        _                       => "secondary"
    };

    public static string IconeStatusBloco(StatusBloco status) => status switch
    {
        StatusBloco.Agendado    => "bi-calendar",
        StatusBloco.EmAndamento => "bi-hourglass-split",
        StatusBloco.Concluido   => "bi-check-circle-fill",
        StatusBloco.Parcial     => "bi-dash-circle",
        StatusBloco.NaoFeito    => "bi-x-circle",
        StatusBloco.Reagendado  => "bi-calendar-plus",
        StatusBloco.HoraExtra   => "bi-clock-history",
        StatusBloco.Pausado     => "bi-pause-circle",
        _                       => "bi-circle"
    };

    public static bool BlocoGerarPendencia(StatusBloco status) =>
        status is StatusBloco.Parcial or StatusBloco.NaoFeito;

    public static string LabelStatusProjeto(StatusProjeto status) => status switch
    {
        StatusProjeto.AFazer      => "A Fazer",
        StatusProjeto.EmAndamento => "Em Andamento",
        StatusProjeto.Pausado     => "Pausado",
        StatusProjeto.Concluido   => "Concluído",
        _                         => "?"
    };

    public static string ClasseStatusProjeto(StatusProjeto status) => status switch
    {
        StatusProjeto.AFazer      => "secondary",
        StatusProjeto.EmAndamento => "primary",
        StatusProjeto.Pausado     => "warning",
        StatusProjeto.Concluido   => "success",
        _                         => "secondary"
    };

    public static string LabelPrioridade(PrioridadeProjeto p) => p switch
    {
        PrioridadeProjeto.Baixa   => "Baixa",
        PrioridadeProjeto.Media   => "Média",
        PrioridadeProjeto.Alta    => "Alta",
        PrioridadeProjeto.Urgente => "Urgente",
        _                         => "?"
    };

    public static string ClassePrioridade(PrioridadeProjeto p) => p switch
    {
        PrioridadeProjeto.Baixa   => "secondary",
        PrioridadeProjeto.Media   => "info",
        PrioridadeProjeto.Alta    => "warning",
        PrioridadeProjeto.Urgente => "danger",
        _                         => "secondary"
    };

    public static string ClasseCountdown(int? dias) => dias switch
    {
        null        => "secondary",
        <= 0        => "danger",
        <= 3        => "danger",
        <= 7        => "warning",
        _           => "success"
    };

    public static string LabelCountdown(int? dias) => dias switch
    {
        null  => "Sem prazo",
        < 0   => $"Atrasado {Math.Abs(dias.Value)}d",
        0     => "Hoje!",
        1     => "Amanhã",
        _     => $"{dias}d restantes"
    };

    public static string LabelStatusLembrete(StatusLembrete status) => status switch
    {
        StatusLembrete.Aberto => "Aberto",
        StatusLembrete.Concluido => "Concluido",
        StatusLembrete.Arquivado => "Arquivado",
        _ => "?"
    };

    public static string ClasseStatusLembrete(StatusLembrete status) => status switch
    {
        StatusLembrete.Aberto => "primary",
        StatusLembrete.Concluido => "success",
        StatusLembrete.Arquivado => "secondary",
        _ => "secondary"
    };

    public static string LabelEscopoLembrete(EscopoLembrete escopo) => escopo switch
    {
        EscopoLembrete.Avulso => "Avulso",
        EscopoLembrete.Faculdade => "Faculdade",
        EscopoLembrete.Projeto => "Projeto",
        EscopoLembrete.Artigo => "Artigo",
        _ => "?"
    };
}
