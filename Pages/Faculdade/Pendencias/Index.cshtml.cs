using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Faculdade.Pendencias;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Pendencia> Pendencias { get; set; } = [];
    public bool MostrarResolvidas { get; set; }

    [BindProperty]
    public Pendencia NovaPendencia { get; set; } = new();

    public async Task OnGetAsync(bool resolvidas = false)
    {
        MostrarResolvidas = resolvidas;
        var query = _db.Pendencias.AsQueryable();
        if (!resolvidas) query = query.Where(p => !p.Resolvida);
        Pendencias = await query.OrderByDescending(p => p.DataCriacao).ToListAsync();
    }

    public async Task<IActionResult> OnPostResolverAsync(int id, string? obs)
    {
        var p = await _db.Pendencias.FindAsync(id);
        if (p != null)
        {
            p.Resolvida = true;
            p.Observacao = obs;
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Pendência marcada como resolvida.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        NovaPendencia.DataCriacao = DateTime.Now;
        _db.Pendencias.Add(NovaPendencia);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Pendência adicionada.";
        return RedirectToPage();
    }
}
