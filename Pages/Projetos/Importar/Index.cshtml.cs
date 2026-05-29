using System.Text.Json;
using System.Text.Json.Serialization;
using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Projetos.Importar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty] public string JsonTexto { get; set; } = "";

    public int? ProjetoImportadoId { get; set; }
    public string? ProjetoImportadoTitulo { get; set; }
    public int SessoesAgendadas { get; set; }
    public string? Erro { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(JsonTexto))
        {
            Erro = "Cole o JSON do projeto antes de importar.";
            return Page();
        }

        ProjetoJsonDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ProjetoJsonDto>(JsonTexto,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Erro = $"JSON inválido: {ex.Message}";
            return Page();
        }

        if (dto == null || dto.Tipo?.ToLower() != "projeto")
        {
            Erro = "O JSON precisa ter \"tipo\": \"projeto\".";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(dto.Titulo))
        {
            Erro = "O campo \"titulo\" é obrigatório.";
            return Page();
        }

        // Resolve MateriaId se informado
        int? materiaId = null;
        if (!string.IsNullOrWhiteSpace(dto.Materia))
        {
            var mat = await _db.Materias.FirstOrDefaultAsync(m =>
                m.Nome.ToLower() == dto.Materia.Trim().ToLower());
            if (mat != null) materiaId = mat.Id;
        }

        // Monta o cronograma
        CronogramaProjeto? cronograma = null;
        if (dto.Recorrencia != null && dto.Sessoes?.Count > 0)
        {
            var diasSemana = (dto.Recorrencia.Dias ?? [])
                .Select(d => CronogramaHelper.NomeDiaParaInt(d))
                .Where(d => d >= 0)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (diasSemana.Count > 0)
            {
                cronograma = new CronogramaProjeto
                {
                    Recorrencia = new RecorrenciaCronograma
                    {
                        Dias = diasSemana,
                        HoraInicio = dto.Recorrencia.HoraInicio ?? "19:00",
                        HoraFim = dto.Recorrencia.HoraFim ?? "21:00"
                    },
                    Sessoes = dto.Sessoes.Select(s => new SessaoCronograma
                    {
                        Titulo = s.Titulo ?? "",
                        Descricao = s.Descricao
                    }).ToList()
                };
            }
        }

        var projeto = new Projeto
        {
            Titulo = dto.Titulo.Trim(),
            Descricao = dto.Descricao?.Trim(),
            Categoria = dto.Categoria?.Trim(),
            Prioridade = ParsePrioridade(dto.Prioridade),
            Status = StatusProjeto.AFazer,
            MateriaId = materiaId,
            CronogramaJson = cronograma != null ? JsonSerializer.Serialize(cronograma) : null,
            ChecklistJson = ParseChecklist(dto.Checklist),
            DataCriacao = DateTime.Now
        };

        _db.Projetos.Add(projeto);
        await _db.SaveChangesAsync();

        // Agenda as sessões se houver cronograma
        if (cronograma != null)
        {
            SessoesAgendadas = await CronogramaHelper.AgendarSessoes(_db, projeto, cronograma);
            projeto.Status = StatusProjeto.EmAndamento;
            await _db.SaveChangesAsync();
        }

        ProjetoImportadoId = projeto.Id;
        ProjetoImportadoTitulo = projeto.Titulo;
        return Page();
    }

    private static PrioridadeProjeto ParsePrioridade(string? s) => s?.ToLower() switch
    {
        "baixa"   or "low"    => PrioridadeProjeto.Baixa,
        "alta"    or "high"   => PrioridadeProjeto.Alta,
        "urgente" or "urgent" => PrioridadeProjeto.Urgente,
        _                     => PrioridadeProjeto.Media
    };

    private static string? ParseChecklist(List<string>? linhas)
    {
        if (linhas == null || linhas.Count == 0) return null;
        var itens = linhas
            .Select(l => l.Trim().TrimStart('-', '*', '•').Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Select(l => new ChecklistItem { Texto = l, Feito = false })
            .ToList();
        return itens.Count == 0 ? null : JsonSerializer.Serialize(itens);
    }
}

// DTOs para desserialização do JSON de entrada
file class ProjetoJsonDto
{
    [JsonPropertyName("tipo")]         public string? Tipo { get; set; }
    [JsonPropertyName("titulo")]       public string? Titulo { get; set; }
    [JsonPropertyName("descricao")]    public string? Descricao { get; set; }
    [JsonPropertyName("categoria")]    public string? Categoria { get; set; }
    [JsonPropertyName("prioridade")]   public string? Prioridade { get; set; }
    [JsonPropertyName("materia")]      public string? Materia { get; set; }
    [JsonPropertyName("recorrencia")]  public RecorrenciaDtoJson? Recorrencia { get; set; }
    [JsonPropertyName("sessoes")]      public List<SessaoDtoJson>? Sessoes { get; set; }
    [JsonPropertyName("checklist")]    public List<string>? Checklist { get; set; }
}

file class RecorrenciaDtoJson
{
    [JsonPropertyName("dias")]         public List<string>? Dias { get; set; }
    [JsonPropertyName("hora_inicio")]  public string? HoraInicio { get; set; }
    [JsonPropertyName("hora_fim")]     public string? HoraFim { get; set; }
}

file class SessaoDtoJson
{
    [JsonPropertyName("titulo")]       public string? Titulo { get; set; }
    [JsonPropertyName("descricao")]    public string? Descricao { get; set; }
}
