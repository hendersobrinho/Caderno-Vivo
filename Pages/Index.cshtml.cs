using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
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
    public List<Projeto> ProjetosUrgentes { get; set; } = [];
    public List<BlocoEstudo> BlocosHoje { get; set; } = [];
    public List<BlocoEstudo> ProximosBlocos { get; set; } = [];
    public List<BlocoEstudo> BlocosAtrasados { get; set; } = [];
    public List<BlocoEstudo> BlocosSemana { get; set; } = [];
    public List<BlocoEstudo> ObservacoesRecentes { get; set; } = [];
    public List<Lembrete> LembretesImportantes { get; set; } = [];
    public int TotalProjetos { get; set; }
    public int BlocosPlanejados { get; set; }
    public int BlocosSemanaTotal { get; set; }
    public int BlocosSemanaConcluidos { get; set; }
    public int BlocosHojeTotal { get; set; }
    public int BlocosHojeConcluidos { get; set; }
    public int TotalBlocosAtrasados { get; set; }
    public int MinutosSemanaPlanejados { get; set; }
    public int MinutosSemanaConcluidos { get; set; }
    public int ConcluidosNoPrazo { get; set; }
    public int ConcluidosAntecipados { get; set; }
    public int ConcluidosDepoisPrazo { get; set; }
    public int HorasExtrasFeitas { get; set; }
    public int MinutosExtrasFeitos { get; set; }
    public int MediaMinutosPorBloco { get; set; }
    public int ProgressoSemana { get; set; }
    public int ProgressoHoje { get; set; }
    public DateTime InicioSemana { get; set; }
    public DateTime FimSemana { get; set; }
    public DateTime? ProximaDataComBlocos { get; set; }
    public List<DashboardDia> DiasDaSemana { get; set; } = [];

    public async Task OnGetAsync()
    {
        var hoje = DateTime.Today;
        InicioSemana = hoje.AddDays(-(((int)hoje.DayOfWeek + 6) % 7)).Date;
        FimSemana = InicioSemana.AddDays(6);
        var em7Dias = DateTime.Now.AddDays(7);

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

        TotalProjetos = await _db.Projetos.CountAsync(p => p.Status != StatusProjeto.Concluido);
        ProjetosUrgentes = await _db.Projetos
            .Include(p => p.Materia)
            .Where(p => p.Status != StatusProjeto.Concluido)
            .OrderBy(p => p.DataLimite == null)
            .ThenBy(p => p.DataLimite)
            .ThenByDescending(p => p.Prioridade)
            .Take(6)
            .ToListAsync();
        
        BlocosHoje = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => b.Data.Date == hoje)
            .OrderBy(b => b.HoraInicio)
            .Take(5)
            .ToListAsync();

        ProximosBlocos = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => b.Data.Date >= hoje && b.Status != StatusBloco.Concluido)
            .OrderBy(b => b.Data)
            .ThenBy(b => b.HoraInicio)
            .Take(5)
            .ToListAsync();

        BlocosAtrasados = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => b.Data.Date < hoje &&
                        b.Status != StatusBloco.Concluido &&
                        b.Status != StatusBloco.NaoFeito)
            .OrderBy(b => b.Data)
            .ThenBy(b => b.HoraInicio)
            .Take(8)
            .ToListAsync();

        TotalBlocosAtrasados = await _db.BloquesEstudo
            .CountAsync(b => b.Data.Date < hoje &&
                             b.Status != StatusBloco.Concluido &&
                             b.Status != StatusBloco.NaoFeito);

        BlocosSemana = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => b.Data.Date >= InicioSemana && b.Data.Date <= FimSemana)
            .OrderBy(b => b.Data)
            .ThenBy(b => b.HoraInicio)
            .ToListAsync();

        BlocosSemanaTotal = BlocosSemana.Count;
        BlocosSemanaConcluidos = BlocosSemana.Count(b => b.Status == StatusBloco.Concluido);
        BlocosHojeTotal = await _db.BloquesEstudo.CountAsync(b => b.Data.Date == hoje);
        BlocosHojeConcluidos = await _db.BloquesEstudo.CountAsync(b =>
            b.Data.Date == hoje && b.Status == StatusBloco.Concluido);

        MinutosSemanaPlanejados = BlocosSemana.Sum(CalcularMinutos);
        MinutosSemanaConcluidos = BlocosSemana
            .Where(b => b.Status == StatusBloco.Concluido)
            .Sum(CalcularMinutosGastos);

        ConcluidosNoPrazo = BlocosSemana.Count(ConcluidoNoPrazo);
        ConcluidosAntecipados = BlocosSemana.Count(ConcluidoAntecipado);
        ConcluidosDepoisPrazo = BlocosSemana.Count(ConcluidoDepoisDoPrazo);
        HorasExtrasFeitas = BlocosSemana.Count(TemHoraExtra);
        MinutosExtrasFeitos = BlocosSemana.Sum(CalcularMinutosExtras);

        // Média usa todo o histórico, não só a semana
        var concluidosTodos = await _db.BloquesEstudo
            .Where(b => b.Status == StatusBloco.Concluido)
            .ToListAsync();
        MediaMinutosPorBloco = concluidosTodos.Count == 0
            ? 0
            : (int)Math.Round(concluidosTodos.Average(b => (double)CalcularMinutosGastos(b)));

        ProgressoSemana = BlocosSemanaTotal == 0
            ? 0
            : (int)Math.Round((double)BlocosSemanaConcluidos / BlocosSemanaTotal * 100);
        ProgressoHoje = BlocosHojeTotal == 0
            ? 0
            : (int)Math.Round((double)BlocosHojeConcluidos / BlocosHojeTotal * 100);

        BlocosPlanejados = await _db.BloquesEstudo
            .CountAsync(b => b.Data.Date >= hoje && b.Status == StatusBloco.Agendado);
        ProximaDataComBlocos = ProximosBlocos.FirstOrDefault()?.Data.Date;

        ObservacoesRecentes = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => !string.IsNullOrWhiteSpace(b.Observacao) ||
                        !string.IsNullOrWhiteSpace(b.Duvidas) ||
                        !string.IsNullOrWhiteSpace(b.OndeParei) ||
                        !string.IsNullOrWhiteSpace(b.ProximoPasso))
            .OrderByDescending(b => b.Data)
            .ThenByDescending(b => b.HoraInicio)
            .Take(5)
            .ToListAsync();

        LembretesImportantes = await _db.Lembretes
            .Include(l => l.Materia)
            .Include(l => l.Projeto)
            .Include(l => l.Artigo)
            .Where(l => l.Status == StatusLembrete.Aberto)
            .OrderByDescending(l => l.Destaque)
            .ThenBy(l => l.DataLimite == null)
            .ThenBy(l => l.DataLimite)
            .ThenByDescending(l => l.Prioridade)
            .Take(4)
            .ToListAsync();

        DiasDaSemana = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var dia = InicioSemana.AddDays(offset).Date;
                var blocos = BlocosSemana.Where(b => b.Data.Date == dia).ToList();
                var total = blocos.Count;
                var concluidos = blocos.Count(b => b.Status == StatusBloco.Concluido);
                var progresso = total == 0 ? 0 : (int)Math.Round((double)concluidos / total * 100);
                return new DashboardDia(dia, total, concluidos, progresso);
            })
            .ToList();
    }

    public async Task<IActionResult> OnPostAdiarBlocoAsync(int id, string destino)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        var hoje = DateTime.Today;
        bloco.Data = destino == "sabado"
            ? ProximoSabado(hoje)
            : hoje.AddDays(1);
        bloco.Status = StatusBloco.Agendado;
        bloco.IniciadoEm = null;
        bloco.SegundosPausados = 0;
        bloco.SegundosGastos = null;
        bloco.PausadoEm = null;

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{bloco.Titulo}\" reagendado para {bloco.Data:dd/MM}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarcarNaoFeitoAsync(int id)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        bloco.Status = StatusBloco.NaoFeito;
        bloco.IniciadoEm = null;
        bloco.SegundosPausados = 0;
        bloco.SegundosGastos = null;
        bloco.PausadoEm = null;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{bloco.Titulo}\" marcado como não feito.";
        return RedirectToPage();
    }

    private static DateTime ProximoSabado(DateTime hoje)
    {
        var diasAteSabado = ((int)DayOfWeek.Saturday - (int)hoje.DayOfWeek + 7) % 7;
        return hoje.AddDays(diasAteSabado == 0 ? 7 : diasAteSabado).Date;
    }

    private static int CalcularMinutos(BlocoEstudo bloco)
    {
        if (!TimeSpan.TryParse(bloco.HoraInicio, out var inicio) ||
            !TimeSpan.TryParse(bloco.HoraFim, out var fim))
            return 0;

        if (fim <= inicio)
            fim = fim.Add(TimeSpan.FromDays(1));

        var duracao = fim - inicio;
        return duracao.TotalMinutes > 0 ? (int)Math.Round(duracao.TotalMinutes) : 0;
    }

    private static int CalcularMinutosGastos(BlocoEstudo bloco)
    {
        if (bloco.SegundosGastos.HasValue)
            return Math.Max(0, (int)Math.Round(bloco.SegundosGastos.Value / 60d));

        return CalcularMinutos(bloco);
    }

    private static bool ConcluidoNoPrazo(BlocoEstudo bloco)
    {
        if (bloco.Status != StatusBloco.Concluido || !bloco.DataConclusao.HasValue)
            return false;

        return bloco.DataConclusao.Value >= bloco.Data.Date &&
               bloco.DataConclusao.Value <= PrazoFinal(bloco);
    }

    private static bool ConcluidoAntecipado(BlocoEstudo bloco)
    {
        return bloco.Status == StatusBloco.Concluido &&
               bloco.DataConclusao.HasValue &&
               bloco.DataConclusao.Value.Date < bloco.Data.Date;
    }

    private static bool ConcluidoDepoisDoPrazo(BlocoEstudo bloco)
    {
        return bloco.Status == StatusBloco.Concluido &&
               bloco.DataConclusao.HasValue &&
               bloco.DataConclusao.Value > PrazoFinal(bloco);
    }

    private static bool TemHoraExtra(BlocoEstudo bloco)
    {
        return bloco.MinutosExtras > 0 || bloco.Status == StatusBloco.HoraExtra;
    }

    private static int CalcularMinutosExtras(BlocoEstudo bloco)
    {
        if (bloco.MinutosExtras > 0)
            return bloco.MinutosExtras;

        return bloco.Status == StatusBloco.HoraExtra
            ? CalcularMinutosGastos(bloco)
            : 0;
    }

    private static DateTime PrazoFinal(BlocoEstudo bloco)
    {
        if (!TimeSpan.TryParse(bloco.HoraInicio, out var inicio) ||
            !TimeSpan.TryParse(bloco.HoraFim, out var fim))
            return bloco.Data.Date.AddDays(1).AddTicks(-1);

        var prazo = bloco.Data.Date.Add(fim);
        if (fim <= inicio)
            prazo = prazo.AddDays(1);

        return prazo;
    }

    public record DashboardDia(DateTime Data, int Total, int Concluidos, int Progresso);
}
