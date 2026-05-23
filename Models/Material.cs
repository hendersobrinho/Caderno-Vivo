using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class Material
{
    public int Id { get; set; }

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(300)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = "";

    [Required]
    [Display(Name = "Tipo")]
    public TipoMaterial Tipo { get; set; }

    [StringLength(500)]
    [Display(Name = "URL / Caminho")]
    public string? Url { get; set; }

    [Display(Name = "Observação")]
    public string? Observacao { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
