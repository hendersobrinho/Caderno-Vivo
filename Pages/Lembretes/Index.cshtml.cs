using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Lembretes;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Lembrete> LembretesDestaque { get; set; } = [];
    public List<Lembrete> LembretesAbertos { get; set; } = [];
    public List<Lembrete> LembretesConcluidos { get; set; } = [];
    public List<Materia> Materias { get; set; } = [];
    public List<Projeto> Projetos { get; set; } = [];

    [BindProperty]
    public Lembrete NovoLembrete { get; set; } = new();

    [BindProperty]
    public string? VinculoSelecionado { get; set; }

    public async Task OnGetAsync()
    {
        await CarregarDados();
    }

    public async Task<IActionResult> OnPostCriarAsync()
    {
        if (string.IsNullOrWhiteSpace(NovoLembrete.Titulo))
        {
            TempData["Erro"] = "Escreva pelo menos o titulo do lembrete.";
            return RedirectToPage();
        }

        AplicarVinculo(NovoLembrete, VinculoSelecionado);
        NovoLembrete.Titulo = NovoLembrete.Titulo.Trim();
        NovoLembrete.Descricao = NovoLembrete.Descricao?.Trim();
        NovoLembrete.Status = StatusLembrete.Aberto;
        NovoLembrete.DataCriacao = DateTime.Now;

        _db.Lembretes.Add(NovoLembrete);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Lembrete criado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostConcluirAsync(int id)
    {
        var lembrete = await _db.Lembretes.FindAsync(id);
        if (lembrete == null) return NotFound();

        lembrete.Status = StatusLembrete.Concluido;
        lembrete.DataConclusao = DateTime.Now;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{lembrete.Titulo}\" concluido.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReabrirAsync(int id)
    {
        var lembrete = await _db.Lembretes.FindAsync(id);
        if (lembrete == null) return NotFound();

        lembrete.Status = StatusLembrete.Aberto;
        lembrete.DataConclusao = null;
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArquivarAsync(int id)
    {
        var lembrete = await _db.Lembretes.FindAsync(id);
        if (lembrete == null) return NotFound();

        lembrete.Status = StatusLembrete.Arquivado;
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAlternarDestaqueAsync(int id)
    {
        var lembrete = await _db.Lembretes.FindAsync(id);
        if (lembrete == null) return NotFound();

        lembrete.Destaque = !lembrete.Destaque;
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAdiarAsync(int id, string destino)
    {
        var lembrete = await _db.Lembretes.FindAsync(id);
        if (lembrete == null) return NotFound();

        var hoje = DateTime.Today;
        lembrete.DataLimite = destino == "sabado"
            ? ProximoSabado(hoje)
            : hoje.AddDays(1);
        lembrete.Status = StatusLembrete.Aberto;

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{lembrete.Titulo}\" movido para {lembrete.DataLimite:dd/MM}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var lembrete = await _db.Lembretes.FindAsync(id);
        if (lembrete != null)
        {
            _db.Lembretes.Remove(lembrete);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task CarregarDados()
    {
        var baseQuery = _db.Lembretes
            .Include(l => l.Materia)
            .Include(l => l.Projeto);

        var abertos = await baseQuery
            .Where(l => l.Status == StatusLembrete.Aberto)
            .OrderByDescending(l => l.Destaque)
            .ThenBy(l => l.DataLimite == null)
            .ThenBy(l => l.DataLimite)
            .ThenByDescending(l => l.Prioridade)
            .ThenByDescending(l => l.DataCriacao)
            .ToListAsync();

        LembretesDestaque = abertos
            .Where(l => l.Destaque || l.Prioridade >= PrioridadeProjeto.Alta || l.DiasRestantes <= 1)
            .Take(6)
            .ToList();

        LembretesAbertos = abertos;

        LembretesConcluidos = await baseQuery
            .Where(l => l.Status == StatusLembrete.Concluido)
            .OrderByDescending(l => l.DataConclusao)
            .Take(8)
            .ToListAsync();

        Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();
        Projetos = await _db.Projetos
            .Where(p => p.Status != StatusProjeto.Concluido)
            .OrderBy(p => p.DataLimite == null)
            .ThenBy(p => p.DataLimite)
            .ToListAsync();
    }

    private static void AplicarVinculo(Lembrete lembrete, string? vinculoSelecionado)
    {
        lembrete.Escopo = EscopoLembrete.Avulso;
        lembrete.MateriaId = null;
        lembrete.ProjetoId = null;
        lembrete.ArtigoId = null;

        if (string.IsNullOrWhiteSpace(vinculoSelecionado))
            return;

        var partes = vinculoSelecionado.Split(':', 2);
        if (partes.Length != 2 || !int.TryParse(partes[1], out var id))
            return;

        var tipo = partes[0];
        switch (tipo)
        {
            case "materia":
                lembrete.Escopo = EscopoLembrete.Faculdade;
                lembrete.MateriaId = id;
                break;
            case "projeto":
                lembrete.Escopo = EscopoLembrete.Projeto;
                lembrete.ProjetoId = id;
                break;
        }
    }

    private static DateTime ProximoSabado(DateTime hoje)
    {
        var diasAteSabado = ((int)DayOfWeek.Saturday - (int)hoje.DayOfWeek + 7) % 7;
        return hoje.AddDays(diasAteSabado == 0 ? 7 : diasAteSabado).Date;
    }
}
