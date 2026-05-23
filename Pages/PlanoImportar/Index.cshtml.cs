using System.Text.Json;
using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.PlanoImportar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty]
    public string JsonTexto { get; set; } = "";

    [BindProperty]
    public IFormFile? ArquivoJson { get; set; }

    [BindProperty]
    public BlocoEstudo BlocoManual { get; set; } = new()
    {
        Data = DateTime.Today,
        HoraInicio = "19:30",
        HoraFim = "21:00",
        Modulo = "Faculdade"
    };

    [BindProperty]
    public DateTime? LimparInicio { get; set; }

    [BindProperty]
    public DateTime? LimparFim { get; set; }

    public List<Materia> Materias { get; set; } = [];
    public List<Projeto> Projetos { get; set; } = [];

    public string? Resultado { get; set; }
    public bool Sucesso { get; set; }

    public async Task OnGetAsync()
    {
        await CarregarApoioAsync();
    }

    public async Task<IActionResult> OnPostAdicionarBlocoManualAsync()
    {
        if (string.IsNullOrWhiteSpace(BlocoManual.Titulo))
        {
            TempData["Erro"] = "Escreva um titulo para o bloco manual.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(BlocoManual.HoraInicio) ||
            string.IsNullOrWhiteSpace(BlocoManual.HoraFim))
        {
            TempData["Erro"] = "Informe horario de inicio e fim.";
            return RedirectToPage();
        }

        BlocoManual.Data = BlocoManual.Data.Date;
        BlocoManual.Titulo = BlocoManual.Titulo.Trim();
        BlocoManual.Descricao = BlocoManual.Descricao?.Trim();
        BlocoManual.Modulo = string.IsNullOrWhiteSpace(BlocoManual.Modulo)
            ? "Geral"
            : BlocoManual.Modulo.Trim();
        BlocoManual.Status = StatusBloco.Agendado;
        BlocoManual.ArtigoId = null;

        _db.BloquesEstudo.Add(BlocoManual);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Bloco \"{BlocoManual.Titulo}\" adicionado em {BlocoManual.Data:dd/MM}.";
        return RedirectToPage("/Faculdade/PlanoDaSemana/Index", new
        {
            inicio = InicioDaSemana(BlocoManual.Data).ToString("yyyy-MM-dd")
        });
    }

    public async Task<IActionResult> OnPostLimparBlocosFuturosAsync()
    {
        var hoje = DateTime.Today;
        var blocos = await _db.BloquesEstudo
            .Where(b => b.Data >= hoje)
            .ToListAsync();

        _db.BloquesEstudo.RemoveRange(blocos);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"{blocos.Count} bloco(s) de hoje em diante removido(s).";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLimparTodosBlocosAsync()
    {
        var blocos = await _db.BloquesEstudo.ToListAsync();

        _db.BloquesEstudo.RemoveRange(blocos);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"{blocos.Count} bloco(s) removido(s).";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLimparPeriodoAsync()
    {
        if (!LimparInicio.HasValue || !LimparFim.HasValue)
        {
            TempData["Erro"] = "Escolha a data inicial e final para limpar o periodo.";
            return RedirectToPage();
        }

        var inicio = LimparInicio.Value.Date;
        var fim = LimparFim.Value.Date;
        if (fim < inicio)
        {
            TempData["Erro"] = "A data final precisa ser maior ou igual a data inicial.";
            return RedirectToPage();
        }

        var blocos = await _db.BloquesEstudo
            .Where(b => b.Data.Date >= inicio && b.Data.Date <= fim)
            .ToListAsync();

        _db.BloquesEstudo.RemoveRange(blocos);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"{blocos.Count} bloco(s) removido(s) de {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ArquivoJson is { Length: > 0 })
        {
            using var reader = new StreamReader(ArquivoJson.OpenReadStream());
            JsonTexto = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(JsonTexto))
        {
            TempData["Erro"] = "Selecione um arquivo JSON ou cole o conteúdo do plano.";
            return RedirectToPage();
        }

        try
        {
            using var doc = JsonDocument.Parse(JsonTexto);
            var root = doc.RootElement;

            var tipo = root.TryGetProperty("tipo", out var t) ? t.GetString() : null;
            if (tipo != "plano")
            {
                TempData["Erro"] = "JSON inválido. O campo \"tipo\" deve ser \"plano\".";
                return RedirectToPage();
            }

            var materias = await _db.Materias.ToListAsync();
            var artigos = await _db.Artigos.ToListAsync();
            var materiasPorNome = new Dictionary<string, Materia>(StringComparer.OrdinalIgnoreCase);
            var artigosPorTitulo = new Dictionary<string, Artigo>(StringComparer.OrdinalIgnoreCase);

            foreach (var materia in materias)
            {
                var chave = materia.Nome.Trim();
                if (!string.IsNullOrWhiteSpace(chave) && !materiasPorNome.ContainsKey(chave))
                    materiasPorNome[chave] = materia;
            }

            foreach (var artigo in artigos)
            {
                var chave = artigo.Titulo.Trim();
                if (!string.IsNullOrWhiteSpace(chave) && !artigosPorTitulo.ContainsKey(chave))
                    artigosPorTitulo[chave] = artigo;
            }

            int criados = 0;
            int duplicados = 0;
            DateTime? primeiroDiaImportado = null;
            DateTime? ultimoDiaImportado = null;

            var inicioSemana = root.TryGetProperty("semana_inicio", out var semanaInicioEl)
                && DateTime.TryParse(semanaInicioEl.GetString(), out var semanaInicioParseado)
                ? semanaInicioParseado.Date
                : (DateTime?)null;

            if (root.TryGetProperty("dias", out var dias))
            {
                foreach (var dia in dias.EnumerateArray())
                {
                    if (!dia.TryGetProperty("data", out var dataEl)) continue;
                    if (!DateTime.TryParse(dataEl.GetString(), out var data)) continue;

                    var dataDia = data.Date;
                    primeiroDiaImportado = !primeiroDiaImportado.HasValue || dataDia < primeiroDiaImportado
                        ? dataDia
                        : primeiroDiaImportado;
                    ultimoDiaImportado = !ultimoDiaImportado.HasValue || dataDia > ultimoDiaImportado
                        ? dataDia
                        : ultimoDiaImportado;

                    if (!dia.TryGetProperty("blocos", out var blocos)) continue;

                    foreach (var bloco in blocos.EnumerateArray())
                    {
                        var inicio = bloco.TryGetProperty("inicio", out var i) ? i.GetString() ?? "08:00" : "08:00";
                        var fim = bloco.TryGetProperty("fim", out var f) ? f.GetString() ?? "10:00" : "10:00";
                        var titulo = bloco.TryGetProperty("titulo", out var ti) ? ti.GetString() ?? "" : "";
                        var descricao = bloco.TryGetProperty("descricao", out var d) ? d.GetString() : null;
                        var modulo = bloco.TryGetProperty("modulo", out var mo) ? mo.GetString() ?? "Geral" : "Geral";
                        var nomMateria = bloco.TryGetProperty("materia", out var ma) ? ma.GetString() : null;
                        var nomArtigo = bloco.TryGetProperty("artigo", out var ar) ? ar.GetString() : null;

                        if (string.IsNullOrWhiteSpace(titulo)) continue;

                        // Verifica duplicata (mesma data, inicio e titulo)
                        var jaExiste = await _db.BloquesEstudo.AnyAsync(b =>
                            b.Data.Date == dataDia &&
                            b.HoraInicio == inicio &&
                            b.Titulo == titulo);

                        if (jaExiste) { duplicados++; continue; }

                        // Mantem a entidade rastreada para evitar MateriaId = 0 em materias criadas no mesmo lote.
                        Materia? materiaDoBloco = null;
                        if (!string.IsNullOrWhiteSpace(nomMateria))
                        {
                            var nomeMateria = nomMateria.Trim();
                            if (!materiasPorNome.TryGetValue(nomeMateria, out materiaDoBloco))
                            {
                                materiaDoBloco = new Materia
                                {
                                    Nome = nomeMateria,
                                    Descricao = "Criada automaticamente pela importação do plano."
                                };
                                _db.Materias.Add(materiaDoBloco);
                                materiasPorNome[nomeMateria] = materiaDoBloco;
                            }
                        }

                        Artigo? artigoDoBloco = null;
                        if (!string.IsNullOrWhiteSpace(nomArtigo))
                        {
                            artigosPorTitulo.TryGetValue(nomArtigo.Trim(), out artigoDoBloco);
                        }

                        _db.BloquesEstudo.Add(new BlocoEstudo
                        {
                            Data = dataDia,
                            HoraInicio = inicio,
                            HoraFim = fim,
                            Titulo = titulo,
                            Descricao = descricao,
                            Modulo = modulo,
                            Materia = materiaDoBloco,
                            Artigo = artigoDoBloco,
                            Status = StatusBloco.Agendado
                        });
                        criados++;
                    }
                }
            }

            await _db.SaveChangesAsync();

            var resumo = criados == 0 && duplicados > 0
                ? $"Plano importado: nenhum bloco novo criado; {duplicados} já existia(m) nessa faixa."
                : $"Plano importado: {criados} bloco(s) criado(s)" +
                  (duplicados > 0 ? $", {duplicados} ignorado(s) (duplicata)." : ".");

            if (primeiroDiaImportado.HasValue && ultimoDiaImportado.HasValue)
                resumo += $" Período: {primeiroDiaImportado:dd/MM} a {ultimoDiaImportado:dd/MM}.";

            TempData["Sucesso"] = resumo;

            var semanaDestino = inicioSemana
                ?? primeiroDiaImportado
                ?? DateTime.Today;

            return RedirectToPage("/Faculdade/PlanoDaSemana/Index", new
            {
                inicio = semanaDestino.ToString("yyyy-MM-dd")
            });
        }
        catch (JsonException ex)
        {
            TempData["Erro"] = $"Erro ao ler o JSON: {ex.Message}";
            return RedirectToPage();
        }
    }

    private async Task CarregarApoioAsync()
    {
        Materias = await _db.Materias.OrderBy(m => m.Nome).ToListAsync();
        Projetos = await _db.Projetos
            .Where(p => p.Status != StatusProjeto.Concluido)
            .OrderBy(p => p.DataLimite == null)
            .ThenBy(p => p.DataLimite)
            .ToListAsync();
    }

    private static DateTime InicioDaSemana(DateTime data)
    {
        var deslocamento = ((int)data.DayOfWeek + 6) % 7;
        return data.AddDays(-deslocamento).Date;
    }
}
