using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Faculdade.PlanoDaSemana;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public string? Inicio { get; set; }

    public List<Materia> Materias { get; set; } = [];
    public List<TarefaFaculdade> TarefasSemana { get; set; } = [];
    public List<BlocoEstudo> BlocosSemana { get; set; } = [];
    public Dictionary<DateTime, List<BlocoEstudo>> BlocosPorDia { get; set; } = [];
    public DateTime InicioSemana { get; set; }
    public DateTime FimSemana { get; set; }
    public DateTime InicioSemanaAtual { get; set; }
    public bool SemanaAtual { get; set; }
    public int BlocosConcluidos { get; set; }
    public int BlocosPendentes { get; set; }
    public int BlocosAtrasados { get; set; }
    public int ProgressoSemana { get; set; }
    public int MinutosPlanejados { get; set; }

    // Grade[dia] = lista de (aula, materia)
    public Dictionary<int, List<(AulaSemanal Aula, string MateriaNome)>> Grade { get; set; } = [];

    public async Task OnGetAsync()
    {
        InicioSemana = ResolverInicioSemana(Inicio);
        FimSemana = InicioSemana.AddDays(6);
        InicioSemanaAtual = ResolverInicioSemana(null);
        SemanaAtual = InicioSemana == InicioSemanaAtual;

        Materias = await _db.Materias
            .Include(m => m.AulasSemanais)
            .ToListAsync();

        for (int i = 0; i < 7; i++)
            Grade[i] = [];

        foreach (var m in Materias)
            foreach (var a in m.AulasSemanais)
                Grade[a.DiaSemana].Add((a, m.Nome));

        TarefasSemana = await _db.TarefasFaculdade
            .Include(t => t.Materia)
            .Where(t => t.DataHoraLimite >= InicioSemana && t.DataHoraLimite < FimSemana.AddDays(1))
            .OrderBy(t => t.DataHoraLimite)
            .ToListAsync();

        BlocosSemana = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => b.Data.Date >= InicioSemana && b.Data.Date <= FimSemana)
            .OrderBy(b => b.Data)
            .ThenBy(b => b.HoraInicio)
            .ToListAsync();

        for (var data = InicioSemana.Date; data <= FimSemana.Date; data = data.AddDays(1))
            BlocosPorDia[data] = [];

        foreach (var bloco in BlocosSemana)
            BlocosPorDia[bloco.Data.Date].Add(bloco);

        BlocosConcluidos = BlocosSemana.Count(b => b.Status == StatusBloco.Concluido);
        BlocosPendentes = BlocosSemana.Count(b => b.Status != StatusBloco.Concluido && b.Status != StatusBloco.NaoFeito);
        BlocosAtrasados = BlocosSemana.Count(b =>
            b.Data.Date < DateTime.Today &&
            b.Status != StatusBloco.Concluido &&
            b.Status != StatusBloco.NaoFeito);
        ProgressoSemana = BlocosSemana.Count == 0
            ? 0
            : (int)Math.Round((double)BlocosConcluidos / BlocosSemana.Count * 100);
        MinutosPlanejados = BlocosSemana.Sum(CalcularMinutos);
    }

    public async Task<IActionResult> OnPostAjustarBlocoAsync(
        int id, DateTime data, string horaInicio, string horaFim, string titulo, string? descricao)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        if (string.IsNullOrWhiteSpace(titulo) ||
            string.IsNullOrWhiteSpace(horaInicio) ||
            string.IsNullOrWhiteSpace(horaFim))
        {
            TempData["Erro"] = "Data, horario e titulo sao obrigatorios para ajustar o bloco.";
            return RedirectToPage(new { inicio = Inicio });
        }

        bloco.Data = data.Date;
        bloco.HoraInicio = horaInicio;
        bloco.HoraFim = horaFim;
        bloco.Titulo = titulo.Trim();
        bloco.Descricao = descricao?.Trim();

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Bloco \"{bloco.Titulo}\" ajustado.";
        var inicioDestino = data.Date.AddDays(-(((int)data.DayOfWeek + 6) % 7));
        return RedirectToPage(new { inicio = inicioDestino.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostExcluirSelecionadosAsync(List<int> blocoIds)
    {
        if (blocoIds.Count == 0)
        {
            TempData["Erro"] = "Selecione pelo menos um bloco para excluir.";
            return RedirectToPage(new { inicio = Inicio });
        }

        var blocos = await _db.BloquesEstudo
            .Where(b => blocoIds.Contains(b.Id))
            .ToListAsync();

        _db.BloquesEstudo.RemoveRange(blocos);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"{blocos.Count} bloco(s) excluido(s) da semana.";
        return RedirectToPage(new { inicio = Inicio });
    }

    private static DateTime ResolverInicioSemana(string? inicio)
    {
        if (DateTime.TryParse(inicio, out var data))
            return data.Date;

        var hoje = DateTime.Today;
        var deslocamento = ((int)hoje.DayOfWeek + 6) % 7; // segunda = 0
        return hoje.AddDays(-deslocamento).Date;
    }

    private static int CalcularMinutos(BlocoEstudo bloco)
    {
        if (!TimeSpan.TryParse(bloco.HoraInicio, out var inicio) ||
            !TimeSpan.TryParse(bloco.HoraFim, out var fim))
            return 0;

        var duracao = fim - inicio;
        return duracao.TotalMinutes > 0 ? (int)Math.Round(duracao.TotalMinutes) : 0;
    }
}
