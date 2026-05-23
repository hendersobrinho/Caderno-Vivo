using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class Pendencia
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Descrição é obrigatória")]
    [Display(Name = "Descrição")]
    public string Descricao { get; set; } = "";

    [Display(Name = "Origem")]
    public string Origem { get; set; } = "";

    public int? TarefaFaculdadeId { get; set; }
    public TarefaFaculdade? TarefaFaculdade { get; set; }

    public int? TarefaArtigoId { get; set; }
    public TarefaArtigo? TarefaArtigo { get; set; }

    [Display(Name = "Resolvida")]
    public bool Resolvida { get; set; }

    [Display(Name = "Data Limite")]
    public DateTime? DataLimite { get; set; }

    [Display(Name = "Observação")]
    public string? Observacao { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;
}
