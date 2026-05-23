using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Pages.Artigos;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Artigo> Artigos { get; set; } = [];

    public async Task OnGetAsync()
    {
        Artigos = await _db.Artigos
            .Include(a => a.Roadmap)
            .Include(a => a.Aulas)
            .OrderByDescending(a => a.DataAtualizacao)
            .ToListAsync();
    }
}
