using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class Materia
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = "";

    [StringLength(200)]
    [Display(Name = "Professor(a)")]
    public string? Professor { get; set; }

    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [StringLength(20)]
    [Display(Name = "Semestre")]
    public string? Semestre { get; set; }

    [Display(Name = "Carga Horária (h)")]
    public int? CargaHoraria { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public List<AulaSemanal> AulasSemanais { get; set; } = [];
    public List<Material> Materiais { get; set; } = [];
    public List<TarefaFaculdade> Tarefas { get; set; } = [];
    public List<HistoricoAula> Historico { get; set; } = [];
    public List<DuvidaFaculdade> Duvidas { get; set; } = [];
}
