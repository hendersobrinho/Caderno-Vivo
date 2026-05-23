using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Faculdade.Tarefas;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public TarefaFaculdade Tarefa { get; set; } = null!;

    public List<Materia> Materias { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var tarefa = await _db.TarefasFaculdade.FindAsync(id);
        if (tarefa == null) return NotFound();
        Tarefa = tarefa;
        Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();
            return Page();
        }

        var statusAnterior = await _db.TarefasFaculdade
            .AsNoTracking()
            .Where(t => t.Id == Tarefa.Id)
            .Select(t => t.Status)
            .FirstOrDefaultAsync();

        if (Tarefa.Status == StatusTarefa.Concluida && Tarefa.DataConclusao == null)
            Tarefa.DataConclusao = DateTime.Now;

        _db.TarefasFaculdade.Update(Tarefa);
        await _db.SaveChangesAsync();

        // Gera pendência automática quando marcada como Parcial ou Não Encaixou
        if (DiasHelper.GerarPendencia(Tarefa.Status) && !DiasHelper.GerarPendencia(statusAnterior))
        {
            var tarefa = await _db.TarefasFaculdade.Include(t => t.Materia).FirstAsync(t => t.Id == Tarefa.Id);
            _db.Pendencias.Add(new Pendencia
            {
                Descricao = $"[{DiasHelper.LabelStatus(Tarefa.Status)}] {tarefa.Titulo} ({tarefa.Materia.Nome})",
                Origem = "Faculdade",
                TarefaFaculdadeId = Tarefa.Id,
                DataCriacao = DateTime.Now,
                DataLimite = Tarefa.DataHoraLimite.AddDays(3)
            });
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Status atualizado. Pendência criada automaticamente.";
        }
        else
        {
            TempData["Sucesso"] = "Tarefa atualizada.";
        }

        return RedirectToPage("Index");
    }
}
