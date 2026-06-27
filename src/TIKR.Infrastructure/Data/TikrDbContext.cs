using Microsoft.EntityFrameworkCore;
using TIKR.Shared.Entities;

namespace TIKR.Infrastructure.Data;

public class TikrDbContext : DbContext
{
    public TikrDbContext(DbContextOptions<TikrDbContext> options) : base(options)
    {
    }

    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<KnowledgeEntry> KnowledgeEntries => Set<KnowledgeEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.DueDate);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<KnowledgeEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
