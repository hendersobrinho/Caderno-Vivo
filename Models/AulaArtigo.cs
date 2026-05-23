using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class AulaArtigo
{
    public int Id { get; set; }

    public int ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    [Required(ErrorMessage = "Título é obrigatório")]
    [StringLength(300)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";

    [Display(Name = "Conteúdo / Notas")]
    public string? Conteudo { get; set; }

    [Display(Name = "Ordem")]
    public int Ordem { get; set; } = 1;

    [Display(Name = "Concluída")]
    public bool Concluida { get; set; }

    public DateTime? DataConclusao { get; set; }
}
