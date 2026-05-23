using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Faculdade.Tarefas;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<TarefaFaculdade> Tarefas { get; set; } = [];
    public string? FiltroStatus { get; set; }
    public int? FiltroMateria { get; set; }
    public List<Materia> Materias { get; set; } = [];

    public async Task OnGetAsync(string? status, int? materia)
    {
        FiltroStatus = status;
        FiltroMateria = materia;
        Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();

        var query = _db.TarefasFaculdade.Include(t => t.Materia).AsQueryable();

        if (materia.HasValue)
            query = query.Where(t => t.MateriaId == materia.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusTarefa>(status, out var s))
            query = query.Where(t => t.Status == s);

        Tarefas = await query.OrderBy(t => t.DataHoraLimite).ToListAsync();
    }
}
