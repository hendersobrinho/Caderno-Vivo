using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Faculdade.PlanoDaSemana;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Materia> Materias { get; set; } = [];
    public List<TarefaFaculdade> TarefasSemana { get; set; } = [];

    // Grade[dia] = lista de (aula, materia)
    public Dictionary<int, List<(AulaSemanal Aula, string MateriaNome)>> Grade { get; set; } = [];

    public async Task OnGetAsync()
    {
        Materias = await _db.Materias
            .Include(m => m.AulasSemanais)
            .ToListAsync();

        for (int i = 0; i < 7; i++)
            Grade[i] = [];

        foreach (var m in Materias)
            foreach (var a in m.AulasSemanais)
                Grade[a.DiaSemana].Add((a, m.Nome));

        var inicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        var fimSemana = inicioSemana.AddDays(7);

        TarefasSemana = await _db.TarefasFaculdade
            .Include(t => t.Materia)
            .Where(t => t.DataHoraLimite >= inicioSemana && t.DataHoraLimite < fimSemana)
            .OrderBy(t => t.DataHoraLimite)
            .ToListAsync();
    }
}
