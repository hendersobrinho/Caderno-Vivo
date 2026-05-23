using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadernoVivo.Pages.Artigos;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;

    public DeleteModel(AppDbContext db) => _db = db;

    public Artigo Artigo { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var artigo = await _db.Artigos.FindAsync(id);
        if (artigo == null) return NotFound();
        Artigo = artigo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var artigo = await _db.Artigos.FindAsync(id);
        if (artigo != null)
        {
            _db.Artigos.Remove(artigo);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = $"Artigo \"{artigo.Titulo}\" removido.";
        }
        return RedirectToPage("Index");
    }
}
