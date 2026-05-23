using System.Text;
using System.Text.Json;
using CadernoVivo.Data;
using CadernoVivo.Helpers;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Exportar;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<BlocoEstudo> BloquesHoje { get; set; } = [];
    public List<Pendencia> Pendencias { get; set; } = [];
    public List<Materia> Materias { get; set; } = [];
    public List<Artigo> Artigos { get; set; } = [];

    public async Task OnGetAsync()
    {
        await CarregarResumo();
    }

    public async Task<IActionResult> OnGetMarkdownAsync()
    {
        await CarregarResumo();
        var md = GerarMarkdown();
        var bytes = Encoding.UTF8.GetBytes(md);
        var nome = $"caderno-vivo-{DateTime.Today:yyyy-MM-dd}.md";
        return File(bytes, "text/markdown; charset=utf-8", nome);
    }

    public async Task<IActionResult> OnGetJsonAsync()
    {
        await CarregarResumo();
        var obj = GerarObjetoJson();
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        var nome = $"caderno-vivo-{DateTime.Today:yyyy-MM-dd}.json";
        return File(bytes, "application/json; charset=utf-8", nome);
    }

    public async Task<IActionResult> OnGetCalendarioAsync()
    {
        var inicio = DateTime.Today.AddDays(-7);
        var fim = DateTime.Today.AddDays(90);
        var blocos = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Include(b => b.Projeto)
            .Where(b => b.Data.Date >= inicio && b.Data.Date <= fim)
            .OrderBy(b => b.Data)
            .ThenBy(b => b.HoraInicio)
            .ToListAsync();

        var ics = GerarCalendarioIcs(blocos);
        var bytes = Encoding.UTF8.GetBytes(ics);
        var nome = $"caderno-vivo-calendario-{DateTime.Today:yyyy-MM-dd}.ics";
        return File(bytes, "text/calendar; charset=utf-8", nome);
    }

    private async Task CarregarResumo()
    {
        var hoje = DateTime.Today;
        BloquesHoje = await _db.BloquesEstudo
            .Include(b => b.Materia)
            .Include(b => b.Artigo)
            .Where(b => b.Data.Date == hoje)
            .OrderBy(b => b.HoraInicio)
            .ToListAsync();

        Pendencias = await _db.Pendencias
            .Where(p => !p.Resolvida)
            .OrderBy(p => p.DataLimite)
            .ToListAsync();

        Materias = await _db.Materias
            .Include(m => m.Tarefas)
            .Include(m => m.Duvidas)
            .OrderBy(m => m.Nome)
            .ToListAsync();

        Artigos = await _db.Artigos
            .Include(a => a.Tarefas)
            .Include(a => a.Duvidas)
            .Include(a => a.Roadmap)
            .OrderBy(a => a.Titulo)
            .ToListAsync();
    }

    private string GerarMarkdown()
    {
        var sb = new StringBuilder();
        var hoje = DateTime.Today;
        var diaSemana = DiasHelper.NomeDia((int)hoje.DayOfWeek);

        sb.AppendLine($"# Estado atual – Caderno Vivo");
        sb.AppendLine($"**Data:** {diaSemana}, {hoje:dd/MM/yyyy}");
        sb.AppendLine();

        // Blocos de hoje
        sb.AppendLine("## Blocos de Hoje");
        if (BloquesHoje.Any())
        {
            foreach (var b in BloquesHoje)
            {
                var vinculo = b.Materia?.Nome ?? b.Artigo?.Titulo ?? b.Modulo;
                sb.AppendLine($"- **{b.HoraInicio}–{b.HoraFim}** {b.Titulo} `[{DiasHelper.LabelStatusBloco(b.Status)}]` ({vinculo})");
                if (!string.IsNullOrWhiteSpace(b.OndeParei))
                    sb.AppendLine($"  - Onde parei: {b.OndeParei}");
                if (!string.IsNullOrWhiteSpace(b.ProximoPasso))
                    sb.AppendLine($"  - Próximo passo: {b.ProximoPasso}");
                if (!string.IsNullOrWhiteSpace(b.Duvidas))
                    sb.AppendLine($"  - Dúvidas: {b.Duvidas.Replace("\n", "; ")}");
            }
        }
        else
        {
            sb.AppendLine("_Nenhum bloco registrado para hoje._");
        }
        sb.AppendLine();

        // Pendências abertas
        sb.AppendLine("## Pendências Abertas");
        if (Pendencias.Any())
        {
            foreach (var p in Pendencias)
            {
                var limite = p.DataLimite.HasValue ? $" (até {p.DataLimite:dd/MM})" : "";
                sb.AppendLine($"- [{p.Origem}] {p.Descricao}{limite}");
                if (!string.IsNullOrWhiteSpace(p.Observacao))
                    sb.AppendLine($"  - Obs: {p.Observacao}");
            }
        }
        else
        {
            sb.AppendLine("_Nenhuma pendência aberta._");
        }
        sb.AppendLine();

        // Faculdade
        sb.AppendLine("## Faculdade – Matérias");
        foreach (var m in Materias)
        {
            sb.AppendLine($"### {m.Nome}");
            var tarefasAbertas = m.Tarefas?.Where(t => t.Status != StatusTarefa.Concluida).ToList() ?? [];
            if (tarefasAbertas.Any())
            {
                sb.AppendLine("**Tarefas abertas:**");
                foreach (var t in tarefasAbertas)
                    sb.AppendLine($"- [{DiasHelper.LabelStatus(t.Status)}] {t.Titulo} — até {t.DataHoraLimite:dd/MM HH:mm}");
            }
            var duvidas = m.Duvidas?.Where(d => !d.Resolvida).ToList() ?? [];
            if (duvidas.Any())
            {
                sb.AppendLine("**Dúvidas abertas:**");
                foreach (var d in duvidas)
                    sb.AppendLine($"- {d.Descricao}");
            }
            if (!tarefasAbertas.Any() && !duvidas.Any())
                sb.AppendLine("_Em dia._");
            sb.AppendLine();
        }

        // Artigos
        sb.AppendLine("## Artigos em Andamento");
        var artigosAtivos = Artigos.Where(a => a.Status != StatusArtigo.Publicado && a.Status != StatusArtigo.Pausado).ToList();
        foreach (var a in artigosAtivos)
        {
            sb.AppendLine($"### {a.Titulo} `{DiasHelper.LabelStatusArtigo(a.Status)}` — {a.Progresso}%");
            if (!string.IsNullOrWhiteSpace(a.UltimoPontoEstudado))
                sb.AppendLine($"**Último ponto:** {a.UltimoPontoEstudado}");
            var roadmapPendente = a.Roadmap?.Where(r => !r.Concluido).ToList() ?? [];
            if (roadmapPendente.Any())
            {
                sb.AppendLine("**Roadmap pendente:**");
                foreach (var r in roadmapPendente)
                    sb.AppendLine($"- {r.Titulo}");
            }
            var duvidas = a.Duvidas?.Where(d => !d.Resolvida).ToList() ?? [];
            if (duvidas.Any())
            {
                sb.AppendLine("**Dúvidas abertas:**");
                foreach (var d in duvidas)
                    sb.AppendLine($"- {d.Descricao}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private object GerarObjetoJson()
    {
        return new
        {
            tipo = "estado_atual",
            data_exportacao = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            blocos_hoje = BloquesHoje.Select(b => new
            {
                id = b.Id,
                inicio = b.HoraInicio,
                fim = b.HoraFim,
                titulo = b.Titulo,
                descricao = b.Descricao,
                modulo = b.Modulo,
                materia = b.Materia?.Nome,
                artigo = b.Artigo?.Titulo,
                status = DiasHelper.LabelStatusBloco(b.Status),
                onde_parei = b.OndeParei,
                proximo_passo = b.ProximoPasso,
                duvidas = b.Duvidas
            }),
            pendencias = Pendencias.Select(p => new
            {
                descricao = p.Descricao,
                origem = p.Origem,
                data_limite = p.DataLimite?.ToString("yyyy-MM-dd"),
                observacao = p.Observacao
            }),
            materias = Materias.Select(m => new
            {
                nome = m.Nome,
                tarefas_abertas = m.Tarefas?
                    .Where(t => t.Status != StatusTarefa.Concluida)
                    .Select(t => new { titulo = t.Titulo, status = DiasHelper.LabelStatus(t.Status), data_limite = t.DataHoraLimite.ToString("yyyy-MM-dd HH:mm") }),
                duvidas_abertas = m.Duvidas?
                    .Where(d => !d.Resolvida)
                    .Select(d => d.Descricao)
            }),
            artigos = Artigos
                .Where(a => a.Status != StatusArtigo.Publicado && a.Status != StatusArtigo.Pausado)
                .Select(a => new
                {
                    titulo = a.Titulo,
                    status = DiasHelper.LabelStatusArtigo(a.Status),
                    progresso = a.Progresso,
                    ultimo_ponto = a.UltimoPontoEstudado,
                    roadmap_pendente = a.Roadmap?
                        .Where(r => !r.Concluido)
                        .Select(r => r.Titulo),
                    duvidas_abertas = a.Duvidas?
                        .Where(d => !d.Resolvida)
                        .Select(d => d.Descricao)
                })
        };
    }

    private static string GerarCalendarioIcs(List<BlocoEstudo> blocos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Caderno Vivo//Agenda de Estudos//PT-BR");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("X-WR-CALNAME:Caderno Vivo");
        sb.AppendLine("X-WR-TIMEZONE:America/Sao_Paulo");

        foreach (var bloco in blocos)
        {
            var inicio = CombinarDataHora(bloco.Data, bloco.HoraInicio);
            var fim = CombinarDataHora(bloco.Data, bloco.HoraFim);
            if (fim <= inicio)
                fim = inicio.AddMinutes(50);

            var vinculo = bloco.Materia?.Nome ?? bloco.Artigo?.Titulo ?? bloco.Projeto?.Titulo ?? bloco.Modulo;
            var descricao = string.Join("\\n", new[]
            {
                bloco.Descricao,
                $"Modulo: {bloco.Modulo}",
                $"Vinculo: {vinculo}",
                $"Status: {DiasHelper.LabelStatusBloco(bloco.Status)}"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:caderno-vivo-bloco-{bloco.Id}@local");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART:{inicio:yyyyMMddTHHmmss}");
            sb.AppendLine($"DTEND:{fim:yyyyMMddTHHmmss}");
            sb.AppendLine($"SUMMARY:{EscaparIcs(bloco.Titulo)}");
            sb.AppendLine($"DESCRIPTION:{EscaparIcs(descricao)}");
            sb.AppendLine($"CATEGORIES:{EscaparIcs(bloco.Modulo)}");
            sb.AppendLine("BEGIN:VALARM");
            sb.AppendLine("TRIGGER:-PT10M");
            sb.AppendLine("ACTION:DISPLAY");
            sb.AppendLine($"DESCRIPTION:{EscaparIcs($"Daqui 10 minutos: {bloco.Titulo}")}");
            sb.AppendLine("END:VALARM");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private static DateTime CombinarDataHora(DateTime data, string hora)
    {
        return TimeSpan.TryParse(hora, out var time)
            ? data.Date.Add(time)
            : data.Date.AddHours(19).AddMinutes(30);
    }

    private static string EscaparIcs(string? valor)
    {
        return (valor ?? "")
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }
}
