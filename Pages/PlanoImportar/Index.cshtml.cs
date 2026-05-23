using System.Text.Json;
using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.PlanoImportar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty]
    public string JsonTexto { get; set; } = "";

    public string? Resultado { get; set; }
    public bool Sucesso { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(JsonTexto))
        {
            TempData["Erro"] = "Cole o JSON do plano antes de importar.";
            return RedirectToPage();
        }

        try
        {
            using var doc = JsonDocument.Parse(JsonTexto);
            var root = doc.RootElement;

            var tipo = root.TryGetProperty("tipo", out var t) ? t.GetString() : null;
            if (tipo != "plano")
            {
                TempData["Erro"] = "JSON inválido. O campo \"tipo\" deve ser \"plano\".";
                return RedirectToPage();
            }

            var materias = await _db.Materias.ToListAsync();
            var artigos = await _db.Artigos.ToListAsync();

            int criados = 0;
            int duplicados = 0;

            if (root.TryGetProperty("dias", out var dias))
            {
                foreach (var dia in dias.EnumerateArray())
                {
                    if (!dia.TryGetProperty("data", out var dataEl)) continue;
                    if (!DateTime.TryParse(dataEl.GetString(), out var data)) continue;

                    if (!dia.TryGetProperty("blocos", out var blocos)) continue;

                    foreach (var bloco in blocos.EnumerateArray())
                    {
                        var inicio = bloco.TryGetProperty("inicio", out var i) ? i.GetString() ?? "08:00" : "08:00";
                        var fim = bloco.TryGetProperty("fim", out var f) ? f.GetString() ?? "10:00" : "10:00";
                        var titulo = bloco.TryGetProperty("titulo", out var ti) ? ti.GetString() ?? "" : "";
                        var descricao = bloco.TryGetProperty("descricao", out var d) ? d.GetString() : null;
                        var modulo = bloco.TryGetProperty("modulo", out var mo) ? mo.GetString() ?? "Geral" : "Geral";
                        var nomMateria = bloco.TryGetProperty("materia", out var ma) ? ma.GetString() : null;
                        var nomArtigo = bloco.TryGetProperty("artigo", out var ar) ? ar.GetString() : null;

                        if (string.IsNullOrWhiteSpace(titulo)) continue;

                        // Verifica duplicata (mesma data, inicio e titulo)
                        var jaExiste = await _db.BloquesEstudo.AnyAsync(b =>
                            b.Data.Date == data.Date &&
                            b.HoraInicio == inicio &&
                            b.Titulo == titulo);

                        if (jaExiste) { duplicados++; continue; }

                        // Tenta encontrar matéria e artigo por nome
                        int? materiaId = null;
                        if (!string.IsNullOrWhiteSpace(nomMateria))
                        {
                            var m = materias.FirstOrDefault(x =>
                                x.Nome.Equals(nomMateria, StringComparison.OrdinalIgnoreCase));
                            materiaId = m?.Id;
                        }

                        int? artigoId = null;
                        if (!string.IsNullOrWhiteSpace(nomArtigo))
                        {
                            var a = artigos.FirstOrDefault(x =>
                                x.Titulo.Equals(nomArtigo, StringComparison.OrdinalIgnoreCase));
                            artigoId = a?.Id;
                        }

                        _db.BloquesEstudo.Add(new BlocoEstudo
                        {
                            Data = data,
                            HoraInicio = inicio,
                            HoraFim = fim,
                            Titulo = titulo,
                            Descricao = descricao,
                            Modulo = modulo,
                            MateriaId = materiaId,
                            ArtigoId = artigoId,
                            Status = StatusBloco.Agendado
                        });
                        criados++;
                    }
                }
            }

            await _db.SaveChangesAsync();

            TempData["Sucesso"] = $"Plano importado: {criados} bloco(s) criado(s)" +
                                  (duplicados > 0 ? $", {duplicados} ignorado(s) (duplicata)." : ".");
            return RedirectToPage("/Hoje/Index");
        }
        catch (JsonException ex)
        {
            TempData["Erro"] = $"Erro ao ler o JSON: {ex.Message}";
            return RedirectToPage();
        }
    }
}
