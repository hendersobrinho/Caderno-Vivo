using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class HistoricoAula
{
    public int Id { get; set; }

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    [Required]
    [Display(Name = "Data")]
    public DateTime Data { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Título/Tema é obrigatório")]
    [StringLength(300)]
    [Display(Name = "Título / Tema")]
    public string Titulo { get; set; } = "";

    [Display(Name = "Conteúdo")]
    public string? Conteudo { get; set; }

    [Display(Name = "Observação")]
    public string? Observacao { get; set; }
}
