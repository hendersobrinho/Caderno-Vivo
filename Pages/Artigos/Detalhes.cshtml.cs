using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Artigos;

public class DetalhesModel : PageModel
{
    private readonly AppDbContext _db;

    public DetalhesModel(AppDbContext db) => _db = db;

    public Artigo Artigo { get; set; } = null!;

    [BindProperty] public RoadmapItem NovoRoadmap { get; set; } = new();
    [BindProperty] public AulaArtigo NovaAula { get; set; } = new();
    [BindProperty] public Insight NovoInsight { get; set; } = new();

    [BindProperty] public string? UltimoPonto { get; set; }
    [BindProperty] public int Progresso { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var artigo = await CarregarArtigo(id);
        if (artigo == null) return NotFound();
        Artigo = artigo;
        UltimoPonto = artigo.UltimoPontoEstudado;
        Progresso = artigo.Progresso;
        return Page();
    }

    public async Task<IActionResult> OnPostAtualizarProgressoAsync(int id)
    {
        var artigo = await _db.Artigos.FindAsync(id);
        if (artigo == null) return NotFound();

        artigo.UltimoPontoEstudado = UltimoPonto;
        artigo.Progresso = Math.Clamp(Progresso, 0, 100);
        artigo.DataAtualizacao = DateTime.Now;
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Progresso atualizado.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddRoadmapAsync(int id)
    {
        var artigo = await CarregarArtigo(id);
        if (artigo == null) return NotFound();
        Artigo = artigo;

        NovoRoadmap.ArtigoId = id;
        NovoRoadmap.Ordem = artigo.Roadmap.Count + 1;
        _db.RoadmapItems.Add(NovoRoadmap);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Etapa adicionada ao roadmap.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostConcluirRoadmapAsync(int id, int itemId)
    {
        var item = await _db.RoadmapItems.FindAsync(itemId);
        if (item != null)
        {
            item.Concluido = !item.Concluido;
            item.DataConclusao = item.Concluido ? DateTime.Now : null;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoverRoadmapAsync(int id, int itemId)
    {
        var item = await _db.RoadmapItems.FindAsync(itemId);
        if (item != null) { _db.RoadmapItems.Remove(item); await _db.SaveChangesAsync(); }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddAulaAsync(int id)
    {
        var artigo = await CarregarArtigo(id);
        if (artigo == null) return NotFound();
        Artigo = artigo;

        NovaAula.ArtigoId = id;
        NovaAula.Ordem = artigo.Aulas.Count + 1;
        _db.AulasArtigo.Add(NovaAula);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Aula adicionada.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostConcluirAulaAsync(int id, int aulaId)
    {
        var aula = await _db.AulasArtigo.FindAsync(aulaId);
        if (aula != null)
        {
            aula.Concluida = !aula.Concluida;
            aula.DataConclusao = aula.Concluida ? DateTime.Now : null;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddInsightAsync(int id)
    {
        var artigo = await CarregarArtigo(id);
        if (artigo == null) return NotFound();
        Artigo = artigo;

        NovoInsight.ArtigoId = id;
        NovoInsight.DataRegistro = DateTime.Now;
        _db.Insights.Add(NovoInsight);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Insight registrado.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoverInsightAsync(int id, int insightId)
    {
        var i = await _db.Insights.FindAsync(insightId);
        if (i != null) { _db.Insights.Remove(i); await _db.SaveChangesAsync(); }
        return RedirectToPage(new { id });
    }

    private async Task<Artigo?> CarregarArtigo(int id) =>
        await _db.Artigos
            .Include(a => a.Roadmap.OrderBy(r => r.Ordem))
            .Include(a => a.Aulas.OrderBy(au => au.Ordem))
            .Include(a => a.Insights.OrderByDescending(i => i.DataRegistro))
            .FirstOrDefaultAsync(a => a.Id == id);
}
