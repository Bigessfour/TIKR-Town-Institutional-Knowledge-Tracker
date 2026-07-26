using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Identity;
using TIKR.Shared.Entities;

namespace TIKR.Infrastructure.Data;

public class TikrDbContext : IdentityDbContext<ApplicationUser>
{
    public TikrDbContext(DbContextOptions<TikrDbContext> options) : base(options)
    {
    }

    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<KnowledgeEntry> KnowledgeEntries => Set<KnowledgeEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RequirementDocument> RequirementDocuments => Set<RequirementDocument>();
    public DbSet<EmbeddingChunk> EmbeddingChunks => Set<EmbeddingChunk>();
    public DbSet<LibraryImportRecord> LibraryImportRecords => Set<LibraryImportRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.Entity<RequirementDocument>(entity =>
        {
            entity.HasKey(e => new { e.RequirementId, e.DocumentId });
            entity.HasOne(e => e.Requirement)
                .WithMany()
                .HasForeignKey(e => e.RequirementId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmbeddingChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ContentHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(500);
            entity.Property(e => e.Facet).HasMaxLength(200);
            entity.HasIndex(e => new { e.SourceType, e.SourceId });
            entity.HasIndex(e => new { e.SourceType, e.Facet });
        });

        modelBuilder.Entity<LibraryImportRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RelativePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ContentFingerprint).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.RelativePath).IsUnique();
            entity.HasIndex(e => e.DocumentId);
        });
    }
}
