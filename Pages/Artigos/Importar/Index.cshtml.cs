using System.Text.Json;
using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadernoVivo.Pages.Artigos.Importar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

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

            if (tipo == "artigo")
            {
                var artigo = new Artigo
                {
                    Titulo = json.GetProperty("titulo").GetString() ?? "Importado",
                    Descricao = json.TryGetProperty("descricao", out var desc) ? desc.GetString() : null,
                    Tags = json.TryGetProperty("tags", out var tags) ? tags.GetString() : null,
                    DataCriacao = DateTime.Now,
                    DataAtualizacao = DateTime.Now
                };

                if (json.TryGetProperty("status", out var st) &&
                    Enum.TryParse<StatusArtigo>(st.GetString(), out var statusEnum))
                    artigo.Status = statusEnum;

                _db.Artigos.Add(artigo);
                await _db.SaveChangesAsync();

                int roadmap = 0, aulas = 0, tarefas = 0, insights = 0, duvidas = 0;

                if (json.TryGetProperty("roadmap", out var roadmapJson))
                {
                    int ordem = 1;
                    foreach (var r in roadmapJson.EnumerateArray())
                    {
                        _db.RoadmapItems.Add(new RoadmapItem
                        {
                            ArtigoId = artigo.Id,
                            Titulo = r.GetProperty("titulo").GetString() ?? "Etapa",
                            Descricao = r.TryGetProperty("descricao", out var d) ? d.GetString() : null,
                            Ordem = r.TryGetProperty("ordem", out var o) ? o.GetInt32() : ordem
                        });
                        ordem++;
                        roadmap++;
                    }
                }

                if (json.TryGetProperty("aulas", out var aulasJson))
                {
                    int ordem = 1;
                    foreach (var a in aulasJson.EnumerateArray())
                    {
                        _db.AulasArtigo.Add(new AulaArtigo
                        {
                            ArtigoId = artigo.Id,
                            Titulo = a.GetProperty("titulo").GetString() ?? "Aula",
                            Conteudo = a.TryGetProperty("conteudo", out var c) ? c.GetString() : null,
                            Ordem = a.TryGetProperty("ordem", out var o) ? o.GetInt32() : ordem
                        });
                        ordem++;
                        aulas++;
                    }
                }

                if (json.TryGetProperty("tarefas", out var tarefasJson))
                {
                    foreach (var t in tarefasJson.EnumerateArray())
                    {
                        DateTime prazo = DateTime.Now.AddDays(7);
                        if (t.TryGetProperty("dataHoraLimite", out var dt))
                            DateTime.TryParse(dt.GetString(), out prazo);

                        _db.TarefasArtigo.Add(new TarefaArtigo
                        {
                            ArtigoId = artigo.Id,
                            Titulo = t.GetProperty("titulo").GetString() ?? "Tarefa",
                            Descricao = t.TryGetProperty("descricao", out var d) ? d.GetString() : null,
                            DataHoraLimite = prazo,
                            DataCriacao = DateTime.Now
                        });
                        tarefas++;
                    }
                }

                if (json.TryGetProperty("insights", out var insightsJson))
                {
                    foreach (var i in insightsJson.EnumerateArray())
                    {
                        _db.Insights.Add(new Insight
                        {
                            ArtigoId = artigo.Id,
                            Conteudo = i.GetProperty("conteudo").GetString() ?? "Insight",
                            DataRegistro = DateTime.Now
                        });
                        insights++;
                    }
                }

                if (json.TryGetProperty("duvidas", out var duvsJson))
                {
                    foreach (var d in duvsJson.EnumerateArray())
                    {
                        _db.DuvidasArtigo.Add(new DuvidaArtigo
                        {
                            ArtigoId = artigo.Id,
                            Descricao = d.GetProperty("descricao").GetString() ?? "Dúvida",
                            DataRegistro = DateTime.Now
                        });
                        duvidas++;
                    }
                }

                await _db.SaveChangesAsync();
                TempData["Sucesso"] = $"Artigo \"{artigo.Titulo}\" importado: {roadmap} etapas, {aulas} aula(s), {tarefas} tarefa(s), {insights} insight(s), {duvidas} dúvida(s).";
                return RedirectToPage("/Artigos/Detalhes", new { id = artigo.Id });
            }
            else
            {
                TempData["Erro"] = $"Tipo \"{tipo}\" não reconhecido. Use tipo: \"artigo\".";
            }
        }
        catch (Exception ex)
        {
            TempData["Erro"] = $"Erro ao importar: {ex.Message}";
        }

        return Page();
    }
}
