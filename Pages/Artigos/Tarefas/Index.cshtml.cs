using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Artigos.Tarefas;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<TarefaArtigo> Tarefas { get; set; } = [];
    public List<Artigo> Artigos { get; set; } = [];
    public string? FiltroStatus { get; set; }
    public int? FiltroArtigo { get; set; }

    public async Task OnGetAsync(string? status, int? artigo)
    {
        FiltroStatus = status;
        FiltroArtigo = artigo;
        Artigos = await _db.Artigos.OrderBy(a => a.Titulo).ToListAsync();

        var query = _db.TarefasArtigo.Include(t => t.Artigo).AsQueryable();

        if (artigo.HasValue)
            query = query.Where(t => t.ArtigoId == artigo.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusTarefa>(status, out var s))
            query = query.Where(t => t.Status == s);

        Tarefas = await query.OrderBy(t => t.DataHoraLimite).ToListAsync();
    }
}
