using CadernoVivo.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Cria tabelas novas sem quebrar o banco existente
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""BloquesEstudo"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_BloquesEstudo"" PRIMARY KEY AUTOINCREMENT,
            ""Data"" TEXT NOT NULL,
            ""HoraInicio"" TEXT NOT NULL,
            ""HoraFim"" TEXT NOT NULL,
            ""Titulo"" TEXT NOT NULL,
            ""Descricao"" TEXT NULL,
            ""Modulo"" TEXT NOT NULL DEFAULT 'Geral',
            ""MateriaId"" INTEGER NULL,
            ""ArtigoId"" INTEGER NULL,
            ""Status"" INTEGER NOT NULL DEFAULT 0,
            ""OndeParei"" TEXT NULL,
            ""ProximoPasso"" TEXT NULL,
            ""Duvidas"" TEXT NULL,
            ""Observacao"" TEXT NULL,
            CONSTRAINT ""FK_BloquesEstudo_Artigos_ArtigoId""
                FOREIGN KEY (""ArtigoId"") REFERENCES ""Artigos"" (""Id"") ON DELETE SET NULL,
            CONSTRAINT ""FK_BloquesEstudo_Materias_MateriaId""
                FOREIGN KEY (""MateriaId"") REFERENCES ""Materias"" (""Id"") ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS ""IX_BloquesEstudo_Data"" ON ""BloquesEstudo"" (""Data"");
        CREATE INDEX IF NOT EXISTS ""IX_BloquesEstudo_MateriaId"" ON ""BloquesEstudo"" (""MateriaId"");
        CREATE INDEX IF NOT EXISTS ""IX_BloquesEstudo_ArtigoId"" ON ""BloquesEstudo"" (""ArtigoId"");
    ");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
