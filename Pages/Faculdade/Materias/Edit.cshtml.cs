using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CadernoVivo.Pages.Faculdade.Materias;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public Materia Materia { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var materia = await _db.Materias.FindAsync(id);
        if (materia == null) return NotFound();
        Materia = materia;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        _db.Materias.Update(Materia);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Matéria atualizada com sucesso!";
        return RedirectToPage("/Faculdade/Index");
    }
}
