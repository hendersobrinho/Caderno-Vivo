using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Faculdade.Materias;

public class DetalhesModel : PageModel
{
    private readonly AppDbContext _db;

    public DetalhesModel(AppDbContext db) => _db = db;

    public Materia Materia { get; set; } = null!;

    [BindProperty] public AulaSemanal NovaAula { get; set; } = new();
    [BindProperty] public Material NovoMaterial { get; set; } = new();
    [BindProperty] public TarefaFaculdade NovaTarefa { get; set; } = new();
    [BindProperty] public HistoricoAula NovoHistorico { get; set; } = new();
    [BindProperty] public DuvidaFaculdade NovaDuvida { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var materia = await CarregarMateria(id);
        if (materia == null) return NotFound();
        Materia = materia;
        return Page();
    }

    public async Task<IActionResult> OnPostAddAulaAsync(int id)
    {
        var materia = await CarregarMateria(id);
        if (materia == null) return NotFound();
        Materia = materia;

        NovaAula.MateriaId = id;
        _db.AulasSemanais.Add(NovaAula);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Aula adicionada.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoverAulaAsync(int id, int aulaId)
    {
        var aula = await _db.AulasSemanais.FindAsync(aulaId);
        if (aula != null) { _db.AulasSemanais.Remove(aula); await _db.SaveChangesAsync(); }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddMaterialAsync(int id)
    {
        var materia = await CarregarMateria(id);
        if (materia == null) return NotFound();
        Materia = materia;

        NovoMaterial.MateriaId = id;
        NovoMaterial.DataCriacao = DateTime.Now;
        _db.Materiais.Add(NovoMaterial);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Material adicionado.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoverMaterialAsync(int id, int materialId)
    {
        var mat = await _db.Materiais.FindAsync(materialId);
        if (mat != null) { _db.Materiais.Remove(mat); await _db.SaveChangesAsync(); }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddTarefaAsync(int id)
    {
        var materia = await CarregarMateria(id);
        if (materia == null) return NotFound();
        Materia = materia;

        NovaTarefa.MateriaId = id;
        NovaTarefa.DataCriacao = DateTime.Now;
        _db.TarefasFaculdade.Add(NovaTarefa);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tarefa adicionada.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddHistoricoAsync(int id)
    {
        var materia = await CarregarMateria(id);
        if (materia == null) return NotFound();
        Materia = materia;

        NovoHistorico.MateriaId = id;
        _db.HistoricoAulas.Add(NovoHistorico);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Registro adicionado ao histórico.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoverHistoricoAsync(int id, int historicoId)
    {
        var h = await _db.HistoricoAulas.FindAsync(historicoId);
        if (h != null) { _db.HistoricoAulas.Remove(h); await _db.SaveChangesAsync(); }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddDuvidaAsync(int id)
    {
        var materia = await CarregarMateria(id);
        if (materia == null) return NotFound();
        Materia = materia;

        NovaDuvida.MateriaId = id;
        NovaDuvida.DataRegistro = DateTime.Now;
        _db.DuvidasFaculdade.Add(NovaDuvida);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Dúvida registrada.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResolverDuvidaAsync(int id, int duvidaId, string resolucao)
    {
        var duvida = await _db.DuvidasFaculdade.FindAsync(duvidaId);
        if (duvida != null)
        {
            duvida.Resolvida = true;
            duvida.Resolucao = resolucao;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    private async Task<Materia?> CarregarMateria(int id) =>
        await _db.Materias
            .Include(m => m.AulasSemanais)
            .Include(m => m.Materiais)
            .Include(m => m.Tarefas)
            .Include(m => m.Historico)
            .Include(m => m.Duvidas)
            .FirstOrDefaultAsync(m => m.Id == id);
}
