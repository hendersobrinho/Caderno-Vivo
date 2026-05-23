using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CadernoVivo.Models;

public class Lembrete
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Titulo e obrigatorio")]
    [StringLength(240)]
    [Display(Name = "Titulo")]
    public string Titulo { get; set; } = "";

    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [Display(Name = "Prazo")]
    public DateTime? DataLimite { get; set; }

    [Display(Name = "Prioridade")]
    public PrioridadeProjeto Prioridade { get; set; } = PrioridadeProjeto.Media;

    [Display(Name = "Status")]
    public StatusLembrete Status { get; set; } = StatusLembrete.Aberto;

    [Display(Name = "Escopo")]
    public EscopoLembrete Escopo { get; set; } = EscopoLembrete.Avulso;

    [Display(Name = "Destaque")]
    public bool Destaque { get; set; }

    public int? MateriaId { get; set; }
    public Materia? Materia { get; set; }

    public int? ProjetoId { get; set; }
    public Projeto? Projeto { get; set; }

    public int? ArtigoId { get; set; }
    public Artigo? Artigo { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime? DataConclusao { get; set; }

    [NotMapped]
    public int? DiasRestantes => DataLimite.HasValue
        ? (int)Math.Ceiling((DataLimite.Value.Date - DateTime.Today).TotalDays)
        : null;
}
