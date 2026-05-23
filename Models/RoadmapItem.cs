using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class RoadmapItem
{
    public int Id { get; set; }

    public int ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    [Required(ErrorMessage = "Título é obrigatório")]
    [StringLength(300)]
    [Display(Name = "Etapa")]
    public string Titulo { get; set; } = "";

    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [Display(Name = "Ordem")]
    public int Ordem { get; set; } = 1;

    [Display(Name = "Concluído")]
    public bool Concluido { get; set; }

    public DateTime? DataConclusao { get; set; }
}
