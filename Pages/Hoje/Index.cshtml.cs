using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Hoje;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<BlocoEstudo> BloquesHoje { get; set; } = [];
    public BlocoEstudo? BlocoAtual { get; set; }
    public BlocoEstudo? ProximoBloco { get; set; }
    public DateTime? ProximaDataComBlocos { get; set; }
    public DateTime? InicioSemanaProximaData { get; set; }
    public int TotalBlocosProximaData { get; set; }
    public List<BlocoEstudo> ProximosBlocos { get; set; } = [];
    public List<BlocoEstudo> FilaBlocos { get; set; } = [];
    public DateTime DataFila { get; set; } = DateTime.Today;
    public bool MostrandoProximaFila { get; set; }
    public int BlocosConcluidosFila { get; set; }

    // Para o formulário de adicionar bloco manual
    [BindProperty] public BlocoEstudo NovoBloco { get; set; } = new();

    public List<Materia> Materias { get; set; } = [];
    public List<Artigo> Artigos { get; set; } = [];
    public List<Projeto> Projetos { get; set; } = [];

    public async Task OnGetAsync()
    {
        await CarregarDados();
    }

    // Finaliza o bloco atual com status + campos de execução
    public async Task<IActionResult> OnPostFinalizarAsync(
        int id, string status, string? ondeParei, string? proximoPasso, string? duvidas)
    {
        var bloco = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bloco == null) return NotFound();

        if (Enum.TryParse<StatusBloco>(status, out var statusEnum))
            bloco.Status = statusEnum;

        bloco.SegundosGastos = DeveRegistrarTempoGasto(bloco.Status)
            ? CalcularSegundosGastos(bloco, DateTime.Now)
            : null;
        bloco.DataConclusao = bloco.Status is StatusBloco.Concluido or StatusBloco.Parcial or StatusBloco.HoraExtra
            ? DateTime.Now
            : null;
        bloco.IniciadoEm = null;
        bloco.PausadoEm = null;
        bloco.SegundosPausados = 0;

        bloco.OndeParei = ondeParei;
        bloco.ProximoPasso = proximoPasso;
        bloco.Duvidas = duvidas;

        // Atualiza UltimoPontoEstudado no Artigo automaticamente
        if (bloco.ArtigoId.HasValue && !string.IsNullOrWhiteSpace(ondeParei))
        {
            var artigo = await _db.Artigos.FindAsync(bloco.ArtigoId.Value);
            if (artigo != null)
            {
                artigo.UltimoPontoEstudado = ondeParei;
                artigo.DataAtualizacao = DateTime.Now;
            }
        }

        // Gera pendência automática
        if (DiasHelper.BlocoGerarPendencia(bloco.Status))
        {
            _db.Pendencias.Add(new Pendencia
            {
                Descricao = $"[{DiasHelper.LabelStatusBloco(bloco.Status)}] {bloco.Titulo}",
                Origem = bloco.Modulo,
                DataCriacao = DateTime.Now,
                DataLimite = DateTime.Today.AddDays(2),
                Observacao = proximoPasso
            });
        }

        // As anotacoes ficam no proprio bloco para aparecerem no dia estudado e no painel.

        // Reagendado: cria bloco idêntico para amanhã
        if (bloco.Status == StatusBloco.Reagendado)
        {
            _db.BloquesEstudo.Add(new BlocoEstudo
            {
                Data = DateTime.Today.AddDays(1),
                HoraInicio = bloco.HoraInicio,
                HoraFim = bloco.HoraFim,
                Titulo = bloco.Titulo,
                Descricao = string.IsNullOrEmpty(proximoPasso) ? bloco.Descricao : proximoPasso,
                Modulo = bloco.Modulo,
                MateriaId = bloco.MateriaId,
                ArtigoId = bloco.ArtigoId,
                Status = StatusBloco.Agendado
            });
        }

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{bloco.Titulo}\" → {DiasHelper.LabelStatusBloco(bloco.Status)}.";
        return RedirectToPage();
    }

    // Inicia manualmente um bloco agendado
    public async Task<IActionResult> OnPostIniciarAsync(int id)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco != null)
        {
            var blocosAtivos = await _db.BloquesEstudo
                .Where(b => (b.Status == StatusBloco.EmAndamento || b.Status == StatusBloco.Pausado) && b.Id != id)
                .ToListAsync();

            foreach (var ativo in blocosAtivos)
            {
                if (ativo.Status == StatusBloco.EmAndamento)
                {
                    ativo.Status = StatusBloco.Pausado;
                    ativo.PausadoEm = DateTime.Now;
                }
                // Blocos já Pausados ficam intocados para poder retomar depois
            }

            var duracao = CalcularDuracao(bloco.HoraInicio, bloco.HoraFim);
            var inicioReal = DateTime.Now;
            var fimReal = inicioReal.Add(duracao);

            bloco.Data = DateTime.Today;
            bloco.HoraInicio = inicioReal.ToString("HH:mm");
            bloco.HoraFim = fimReal.ToString("HH:mm");
            bloco.Status = StatusBloco.EmAndamento;
            bloco.IniciadoEm = inicioReal;
            bloco.SegundosPausados = 0;
            bloco.SegundosGastos = null;
            bloco.MinutosExtras = 0;
            bloco.PausadoEm = null;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPausarAsync(int id)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        if (bloco.Status != StatusBloco.EmAndamento)
        {
            TempData["Erro"] = "So da para pausar um bloco que esteja em andamento.";
            return RedirectToPage();
        }

        bloco.Status = StatusBloco.Pausado;
        bloco.PausadoEm = DateTime.Now;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{bloco.Titulo}\" pausado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRetomarAsync(int id)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        if (bloco.Status != StatusBloco.Pausado || !bloco.PausadoEm.HasValue)
        {
            TempData["Erro"] = "Este bloco nao esta pausado.";
            return RedirectToPage();
        }

        var fimAtual = ResolverFimBloco(bloco);
        if (!fimAtual.HasValue)
        {
            TempData["Erro"] = "Nao consegui retomar porque o horario final do bloco esta invalido.";
            return RedirectToPage();
        }

        var novoFim = fimAtual.Value.Add(DateTime.Now - bloco.PausadoEm.Value);
        bloco.SegundosPausados += Math.Max(0, (int)Math.Round((DateTime.Now - bloco.PausadoEm.Value).TotalSeconds));
        bloco.HoraFim = novoFim.ToString("HH:mm");
        bloco.Status = StatusBloco.EmAndamento;
        bloco.PausadoEm = null;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"\"{bloco.Titulo}\" retomado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAdicionarTempoAsync(int id, int minutos)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        if (minutos <= 0)
        {
            TempData["Erro"] = "Informe uma quantidade valida de minutos extras.";
            return RedirectToPage();
        }

        if (bloco.Status != StatusBloco.EmAndamento && bloco.Status != StatusBloco.Pausado)
        {
            TempData["Erro"] = "So da para adicionar tempo extra em um bloco iniciado.";
            return RedirectToPage();
        }

        var fimAtual = ResolverFimBloco(bloco);
        if (!fimAtual.HasValue)
        {
            TempData["Erro"] = "Nao consegui somar tempo porque o horario final do bloco esta invalido.";
            return RedirectToPage();
        }

        var novoFim = fimAtual.Value.AddMinutes(minutos);
        bloco.HoraFim = novoFim.ToString("HH:mm");
        bloco.MinutosExtras += minutos;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Adicionados {minutos} min de hora extra em \"{bloco.Titulo}\".";
        return RedirectToPage();
    }

    // Adiciona um bloco manual
    public async Task<IActionResult> OnPostAdicionarBlocoAsync()
    {
        if (string.IsNullOrWhiteSpace(NovoBloco.Titulo))
        {
            TempData["Erro"] = "Título é obrigatório.";
            return RedirectToPage();
        }

        NovoBloco.Status = StatusBloco.Agendado;
        _db.BloquesEstudo.Add(NovoBloco);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Bloco \"{NovoBloco.Titulo}\" adicionado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAjustarBlocoAsync(
        int id, DateTime data, string horaInicio, string horaFim, string titulo, string? descricao, string? observacoes)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco == null) return NotFound();

        if (string.IsNullOrWhiteSpace(titulo) ||
            string.IsNullOrWhiteSpace(horaInicio) ||
            string.IsNullOrWhiteSpace(horaFim))
        {
            TempData["Erro"] = "Data, horario e titulo sao obrigatorios para ajustar o bloco.";
            return RedirectToPage();
        }

        bloco.Data = data.Date;
        bloco.HoraInicio = horaInicio;
        bloco.HoraFim = horaFim;
        bloco.Titulo = titulo.Trim();
        bloco.Descricao = descricao?.Trim();
        bloco.Duvidas = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        bloco.IniciadoEm = null;
        bloco.SegundosPausados = 0;
        bloco.SegundosGastos = null;
        bloco.PausadoEm = null;

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Bloco \"{bloco.Titulo}\" ajustado.";
        return RedirectToPage();
    }

    // Exclui um bloco
    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var bloco = await _db.BloquesEstudo.FindAsync(id);
        if (bloco != null)
        {
            _db.BloquesEstudo.Remove(bloco);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    private async Task CarregarDados()
    {
        var hoje = DateTime.Today;
        BloquesHoje = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => b.Data.Date == hoje)
            .OrderBy(b => b.HoraInicio)
            .ToListAsync();

        if (!BloquesHoje.Any())
        {
            ProximosBlocos = await _db.BloquesEstudo
                .Include(b => b.Materia)
                .Include(b => b.Artigo)
                .Include(b => b.Projeto)
                .Where(b => b.Data.Date > hoje)
                .OrderBy(b => b.Data)
                .ThenBy(b => b.HoraInicio)
                .Take(8)
                .ToListAsync();

            var proximaData = ProximosBlocos.FirstOrDefault()?.Data;

            if (proximaData.HasValue)
            {
                ProximaDataComBlocos = proximaData.Value.Date;
                TotalBlocosProximaData = await _db.BloquesEstudo
                    .CountAsync(b => b.Data.Date == ProximaDataComBlocos.Value);

                var deslocamento = ((int)ProximaDataComBlocos.Value.DayOfWeek + 6) % 7;
                InicioSemanaProximaData = ProximaDataComBlocos.Value.AddDays(-deslocamento).Date;
            }
        }

        FilaBlocos = BloquesHoje.Any()
            ? BloquesHoje
            : ProximosBlocos
                .Where(b => ProximaDataComBlocos.HasValue && b.Data.Date == ProximaDataComBlocos.Value)
                .ToList();

        MostrandoProximaFila = !BloquesHoje.Any() && FilaBlocos.Any();
        DataFila = FilaBlocos.FirstOrDefault()?.Data.Date ?? hoje;
        BlocosConcluidosFila = FilaBlocos.Count(b => b.Status == StatusBloco.Concluido);

        Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();
        Artigos = await _db.Artigos.OrderBy(a => a.Titulo).ToListAsync();
        Projetos = await _db.Projetos
            .Where(p => p.Status != StatusProjeto.Concluido)
            .OrderBy(p => p.DataLimite == null)
            .ThenBy(p => p.DataLimite)
            .ToListAsync();

        var agoraStr = DateTime.Now.ToString("HH:mm");

        // Bloco atual: dentro do horário ou explicitamente EmAndamento
        BlocoAtual = BloquesHoje.FirstOrDefault(b =>
            b.Status == StatusBloco.EmAndamento);

        BlocoAtual ??= BloquesHoje.FirstOrDefault(b =>
            b.Status == StatusBloco.Pausado);

        BlocoAtual ??= BloquesHoje.FirstOrDefault(b =>
            string.Compare(b.HoraInicio, agoraStr) <= 0 &&
            string.Compare(b.HoraFim, agoraStr) > 0 &&
            b.Status == StatusBloco.Agendado);

        var idxAtual = BlocoAtual != null ? BloquesHoje.IndexOf(BlocoAtual) : -1;
        ProximoBloco = BloquesHoje
            .Skip(idxAtual + 1)
            .FirstOrDefault(b => b.Status == StatusBloco.Agendado);
    }

    private static TimeSpan CalcularDuracao(string horaInicio, string horaFim)
    {
        if (!TimeSpan.TryParse(horaInicio, out var inicio) ||
            !TimeSpan.TryParse(horaFim, out var fim))
            return TimeSpan.FromMinutes(50);

        if (fim <= inicio)
            fim = fim.Add(TimeSpan.FromDays(1));

        var duracao = fim - inicio;
        return duracao.TotalMinutes > 0
            ? duracao
            : TimeSpan.FromMinutes(50);
    }

    private static DateTime? ResolverFimBloco(BlocoEstudo bloco)
    {
        if (!TimeSpan.TryParse(bloco.HoraInicio, out var inicio) ||
            !TimeSpan.TryParse(bloco.HoraFim, out var fim))
            return null;

        var baseDate = bloco.IniciadoEm?.Date ?? bloco.Data.Date;
        var fimDataHora = baseDate.Add(fim);
        if (fim <= inicio)
            fimDataHora = fimDataHora.AddDays(1);

        return fimDataHora;
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

        return CalcularDuracao(bloco.HoraInicio, bloco.HoraFim) is var duracao
            ? Math.Max(0, (int)Math.Round(duracao.TotalSeconds))
            : 0;
    }
}
