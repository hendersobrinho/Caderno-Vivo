using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadernoVivo.Pages.Artigos;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public Artigo Artigo { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var artigo = await _db.Artigos.FindAsync(id);
        if (artigo == null) return NotFound();
        Artigo = artigo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        Artigo.DataAtualizacao = DateTime.Now;
        _db.Artigos.Update(Artigo);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Artigo atualizado.";
        return RedirectToPage("Detalhes", new { id = Artigo.Id });
    }
}
