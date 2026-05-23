using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class Artigo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Título é obrigatório")]
    [StringLength(300)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";

    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [StringLength(500)]
    [Display(Name = "Tags")]
    public string? Tags { get; set; }

    [Display(Name = "Status")]
    public StatusArtigo Status { get; set; } = StatusArtigo.Ideia;

    [Display(Name = "Último ponto estudado")]
    public string? UltimoPontoEstudado { get; set; }

    [Display(Name = "Progresso (%)")]
    [Range(0, 100)]
    public int Progresso { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;

    public List<AulaArtigo> Aulas { get; set; } = [];
    public List<RoadmapItem> Roadmap { get; set; } = [];
    public List<TarefaArtigo> Tarefas { get; set; } = [];
    public List<Insight> Insights { get; set; } = [];
    public List<DuvidaArtigo> Duvidas { get; set; } = [];
}
