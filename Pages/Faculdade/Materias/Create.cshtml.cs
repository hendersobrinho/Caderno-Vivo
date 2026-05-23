using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadernoVivo.Pages.Faculdade.Materias;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty]
    public Materia Materia { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        Materia.DataCriacao = DateTime.Now;
        _db.Materias.Add(Materia);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Matéria \"{Materia.Nome}\" criada com sucesso!";
        return RedirectToPage("/Faculdade/Index");
    }
}
