using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Faculdade.Duvidas;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<DuvidaFaculdade> Duvidas { get; set; } = [];

    public async Task OnGetAsync()
    {
        Duvidas = await _db.DuvidasFaculdade
            .Include(d => d.Materia)
            .OrderBy(d => d.Resolvida)
            .ThenByDescending(d => d.DataRegistro)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostResolverAsync(int id, string resolucao)
    {
        var duvida = await _db.DuvidasFaculdade.FindAsync(id);
        if (duvida != null)
        {
            duvida.Resolvida = true;
            duvida.Resolucao = resolucao;
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Dúvida marcada como resolvida.";
        }
        return RedirectToPage();
    }
}
