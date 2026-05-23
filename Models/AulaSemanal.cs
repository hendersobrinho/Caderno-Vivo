using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class AulaSemanal
{
    public int Id { get; set; }

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    [Required]
    [Display(Name = "Dia da Semana")]
    [Range(0, 6)]
    public int DiaSemana { get; set; }

    [Required]
    [StringLength(5)]
    [Display(Name = "Início")]
    public string HoraInicio { get; set; } = "08:00";

    [Required]
    [StringLength(5)]
    [Display(Name = "Fim")]
    public string HoraFim { get; set; } = "10:00";

    [StringLength(100)]
    [Display(Name = "Sala / Local")]
    public string? Sala { get; set; }

    [Display(Name = "Observação")]
    public string? Observacao { get; set; }
}
