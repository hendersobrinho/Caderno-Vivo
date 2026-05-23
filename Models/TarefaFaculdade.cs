using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class TarefaFaculdade
{
    public int Id { get; set; }

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    [Required(ErrorMessage = "Título é obrigatório")]
    [StringLength(300)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";

    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [Required]
    [Display(Name = "Data/Hora Limite")]
    public DateTime DataHoraLimite { get; set; } = DateTime.Now.AddDays(7);

    [Display(Name = "Status")]
    public StatusTarefa Status { get; set; } = StatusTarefa.Pendente;

    public DateTime? DataConclusao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
