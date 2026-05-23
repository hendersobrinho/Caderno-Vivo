using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Faculdade;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Materia> Materias { get; set; } = [];

    public async Task OnGetAsync()
    {
        Materias = await _db.Materias
            .Include(m => m.AulasSemanais)
            .Include(m => m.Tarefas)
            .Include(m => m.Duvidas)
            .OrderBy(m => m.Nome)
            .ToListAsync();
    }
}
