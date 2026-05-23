using System.Text.Json;
using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadernoVivo.Pages.Faculdade.Importar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public string? ResultadoImportacao { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
        {
            TempData["Erro"] = "Selecione um arquivo JSON.";
            return Page();
        }

        try
        {
            using var stream = arquivo.OpenReadStream();
            var json = await JsonSerializer.DeserializeAsync<JsonElement>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var tipo = json.GetProperty("tipo").GetString();

            if (tipo == "materia")
            {
                var materia = new Materia
                {
                    Nome = json.GetProperty("nome").GetString() ?? "Importada",
                    Professor = json.TryGetProperty("professor", out var prof) ? prof.GetString() : null,
                    Descricao = json.TryGetProperty("descricao", out var desc) ? desc.GetString() : null,
                    Semestre = json.TryGetProperty("semestre", out var sem) ? sem.GetString() : null,
                    DataCriacao = DateTime.Now
                };

                _db.Materias.Add(materia);
                await _db.SaveChangesAsync();

                int aulas = 0, mats = 0, tarefas = 0, duvidas = 0;

                if (json.TryGetProperty("aulas", out var aulasJson))
                {
                    foreach (var a in aulasJson.EnumerateArray())
                    {
                        _db.AulasSemanais.Add(new AulaSemanal
                        {
                            MateriaId = materia.Id,
                            DiaSemana = a.TryGetProperty("diaSemana", out var d) ? d.GetInt32() : 1,
                            HoraInicio = a.TryGetProperty("horaInicio", out var hi) ? hi.GetString() ?? "08:00" : "08:00",
                            HoraFim = a.TryGetProperty("horaFim", out var hf) ? hf.GetString() ?? "10:00" : "10:00",
                            Sala = a.TryGetProperty("sala", out var sala) ? sala.GetString() : null
                        });
                        aulas++;
                    }
                }

                if (json.TryGetProperty("materiais", out var matsJson))
                {
                    foreach (var mat in matsJson.EnumerateArray())
                    {
                        var tipoStr = mat.TryGetProperty("tipo", out var t) ? t.GetString() : "Outro";
                        Enum.TryParse<TipoMaterial>(tipoStr, out var tipoEnum);
                        _db.Materiais.Add(new Material
                        {
                            MateriaId = materia.Id,
                            Nome = mat.GetProperty("nome").GetString() ?? "Material",
                            Tipo = tipoEnum,
                            Url = mat.TryGetProperty("url", out var url) ? url.GetString() : null,
                            DataCriacao = DateTime.Now
                        });
                        mats++;
                    }
                }

                if (json.TryGetProperty("tarefas", out var tarefasJson))
                {
                    foreach (var t in tarefasJson.EnumerateArray())
                    {
                        DateTime prazo = DateTime.Now.AddDays(7);
                        if (t.TryGetProperty("dataHoraLimite", out var dt))
                            DateTime.TryParse(dt.GetString(), out prazo);

                        _db.TarefasFaculdade.Add(new TarefaFaculdade
                        {
                            MateriaId = materia.Id,
                            Titulo = t.GetProperty("titulo").GetString() ?? "Tarefa",
                            Descricao = t.TryGetProperty("descricao", out var d) ? d.GetString() : null,
                            DataHoraLimite = prazo,
                            DataCriacao = DateTime.Now
                        });
                        tarefas++;
                    }
                }

                if (json.TryGetProperty("duvidas", out var duvsJson))
                {
                    foreach (var d in duvsJson.EnumerateArray())
                    {
                        _db.DuvidasFaculdade.Add(new DuvidaFaculdade
                        {
                            MateriaId = materia.Id,
                            Descricao = d.GetProperty("descricao").GetString() ?? "Dúvida",
                            DataRegistro = DateTime.Now
                        });
                        duvidas++;
                    }
                }

                await _db.SaveChangesAsync();
                TempData["Sucesso"] = $"Matéria \"{materia.Nome}\" importada: {aulas} aula(s), {mats} material(is), {tarefas} tarefa(s), {duvidas} dúvida(s).";
            }
            else
            {
                TempData["Erro"] = $"Tipo \"{tipo}\" não reconhecido para este módulo. Use tipo: \"materia\".";
            }
        }
        catch (Exception ex)
        {
            TempData["Erro"] = $"Erro ao importar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
