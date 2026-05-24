using System.ComponentModel.DataAnnotations;

namespace CadernoVivo.Models;

public class BlocoEstudo
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Data")]
    public DateTime Data { get; set; } = DateTime.Today;

    [Required]
    [StringLength(5)]
    [Display(Name = "Início")]
    public string HoraInicio { get; set; } = "08:00";

    [Required]
    [StringLength(5)]
    [Display(Name = "Fim")]
    public string HoraFim { get; set; } = "10:00";

    [Required(ErrorMessage = "Título é obrigatório")]
    [StringLength(300)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";

    [Display(Name = "Descrição / objetivo")]
    public string? Descricao { get; set; }

    [Display(Name = "Módulo")]
    public string Modulo { get; set; } = "Geral"; // Faculdade, Artigo, Geral

    public int? MateriaId { get; set; }
    public Materia? Materia { get; set; }

    public int? ArtigoId { get; set; }
    public Artigo? Artigo { get; set; }

    public int? ProjetoId { get; set; }
    public Projeto? Projeto { get; set; }

    [Display(Name = "Status")]
    public StatusBloco Status { get; set; } = StatusBloco.Agendado;

    [Display(Name = "Conclusão")]
    public DateTime? DataConclusao { get; set; }

    [Display(Name = "Minutos extras")]
    public int MinutosExtras { get; set; }

    [Display(Name = "Iniciado em")]
    public DateTime? IniciadoEm { get; set; }

    [Display(Name = "Segundos pausados")]
    public int SegundosPausados { get; set; }

    [Display(Name = "Segundos gastos")]
    public int? SegundosGastos { get; set; }

    [Display(Name = "Pausado em")]
    public DateTime? PausadoEm { get; set; }

    [Display(Name = "Onde parei")]
    public string? OndeParei { get; set; }

    [Display(Name = "Próximo passo")]
    public string? ProximoPasso { get; set; }

    [Display(Name = "Dúvidas (uma por linha)")]
    public string? Duvidas { get; set; }

    [Display(Name = "Observação")]
    public string? Observacao { get; set; }
}
