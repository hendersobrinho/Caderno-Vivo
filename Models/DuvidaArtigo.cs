using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class DuvidaArtigo
{
    public int Id { get; set; }

    public int ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    [Required(ErrorMessage = "Descrição é obrigatória")]
    [Display(Name = "Dúvida")]
    public string Descricao { get; set; } = "";

    [Display(Name = "Resolvida")]
    public bool Resolvida { get; set; }

    [Display(Name = "Resolução")]
    public string? Resolucao { get; set; }

    public DateTime DataRegistro { get; set; } = DateTime.Now;
}
