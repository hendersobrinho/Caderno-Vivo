using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CadernoVivo.Models;

public class Projeto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Título é obrigatório")]
    [StringLength(300)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";

    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [Display(Name = "Prazo")]
    public DateTime? DataLimite { get; set; }

    [Display(Name = "Prioridade")]
    public PrioridadeProjeto Prioridade { get; set; } = PrioridadeProjeto.Media;

    [Display(Name = "Status")]
    public StatusProjeto Status { get; set; } = StatusProjeto.AFazer;

    [Display(Name = "Categoria")]
    public string? Categoria { get; set; }

    public int? MateriaId { get; set; }
    public Materia? Materia { get; set; }

    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }

    // JSON: [{"t":"texto","f":false}, ...]
    public string? ChecklistJson { get; set; }

    // JSON: CronogramaProjeto — recorrência + lista de sessões
    public string? CronogramaJson { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    // Não persistidos — calculados em memória
    [NotMapped]
    public int? DiasRestantes => DataLimite.HasValue
        ? (int)Math.Ceiling((DataLimite.Value.Date - DateTime.Today).TotalDays)
        : null;

    [NotMapped]
    public List<ChecklistItem> Checklist
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ChecklistJson)) return [];
            try { return JsonSerializer.Deserialize<List<ChecklistItem>>(ChecklistJson) ?? []; }
            catch { return []; }
        }
    }

    [NotMapped]
    public CronogramaProjeto? Cronograma
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CronogramaJson)) return null;
            try { return JsonSerializer.Deserialize<CronogramaProjeto>(CronogramaJson); }
            catch { return null; }
        }
    }

    [NotMapped]
    public bool TemCronograma => !string.IsNullOrWhiteSpace(CronogramaJson);
}

public class ChecklistItem
{
    [JsonPropertyName("t")] public string Texto { get; set; } = "";
    [JsonPropertyName("f")] public bool Feito { get; set; }
}
