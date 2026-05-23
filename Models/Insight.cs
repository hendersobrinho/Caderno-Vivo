using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class Insight
{
    public int Id { get; set; }

    public int ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    [Required(ErrorMessage = "Conteúdo é obrigatório")]
    [Display(Name = "Insight")]
    public string Conteudo { get; set; } = "";

    public DateTime DataRegistro { get; set; } = DateTime.Now;
}
