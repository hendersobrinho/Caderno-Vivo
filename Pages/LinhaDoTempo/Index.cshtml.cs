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

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{bloco.Titulo}\" reagendado para {bloco.Data:dd/MM}.";
        return RedirectToPage(new { data = bloco.Data.ToString("yyyy-MM-dd") });
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
        bloco.DataConclusao = novoStatus is StatusBloco.Concluido or StatusBloco.Parcial or StatusBloco.HoraExtra
            ? DateTime.Now
            : null;

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

        var duracao = fim - inicio;
        return duracao.TotalMinutes > 0 ? (int)Math.Round(duracao.TotalMinutes) : 0;
    }

    public record DiaResumo(DateTime Data, int Total, int Concluidos, int Pendentes, bool Atrasado);
}
