using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadernoVivo.Pages.Faculdade.Materias;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;

    public DeleteModel(AppDbContext db) => _db = db;

    public Materia Materia { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var materia = await _db.Materias.FindAsync(id);
        if (materia == null) return NotFound();
        Materia = materia;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var materia = await _db.Materias.FindAsync(id);
        if (materia != null)
        {
            _db.Materias.Remove(materia);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = $"Matéria \"{materia.Nome}\" removida.";
        }
        return RedirectToPage("/Faculdade/Index");
    }
}
