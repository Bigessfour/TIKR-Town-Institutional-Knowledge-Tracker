using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Identity;
using TIKR.Shared.Entities;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

static string Req(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Missing env {name}");

var sqlitePath = Req("SQLITE_PATH");
var pgCs = Req("POSTGRES_CS");
var batchSize = int.TryParse(Environment.GetEnvironmentVariable("BATCH_SIZE"), out var b) ? b : 200;

if (!File.Exists(sqlitePath))
{
    throw new FileNotFoundException(sqlitePath);
}

Console.WriteLine($"SQLite: {sqlitePath}");
Console.WriteLine($"BatchSize: {batchSize}");

await using var pg = new PostgresTikrDbContext(pgCs);
var created = await pg.Database.EnsureCreatedAsync();
Console.WriteLine($"EnsureCreated={created}");

await pg.Database.ExecuteSqlRawAsync("""
    CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
        "MigrationId" character varying(150) NOT NULL,
        "ProductVersion" character varying(32) NOT NULL,
        CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
    );
    """);

var sqliteOptions = new DbContextOptionsBuilder<TikrDbContext>()
    .UseSqlite($"Data Source={sqlitePath}")
    .Options;
await using var sqlite = new TikrDbContext(sqliteOptions);

var migrationIds = new List<string>();
await sqlite.Database.OpenConnectionAsync();
await using (var cmd = sqlite.Database.GetDbConnection().CreateCommand())
{
    cmd.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY 1";
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        migrationIds.Add(reader.GetString(0));
    }
}

foreach (var id in migrationIds)
{
    await pg.Database.ExecuteSqlRawAsync(
        """INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
        id,
        "10.0.14");
}
Console.WriteLine($"Stamped {migrationIds.Count} migration id(s).");

static async Task<int> CopyAsync<T>(TikrDbContext from, TikrDbContext to, string label, int batchSize)
    where T : class
{
    var existing = await to.Set<T>().IgnoreQueryFilters().CountAsync();
    var total = await from.Set<T>().IgnoreQueryFilters().CountAsync();
    if (total == 0)
    {
        Console.WriteLine($"{label}: 0");
        return 0;
    }

    if (existing >= total)
    {
        Console.WriteLine($"{label}: skip (already {existing}/{total})");
        return existing;
    }

    var copied = existing;
    if (existing > 0)
    {
        Console.WriteLine($"{label}: resume from {existing}/{total}");
    }

    while (copied < total)
    {
        var rows = await from.Set<T>().IgnoreQueryFilters().AsNoTracking()
            .Skip(copied).Take(batchSize).ToListAsync();
        if (rows.Count == 0)
        {
            break;
        }

        foreach (var row in rows)
        {
            SanitizeNullChars(row);
        }

        to.ChangeTracker.Clear();
        await to.Set<T>().AddRangeAsync(rows);
        await to.SaveChangesAsync();
        copied += rows.Count;
        Console.WriteLine($"{label}: {copied}/{total}");
    }

    return copied;
}

static void SanitizeNullChars(object entity)
{
    foreach (var prop in entity.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string)))
    {
        if (prop.GetValue(entity) is string s && s.Contains('\0'))
        {
            prop.SetValue(entity, s.Replace("\0", string.Empty));
        }
    }
}

await CopyAsync<IdentityRole>(sqlite, pg, "AspNetRoles", batchSize);
await CopyAsync<ApplicationUser>(sqlite, pg, "AspNetUsers", batchSize);
await CopyAsync<IdentityUserRole<string>>(sqlite, pg, "AspNetUserRoles", batchSize);
await CopyAsync<IdentityUserClaim<string>>(sqlite, pg, "AspNetUserClaims", batchSize);
await CopyAsync<IdentityRoleClaim<string>>(sqlite, pg, "AspNetRoleClaims", batchSize);
await CopyAsync<IdentityUserLogin<string>>(sqlite, pg, "AspNetUserLogins", batchSize);
await CopyAsync<IdentityUserToken<string>>(sqlite, pg, "AspNetUserTokens", batchSize);

await CopyAsync<AppSetting>(sqlite, pg, "AppSettings", batchSize);
await CopyAsync<Contact>(sqlite, pg, "Contacts", batchSize);
await CopyAsync<Requirement>(sqlite, pg, "Requirements", batchSize);
await CopyAsync<Document>(sqlite, pg, "Documents", batchSize);
await CopyAsync<DocumentVersion>(sqlite, pg, "DocumentVersions", batchSize);
await CopyAsync<KnowledgeEntry>(sqlite, pg, "KnowledgeEntries", batchSize);
await CopyAsync<RequirementDocument>(sqlite, pg, "RequirementDocuments", batchSize);
await CopyAsync<RequirementChecklistItem>(sqlite, pg, "RequirementChecklistItems", batchSize);
await CopyAsync<RequirementContact>(sqlite, pg, "RequirementContacts", batchSize);
await CopyAsync<LibraryImportRecord>(sqlite, pg, "LibraryImportRecords", batchSize);
await CopyAsync<ChatConversation>(sqlite, pg, "ChatConversations", batchSize);
await CopyAsync<ChatMessageRecord>(sqlite, pg, "ChatMessages", batchSize);
await CopyAsync<UserMemoryFact>(sqlite, pg, "UserMemoryFacts", batchSize);
await CopyAsync<EmbeddingChunk>(sqlite, pg, "EmbeddingChunks", batchSize);
await CopyAsync<AuditLog>(sqlite, pg, "AuditLogs", batchSize);

var docs = await pg.Documents.IgnoreQueryFilters().CountAsync();
var chunks = await pg.EmbeddingChunks.CountAsync();
var reqs = await pg.Requirements.CountAsync();
Console.WriteLine($"VERIFY documents={docs} embedding_chunks={chunks} requirements={reqs}");
if (docs < 1000 || chunks < 1000 || reqs < 1)
{
    throw new InvalidOperationException("Post-copy counts look too low.");
}

Console.WriteLine("RESULT=PASS");

file sealed class PostgresTikrDbContext : TikrDbContext
{
    public PostgresTikrDbContext(string cs)
        : base(new DbContextOptionsBuilder<TikrDbContext>().UseNpgsql(cs).Options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Replace SQLite filter "IsArchived = 0" with Postgres boolean filter.
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasFilter(""" "IsArchived" = false """)
                .HasDatabaseName("IX_ChatConversations_UserId_Active");
        });
    }
}
