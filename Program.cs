using CadernoVivo.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var databasePath = ResolveDatabasePath(builder.Environment);
var culturaBrasil = new CultureInfo("pt-BR");

CultureInfo.DefaultThreadCurrentCulture = culturaBrasil;
CultureInfo.DefaultThreadCurrentUICulture = culturaBrasil;

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

var app = builder.Build();

app.Logger.LogInformation("Usando banco de dados em: {DatabasePath}", databasePath);

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
            ""DataConclusao"" TEXT NULL,
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

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Projetos"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Projetos"" PRIMARY KEY AUTOINCREMENT,
            ""Titulo"" TEXT NOT NULL,
            ""Descricao"" TEXT NULL,
            ""DataLimite"" TEXT NULL,
            ""Prioridade"" INTEGER NOT NULL DEFAULT 1,
            ""Status"" INTEGER NOT NULL DEFAULT 0,
            ""Categoria"" TEXT NULL,
            ""MateriaId"" INTEGER NULL,
            ""Observacoes"" TEXT NULL,
            ""ChecklistJson"" TEXT NULL,
            ""DataCriacao"" TEXT NOT NULL DEFAULT (datetime('now')),
            CONSTRAINT ""FK_Projetos_Materias_MateriaId""
                FOREIGN KEY (""MateriaId"") REFERENCES ""Materias"" (""Id"") ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS ""IX_Projetos_MateriaId"" ON ""Projetos"" (""MateriaId"");
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Lembretes"" (
            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Lembretes"" PRIMARY KEY AUTOINCREMENT,
            ""Titulo"" TEXT NOT NULL,
            ""Descricao"" TEXT NULL,
            ""DataLimite"" TEXT NULL,
            ""Prioridade"" INTEGER NOT NULL DEFAULT 1,
            ""Status"" INTEGER NOT NULL DEFAULT 0,
            ""Escopo"" INTEGER NOT NULL DEFAULT 0,
            ""Destaque"" INTEGER NOT NULL DEFAULT 0,
            ""MateriaId"" INTEGER NULL,
            ""ProjetoId"" INTEGER NULL,
            ""ArtigoId"" INTEGER NULL,
            ""DataCriacao"" TEXT NOT NULL DEFAULT (datetime('now')),
            ""DataConclusao"" TEXT NULL,
            CONSTRAINT ""FK_Lembretes_Artigos_ArtigoId""
                FOREIGN KEY (""ArtigoId"") REFERENCES ""Artigos"" (""Id"") ON DELETE SET NULL,
            CONSTRAINT ""FK_Lembretes_Materias_MateriaId""
                FOREIGN KEY (""MateriaId"") REFERENCES ""Materias"" (""Id"") ON DELETE SET NULL,
            CONSTRAINT ""FK_Lembretes_Projetos_ProjetoId""
                FOREIGN KEY (""ProjetoId"") REFERENCES ""Projetos"" (""Id"") ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS ""IX_Lembretes_Status"" ON ""Lembretes"" (""Status"");
        CREATE INDEX IF NOT EXISTS ""IX_Lembretes_DataLimite"" ON ""Lembretes"" (""DataLimite"");
        CREATE INDEX IF NOT EXISTS ""IX_Lembretes_MateriaId"" ON ""Lembretes"" (""MateriaId"");
        CREATE INDEX IF NOT EXISTS ""IX_Lembretes_ProjetoId"" ON ""Lembretes"" (""ProjetoId"");
        CREATE INDEX IF NOT EXISTS ""IX_Lembretes_ArtigoId"" ON ""Lembretes"" (""ArtigoId"");
    ");

    // Adiciona coluna ProjetoId no BloquesEstudo apenas quando necessário.
    if (!ColumnExists(db, "BloquesEstudo", "ProjetoId"))
    {
        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""BloquesEstudo"" ADD COLUMN ""ProjetoId"" INTEGER NULL REFERENCES ""Projetos""(""Id"") ON DELETE SET NULL");
    }

    if (!ColumnExists(db, "BloquesEstudo", "DataConclusao"))
    {
        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""BloquesEstudo"" ADD COLUMN ""DataConclusao"" TEXT NULL");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culturaBrasil),
    SupportedCultures = [culturaBrasil],
    SupportedUICultures = [culturaBrasil]
});
app.UseRouting();
app.MapRazorPages();

app.Run();

static string ResolveDatabasePath(IWebHostEnvironment environment)
{
    var envPath = Environment.GetEnvironmentVariable("CADERNO_VIVO_DB");
    if (!string.IsNullOrWhiteSpace(envPath))
        return Path.GetFullPath(envPath);

    var projectRoot = EncontrarDiretorioProjeto(AppContext.BaseDirectory)
        ?? EncontrarDiretorioProjeto(environment.ContentRootPath);

    if (!string.IsNullOrWhiteSpace(projectRoot))
        return Path.Combine(projectRoot, "caderno.db");

    return Path.Combine(environment.ContentRootPath, "caderno.db");
}

static string? EncontrarDiretorioProjeto(string startPath)
{
    var dir = new DirectoryInfo(startPath);
    if (!dir.Exists)
        return null;

    while (dir != null)
    {
        var csproj = Path.Combine(dir.FullName, "CadernoVivo.csproj");
        if (File.Exists(csproj))
            return dir.FullName;

        dir = dir.Parent;
    }

    return null;
}

static bool ColumnExists(AppDbContext db, string tableName, string columnName)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;

    if (shouldClose)
        connection.Open();

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"PRAGMA table_info(""{tableName}"")";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var currentName = reader["name"]?.ToString();
            if (string.Equals(currentName, columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
    finally
    {
        if (shouldClose)
            connection.Close();
    }
}
