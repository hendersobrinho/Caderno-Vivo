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

    // Para o formulário de adicionar bloco manual
    [BindProperty] public BlocoEstudo NovoBloco { get; set; } = new();

    public List<Materia> Materias { get; set; } = [];
    public List<Artigo> Artigos { get; set; } = [];

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
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bloco == null) return NotFound();

        if (Enum.TryParse<StatusBloco>(status, out var statusEnum))
            bloco.Status = statusEnum;

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

        // Salva dúvidas nas tabelas corretas
        if (!string.IsNullOrWhiteSpace(duvidas))
        {
            foreach (var linha in duvidas.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var desc = linha.Trim().TrimStart('-', '*', '•').Trim();
                if (string.IsNullOrEmpty(desc)) continue;

                if (bloco.MateriaId.HasValue)
                    _db.DuvidasFaculdade.Add(new DuvidaFaculdade
                        { MateriaId = bloco.MateriaId.Value, Descricao = desc, DataRegistro = DateTime.Now });
                else if (bloco.ArtigoId.HasValue)
                    _db.DuvidasArtigo.Add(new DuvidaArtigo
                        { ArtigoId = bloco.ArtigoId.Value, Descricao = desc, DataRegistro = DateTime.Now });
            }
        }

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
            bloco.Status = StatusBloco.EmAndamento;
            await _db.SaveChangesAsync();
        }
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
            .Where(b => b.Data.Date == hoje)
            .OrderBy(b => b.HoraInicio)
            .ToListAsync();

        Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();
        Artigos = await _db.Artigos.OrderBy(a => a.Titulo).ToListAsync();

        var agoraStr = DateTime.Now.ToString("HH:mm");

        // Bloco atual: dentro do horário ou explicitamente EmAndamento
        BlocoAtual = BloquesHoje.FirstOrDefault(b =>
            b.Status == StatusBloco.EmAndamento);

        BlocoAtual ??= BloquesHoje.FirstOrDefault(b =>
            string.Compare(b.HoraInicio, agoraStr) <= 0 &&
            string.Compare(b.HoraFim, agoraStr) > 0 &&
            b.Status == StatusBloco.Agendado);

        var idxAtual = BlocoAtual != null ? BloquesHoje.IndexOf(BlocoAtual) : -1;
        ProximoBloco = BloquesHoje
            .Skip(idxAtual + 1)
            .FirstOrDefault(b => b.Status == StatusBloco.Agendado);
    }
}
