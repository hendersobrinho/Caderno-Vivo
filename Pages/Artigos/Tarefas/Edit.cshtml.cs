using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Artigos.Tarefas;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public TarefaArtigo Tarefa { get; set; } = null!;

    public List<Artigo> Artigos { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var tarefa = await _db.TarefasArtigo.FindAsync(id);
        if (tarefa == null) return NotFound();
        Tarefa = tarefa;
        Artigos = await _db.Artigos.OrderBy(a => a.Titulo).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Artigos = await _db.Artigos.OrderBy(a => a.Titulo).ToListAsync();
            return Page();
        }

        var statusAnterior = await _db.TarefasArtigo
            .AsNoTracking()
            .Where(t => t.Id == Tarefa.Id)
            .Select(t => t.Status)
            .FirstOrDefaultAsync();

        if (Tarefa.Status == StatusTarefa.Concluida && Tarefa.DataConclusao == null)
            Tarefa.DataConclusao = DateTime.Now;

        _db.TarefasArtigo.Update(Tarefa);
        await _db.SaveChangesAsync();

        if (DiasHelper.GerarPendencia(Tarefa.Status) && !DiasHelper.GerarPendencia(statusAnterior))
        {
            var tarefa = await _db.TarefasArtigo.Include(t => t.Artigo).FirstAsync(t => t.Id == Tarefa.Id);
            _db.Pendencias.Add(new Pendencia
            {
                Descricao = $"[{DiasHelper.LabelStatus(Tarefa.Status)}] {tarefa.Titulo} ({tarefa.Artigo.Titulo})",
                Origem = "Artigo",
                TarefaArtigoId = Tarefa.Id,
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
