using CadernoVivo.Models;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Faculdade
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<AulaSemanal> AulasSemanais => Set<AulaSemanal>();
    public DbSet<Material> Materiais => Set<Material>();
    public DbSet<TarefaFaculdade> TarefasFaculdade => Set<TarefaFaculdade>();
    public DbSet<HistoricoAula> HistoricoAulas => Set<HistoricoAula>();
    public DbSet<DuvidaFaculdade> DuvidasFaculdade => Set<DuvidaFaculdade>();
    public DbSet<Pendencia> Pendencias => Set<Pendencia>();

    // Artigos
    public DbSet<Artigo> Artigos => Set<Artigo>();
    public DbSet<AulaArtigo> AulasArtigo => Set<AulaArtigo>();
    public DbSet<RoadmapItem> RoadmapItems => Set<RoadmapItem>();
    public DbSet<TarefaArtigo> TarefasArtigo => Set<TarefaArtigo>();
    public DbSet<Insight> Insights => Set<Insight>();
    public DbSet<DuvidaArtigo> DuvidasArtigo => Set<DuvidaArtigo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pendencia>()
            .HasOne(p => p.TarefaFaculdade)
            .WithMany()
            .HasForeignKey(p => p.TarefaFaculdadeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Pendencia>()
            .HasOne(p => p.TarefaArtigo)
            .WithMany()
            .HasForeignKey(p => p.TarefaArtigoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
