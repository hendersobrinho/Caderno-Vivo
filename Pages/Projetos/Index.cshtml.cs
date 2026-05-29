using System.Text.Json;
using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Projetos;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<Projeto> Projetos { get; set; } = [];
    public List<Materia> Materias { get; set; } = [];

    public int TotalAtivos { get; set; }
    public int TotalUrgentes { get; set; }
    public int TotalConcluidos { get; set; }

    [BindProperty] public Projeto NovoProjeto { get; set; } = new();
    [BindProperty] public string? ChecklistTexto { get; set; }

    public string? FiltroStatus { get; set; }

    public async Task OnGetAsync(string? status)
    {
        FiltroStatus = status;
        await CarregarDados(status);
    }

    public async Task<IActionResult> OnPostCriarAsync()
    {
        if (string.IsNullOrWhiteSpace(NovoProjeto.Titulo))
        {
            TempData["Erro"] = "Título é obrigatório.";
            return RedirectToPage();
        }

        NovoProjeto.ChecklistJson = ParseChecklist(ChecklistTexto);
        NovoProjeto.DataCriacao = DateTime.Now;
        _db.Projetos.Add(NovoProjeto);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Projeto \"{NovoProjeto.Titulo}\" criado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleItemAsync(int id, int idx)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null) return NotFound();

        var itens = p.Checklist;
        if (idx >= 0 && idx < itens.Count)
        {
            itens[idx].Feito = !itens[idx].Feito;
            p.ChecklistJson = JsonSerializer.Serialize(itens);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { status = FiltroStatus });
    }

    public async Task<IActionResult> OnPostAddItemAsync(int id, string texto)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null || string.IsNullOrWhiteSpace(texto))
            return RedirectToPage(new { status = FiltroStatus });

        var itens = p.Checklist;
        itens.Add(new ChecklistItem { Texto = texto.Trim(), Feito = false });
        p.ChecklistJson = JsonSerializer.Serialize(itens);
        await _db.SaveChangesAsync();
        return RedirectToPage(new { status = FiltroStatus });
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p != null)
        {
            _db.Projetos.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Projeto excluído.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMudarStatusAsync(int id, StatusProjeto status)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null) return RedirectToPage();

        var statusAnterior = p.Status;
        p.Status = status;

        if (p.TemCronograma)
        {
            if (status == StatusProjeto.Pausado)
            {
                // Remove blocos futuros agendados ao pausar
                var futuros = await _db.BloquesEstudo
                    .Where(b => b.ProjetoId == id &&
                                b.Status == StatusBloco.Agendado &&
                                b.Data >= DateTime.Today)
                    .ToListAsync();
                _db.BloquesEstudo.RemoveRange(futuros);
            }
            else if (status == StatusProjeto.EmAndamento &&
                     statusAnterior == StatusProjeto.Pausado)
            {
                // Retomada: reagenda todas as sessões restantes
                var cronograma = p.Cronograma;
                if (cronograma != null)
                    await CronogramaHelper.AgendarSessoes(_db, p, cronograma);
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task CarregarDados(string? status)
    {
        var hoje = DateTime.Today;

        TotalAtivos = await _db.Projetos.CountAsync(p => p.Status != StatusProjeto.Concluido);
        TotalConcluidos = await _db.Projetos.CountAsync(p => p.Status == StatusProjeto.Concluido);
        TotalUrgentes = await _db.Projetos.CountAsync(p =>
            p.Status != StatusProjeto.Concluido &&
            p.DataLimite.HasValue &&
            p.DataLimite.Value.Date <= hoje.AddDays(3));

        var query = _db.Projetos.Include(p => p.Materia).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusProjeto>(status, out var s))
            query = query.Where(p => p.Status == s);

        Projetos = await query
            .OrderBy(p => p.Status == StatusProjeto.Concluido)
            .ThenBy(p => p.DataLimite == null)
            .ThenBy(p => p.DataLimite)
            .ThenByDescending(p => p.Prioridade)
            .ToListAsync();

        Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();
    }

    private static string? ParseChecklist(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var itens = texto
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim().TrimStart('-', '*', '•').Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Select(l => new ChecklistItem { Texto = l, Feito = false })
            .ToList();
        return itens.Count == 0 ? null : JsonSerializer.Serialize(itens);
    }
}
