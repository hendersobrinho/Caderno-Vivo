using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.LinhaDoTempo;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public string? Data { get; set; }

    public DateTime DiaSelecionado { get; set; }
    public List<DiaResumo> Dias { get; set; } = [];
    public List<BlocoEstudo> BlocosDia { get; set; } = [];
    public int TotalDia { get; set; }
    public int ConcluidosDia { get; set; }
    public int PendentesDia { get; set; }
    public int AtrasadosDia { get; set; }
    public int ProgressoDia { get; set; }
    public int MinutosDia { get; set; }
    public int MinutosRegistradosDia { get; set; }

    public async Task OnGetAsync()
    {
        DiaSelecionado = DateTime.TryParse(Data, out var data)
            ? data.Date
            : DateTime.Today;

        var inicio = DiaSelecionado.AddDays(-10);
        var fim = DiaSelecionado.AddDays(14);

        var blocosPeriodo = await _db.BloquesEstudo
            .Where(b => b.Data.Date >= inicio && b.Data.Date <= fim)
            .OrderBy(b => b.Data)
            .ThenBy(b => b.HoraInicio)
            .ToListAsync();

        Dias = Enumerable.Range(0, (fim - inicio).Days + 1)
            .Select(offset =>
            {
                var dia = inicio.AddDays(offset).Date;
                var blocos = blocosPeriodo.Where(b => b.Data.Date == dia).ToList();
                return new DiaResumo(
                    dia,
                    blocos.Count,
                    blocos.Count(b => b.Status == StatusBloco.Concluido),
                    blocos.Count(b => b.Status != StatusBloco.Concluido && b.Status != StatusBloco.NaoFeito),
                    dia < DateTime.Today && blocos.Any(b => b.Status != StatusBloco.Concluido && b.Status != StatusBloco.NaoFeito));
            })
            .ToList();

        BlocosDia = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => b.Data.Date == DiaSelecionado)
            .OrderBy(b => b.HoraInicio)
            .ToListAsync();

        TotalDia = BlocosDia.Count;
        ConcluidosDia = BlocosDia.Count(b => b.Status == StatusBloco.Concluido);
        PendentesDia = BlocosDia.Count(b => b.Status != StatusBloco.Concluido && b.Status != StatusBloco.NaoFeito);
        AtrasadosDia = DiaSelecionado < DateTime.Today ? PendentesDia : 0;
        ProgressoDia = TotalDia == 0
            ? 0
            : (int)Math.Round((double)ConcluidosDia / TotalDia * 100);
        MinutosDia = BlocosDia.Sum(CalcularMinutos);
        MinutosRegistradosDia = BlocosDia
            .Where(b => b.SegundosGastos.HasValue)
            .Sum(CalcularMinutosGastos);
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
        return RedirectToPage(new { data = bloco.Data.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostIniciarAsync(int id)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null)
            return ResponderNaoEncontrado();

        var blocosAtivos = await _db.BloquesEstudo
            .Where(b => (b.Status == StatusBloco.EmAndamento || b.Status == StatusBloco.Pausado) && b.Id != id)
            .ToListAsync();

        foreach (var ativo in blocosAtivos)
        {
            ativo.Status = StatusBloco.Agendado;
            ativo.IniciadoEm = null;
            ativo.SegundosPausados = 0;
            ativo.SegundosGastos = null;
            ativo.PausadoEm = null;
        }

        bloco.Status = StatusBloco.EmAndamento;
        bloco.IniciadoEm = DateTime.Now;
        bloco.SegundosPausados = 0;
        bloco.SegundosGastos = null;
        bloco.PausadoEm = null;

        await _db.SaveChangesAsync();

        return ResponderControle(bloco, $"\"{bloco.Titulo}\" iniciado daqui mesmo.");
    }

    public async Task<IActionResult> OnPostPausarAsync(int id)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null)
            return ResponderNaoEncontrado();

        if (bloco.Status != StatusBloco.EmAndamento)
        {
            return ResponderErro("So da para pausar um bloco que esteja em andamento.", bloco.Data);
        }

        bloco.Status = StatusBloco.Pausado;
        bloco.PausadoEm = DateTime.Now;
        await _db.SaveChangesAsync();

        return ResponderControle(bloco, $"\"{bloco.Titulo}\" pausado.");
    }

    public async Task<IActionResult> OnPostRetomarAsync(int id)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null)
            return ResponderNaoEncontrado();

        if (bloco.Status != StatusBloco.Pausado || !bloco.PausadoEm.HasValue)
        {
            return ResponderErro("Este bloco nao esta pausado.", bloco.Data);
        }

        bloco.SegundosPausados += Math.Max(0, (int)Math.Round((DateTime.Now - bloco.PausadoEm.Value).TotalSeconds));
        bloco.Status = StatusBloco.EmAndamento;
        bloco.PausadoEm = null;
        await _db.SaveChangesAsync();

        return ResponderControle(bloco, $"\"{bloco.Titulo}\" retomado.");
    }

    public async Task<IActionResult> OnPostAdicionarTempoAsync(int id, int minutos)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null)
            return ResponderNaoEncontrado();

        if (minutos <= 0)
        {
            return ResponderErro("Informe uma quantidade valida de minutos extras.", bloco.Data);
        }

        if (bloco.Status != StatusBloco.EmAndamento && bloco.Status != StatusBloco.Pausado)
        {
            return ResponderErro("So da para adicionar tempo extra em um bloco iniciado.", bloco.Data);
        }

        bloco.MinutosExtras += minutos;
        await _db.SaveChangesAsync();

        return ResponderControle(bloco, $"Adicionados {minutos} min de hora extra em \"{bloco.Titulo}\".");
    }

    public async Task<IActionResult> OnPostRemarcarBlocoAsync(
        int id, DateTime data, string horaInicio, string horaFim, string? observacao)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        if (data == default ||
            string.IsNullOrWhiteSpace(horaInicio) ||
            string.IsNullOrWhiteSpace(horaFim))
        {
            TempData["Erro"] = "Escolha a nova data, inicio e fim para remarcar o bloco.";
            return RedirectToPage(new { data = bloco.Data.ToString("yyyy-MM-dd") });
        }

        var dataOriginal = bloco.Data.Date;
        bloco.Data = data.Date;
        bloco.HoraInicio = horaInicio.Trim();
        bloco.HoraFim = horaFim.Trim();
        bloco.Status = StatusBloco.Agendado;
        bloco.IniciadoEm = null;
        bloco.SegundosPausados = 0;
        bloco.SegundosGastos = null;
        bloco.PausadoEm = null;

        if (!string.IsNullOrWhiteSpace(observacao))
            bloco.Observacao = observacao.Trim();
        else if (dataOriginal < DateTime.Today)
            bloco.Observacao = $"Remarcado em {DateTime.Now:dd/MM/yyyy HH:mm}; originalmente em {dataOriginal:dd/MM/yyyy}.";

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{bloco.Titulo}\" remarcado para {bloco.Data:dd/MM} das {bloco.HoraInicio} as {bloco.HoraFim}.";
        return RedirectToPage(new { data = bloco.Data.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostAtualizarStatusAsync(int id, string status, string? observacao)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        if (!Enum.TryParse<StatusBloco>(status, out var novoStatus))
        {
            TempData["Erro"] = "Status invalido para este bloco.";
            return RedirectToPage(new { data = bloco.Data.ToString("yyyy-MM-dd") });
        }

        var eraAtrasado = bloco.Data.Date < DateTime.Today &&
                          bloco.Status != StatusBloco.Concluido &&
                          bloco.Status != StatusBloco.NaoFeito;

        bloco.Status = novoStatus;
        bloco.SegundosGastos = DeveRegistrarTempoGasto(novoStatus)
            ? CalcularSegundosGastos(bloco, DateTime.Now)
            : null;
        bloco.DataConclusao = novoStatus is StatusBloco.Concluido or StatusBloco.Parcial or StatusBloco.HoraExtra
            ? DateTime.Now
            : null;
        bloco.IniciadoEm = null;
        bloco.SegundosPausados = 0;
        bloco.PausadoEm = null;

        if (!string.IsNullOrWhiteSpace(observacao))
            bloco.Observacao = observacao.Trim();
        else if (eraAtrasado && novoStatus == StatusBloco.Concluido)
            bloco.Observacao = $"Concluido com atraso em {DateTime.Now:dd/MM/yyyy HH:mm}.";

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{bloco.Titulo}\" atualizado para {DiasHelper.LabelStatusBloco(bloco.Status)}.";
        return RedirectToPage(new { data = bloco.Data.ToString("yyyy-MM-dd") });
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
        return RedirectToPage(new { data = bloco.Data.ToString("yyyy-MM-dd") });
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

    private static int CalcularSegundosPlanejados(BlocoEstudo bloco)
    {
        return Math.Max(60, CalcularMinutos(bloco) * 60) + (bloco.MinutosExtras * 60);
    }

    private static int CalcularSegundosRestantes(BlocoEstudo bloco, DateTime referencia)
    {
        var total = CalcularSegundosPlanejados(bloco);
        if (!bloco.IniciadoEm.HasValue)
            return total;

        var fimMedicao = bloco.Status == StatusBloco.Pausado && bloco.PausadoEm.HasValue
            ? bloco.PausadoEm.Value
            : referencia;

        var gastos = fimMedicao - bloco.IniciadoEm.Value - TimeSpan.FromSeconds(bloco.SegundosPausados);
        var restantes = total - (int)Math.Round(gastos.TotalSeconds);
        return Math.Max(0, restantes);
    }

    private IActionResult ResponderControle(BlocoEstudo bloco, string mensagem)
    {
        if (EhAjax())
        {
            var total = CalcularSegundosPlanejados(bloco);
            var restantes = CalcularSegundosRestantes(bloco, DateTime.Now);
            return new JsonResult(new
            {
                ok = true,
                message = mensagem,
                block = new
                {
                    id = bloco.Id,
                    status = bloco.Status.ToString(),
                    statusLabel = DiasHelper.LabelStatusBloco(bloco.Status),
                    statusClass = DiasHelper.ClasseStatusBloco(bloco.Status),
                    minutesExtra = bloco.MinutosExtras,
                    totalSeconds = total,
                    remainingSeconds = restantes,
                    elapsedSeconds = Math.Max(0, total - restantes),
                    totalMinutes = (int)Math.Ceiling(total / 60d),
                    pausedSeconds = bloco.SegundosPausados,
                    startedAt = bloco.IniciadoEm?.ToString("o"),
                    pausedAt = bloco.PausadoEm?.ToString("o"),
                    isRunning = bloco.Status == StatusBloco.EmAndamento,
                    isPaused = bloco.Status == StatusBloco.Pausado,
                    isStarted = bloco.Status == StatusBloco.EmAndamento || bloco.Status == StatusBloco.Pausado,
                    hasExtra = bloco.MinutosExtras > 0
                }
            });
        }

        TempData["Sucesso"] = mensagem;
        return RedirectToPage(new { data = bloco.Data.ToString("yyyy-MM-dd") });
    }

    private IActionResult ResponderErro(string mensagem, DateTime data)
    {
        if (EhAjax())
            return new BadRequestObjectResult(new { ok = false, message = mensagem });

        TempData["Erro"] = mensagem;
        return RedirectToPage(new { data = data.ToString("yyyy-MM-dd") });
    }

    private IActionResult ResponderNaoEncontrado()
    {
        if (EhAjax())
            return NotFound(new { ok = false, message = "Bloco nao encontrado." });

        return NotFound();
    }

    private bool EhAjax()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DeveRegistrarTempoGasto(StatusBloco status)
    {
        return status != StatusBloco.Agendado;
    }

    private static int CalcularSegundosGastos(BlocoEstudo bloco, DateTime referencia)
    {
        if (bloco.IniciadoEm.HasValue)
        {
            var fimMedicao = bloco.Status == StatusBloco.Pausado && bloco.PausadoEm.HasValue
                ? bloco.PausadoEm.Value
                : referencia;

            var total = fimMedicao - bloco.IniciadoEm.Value - TimeSpan.FromSeconds(bloco.SegundosPausados);
            return Math.Max(0, (int)Math.Round(total.TotalSeconds));
        }

        return Math.Max(0, CalcularMinutos(bloco) * 60);
    }

    public record DiaResumo(DateTime Data, int Total, int Concluidos, int Pendentes, bool Atrasado);
}
