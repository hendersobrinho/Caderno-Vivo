using System.Text.Json;
using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Projetos;

public class DetalhesModel : PageModel
{
    private readonly AppDbContext _db;
    public DetalhesModel(AppDbContext db) => _db = db;

    public Projeto Projeto { get; set; } = null!;
    public List<Materia> Materias { get; set; } = [];
    public List<BlocoEstudo> BloquesVinculados { get; set; } = [];
    public int SessoesConcluidas { get; set; }

    [BindProperty] public Projeto ProjetoEdit { get; set; } = new();
    [BindProperty] public string? ChecklistTexto { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var p = await _db.Projetos.Include(x => x.Materia).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        Projeto = p;
        ProjetoEdit = p;
        ChecklistTexto = ChecklistParaTexto(p.Checklist);
        Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();
        BloquesVinculados = await _db.BloquesEstudo
            .Where(b => b.ProjetoId == id)
            .OrderByDescending(b => b.Data)
            .ToListAsync();
        SessoesConcluidas = BloquesVinculados.Count(b =>
            b.Status == StatusBloco.Concluido || b.Status == StatusBloco.Parcial);
        return Page();
    }

    public async Task<IActionResult> OnPostSalvarAsync(int id)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null) return NotFound();

        var statusAnterior = p.Status;

        p.Titulo = ProjetoEdit.Titulo;
        p.Descricao = ProjetoEdit.Descricao;
        p.DataLimite = ProjetoEdit.DataLimite;
        p.Prioridade = ProjetoEdit.Prioridade;
        p.Status = ProjetoEdit.Status;
        p.Categoria = ProjetoEdit.Categoria;
        p.MateriaId = ProjetoEdit.MateriaId;
        p.Observacoes = ProjetoEdit.Observacoes;

        if (p.TemCronograma && statusAnterior != p.Status)
        {
            if (p.Status == StatusProjeto.Pausado)
            {
                var futuros = await _db.BloquesEstudo
                    .Where(b => b.ProjetoId == id &&
                                b.Status == StatusBloco.Agendado &&
                                b.Data >= DateTime.Today)
                    .ToListAsync();
                _db.BloquesEstudo.RemoveRange(futuros);
            }
            else if (p.Status == StatusProjeto.EmAndamento &&
                     statusAnterior == StatusProjeto.Pausado)
            {
                var cronograma = p.Cronograma;
                if (cronograma != null)
                    await CronogramaHelper.AgendarSessoes(_db, p, cronograma);
            }
        }

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Projeto atualizado.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPausarAsync(int id)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null) return NotFound();

        p.Status = StatusProjeto.Pausado;

        if (p.TemCronograma)
        {
            var futuros = await _db.BloquesEstudo
                .Where(b => b.ProjetoId == id &&
                            b.Status == StatusBloco.Agendado &&
                            b.Data >= DateTime.Today)
                .ToListAsync();
            _db.BloquesEstudo.RemoveRange(futuros);
        }

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Projeto pausado. Blocos futuros removidos da fila.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRetomarAsync(int id)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null) return NotFound();

        p.Status = StatusProjeto.EmAndamento;

        int agendadas = 0;
        if (p.TemCronograma)
        {
            var cronograma = p.Cronograma;
            if (cronograma != null)
                agendadas = await CronogramaHelper.AgendarSessoes(_db, p, cronograma);
        }

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = agendadas > 0
            ? $"Projeto retomado. {agendadas} sessão(ões) reagendadas a partir de amanhã."
            : "Projeto retomado.";
        return RedirectToPage(new { id });
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
            AtualizarStatusAutomatico(p, itens);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddItemAsync(int id, string texto)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null || string.IsNullOrWhiteSpace(texto)) return RedirectToPage(new { id });

        var itens = p.Checklist;
        itens.Add(new ChecklistItem { Texto = texto.Trim(), Feito = false });
        p.ChecklistJson = JsonSerializer.Serialize(itens);
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveItemAsync(int id, int idx)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null) return NotFound();

        var itens = p.Checklist;
        if (idx >= 0 && idx < itens.Count)
        {
            itens.RemoveAt(idx);
            p.ChecklistJson = itens.Count == 0 ? null : JsonSerializer.Serialize(itens);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAdicionarBlocoAsync(
        int id, DateTime data, string horaInicio, string horaFim, string titulo, string? descricao)
    {
        var p = await _db.Projetos.FindAsync(id);
        if (p == null) return NotFound();

        if (data == default ||
            string.IsNullOrWhiteSpace(horaInicio) ||
            string.IsNullOrWhiteSpace(horaFim) ||
            string.IsNullOrWhiteSpace(titulo))
        {
            TempData["Erro"] = "Informe data, horario e titulo para criar o bloco do projeto.";
            return RedirectToPage(new { id });
        }

        _db.BloquesEstudo.Add(new BlocoEstudo
        {
            Data = data.Date,
            HoraInicio = horaInicio.Trim(),
            HoraFim = horaFim.Trim(),
            Titulo = titulo.Trim(),
            Descricao = descricao?.Trim(),
            Modulo = "Projeto",
            ProjetoId = p.Id,
            MateriaId = p.MateriaId,
            Status = StatusBloco.Agendado
        });

        if (p.Status == StatusProjeto.AFazer)
            p.Status = StatusProjeto.EmAndamento;

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Bloco de estudo criado para este projeto.";
        return RedirectToPage(new { id });
    }

    private static void AtualizarStatusAutomatico(Projeto p, List<ChecklistItem> itens)
    {
        if (itens.Count == 0) return;
        if (p.Status == StatusProjeto.Concluido) return; // não reverter manualmente concluído

        var feitos = itens.Count(i => i.Feito);
        if (feitos == itens.Count)
            p.Status = StatusProjeto.Concluido;
        else if (feitos > 0 && p.Status == StatusProjeto.AFazer)
            p.Status = StatusProjeto.EmAndamento;
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

    private static string ChecklistParaTexto(List<ChecklistItem> itens) =>
        string.Join('\n', itens.Select(i => i.Texto));
}
