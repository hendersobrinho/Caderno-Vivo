using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadernoVivo.Pages.Artigos;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty]
    public Artigo Artigo { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        Artigo.DataCriacao = DateTime.Now;
        Artigo.DataAtualizacao = DateTime.Now;
        _db.Artigos.Add(Artigo);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Artigo \"{Artigo.Titulo}\" criado!";
        return RedirectToPage("Detalhes", new { id = Artigo.Id });
    }
}
