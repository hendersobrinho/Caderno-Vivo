using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public int TotalMaterias { get; set; }
    public int TarefasFaculdadePendentes { get; set; }
    public int TotalArtigos { get; set; }
    public int ArtigosEmAndamento { get; set; }
    public int PendenciasAbertas { get; set; }
    public int DuvidasAbertas { get; set; }

    public List<TarefaFaculdade> ProximasTarefasFaculdade { get; set; } = [];
    public List<TarefaArtigo> ProximasTarefasArtigo { get; set; } = [];
    public List<Pendencia> UltimasPendencias { get; set; } = [];
    public List<Artigo> ArtigosEmProgresso { get; set; } = [];

    public async Task OnGetAsync()
    {
        var hoje = DateTime.Now;
        var em7Dias = hoje.AddDays(7);

        TotalMaterias = await _db.Materias.CountAsync();
        TarefasFaculdadePendentes = await _db.TarefasFaculdade
            .CountAsync(t => t.Status == StatusTarefa.Pendente || t.Status == StatusTarefa.EmAndamento);
        TotalArtigos = await _db.Artigos.CountAsync();
        ArtigosEmAndamento = await _db.Artigos
            .CountAsync(a => a.Status == StatusArtigo.EmDesenvolvimento || a.Status == StatusArtigo.Revisao);
        PendenciasAbertas = await _db.Pendencias.CountAsync(p => !p.Resolvida);
        DuvidasAbertas = await _db.DuvidasFaculdade.CountAsync(d => !d.Resolvida)
                       + await _db.DuvidasArtigo.CountAsync(d => !d.Resolvida);

        ProximasTarefasFaculdade = await _db.TarefasFaculdade
            .Include(t => t.Materia)
            .Where(t => t.Status != StatusTarefa.Concluida && t.DataHoraLimite <= em7Dias)
            .OrderBy(t => t.DataHoraLimite)
            .Take(5)
            .ToListAsync();

        ProximasTarefasArtigo = await _db.TarefasArtigo
            .Include(t => t.Artigo)
            .Where(t => t.Status != StatusTarefa.Concluida && t.DataHoraLimite <= em7Dias)
            .OrderBy(t => t.DataHoraLimite)
            .Take(5)
            .ToListAsync();

        UltimasPendencias = await _db.Pendencias
            .Where(p => !p.Resolvida)
            .OrderByDescending(p => p.DataCriacao)
            .Take(5)
            .ToListAsync();

        ArtigosEmProgresso = await _db.Artigos
            .Where(a => a.Status == StatusArtigo.EmDesenvolvimento || a.Status == StatusArtigo.Revisao)
            .OrderByDescending(a => a.DataAtualizacao)
            .Take(4)
            .ToListAsync();
    }
}
