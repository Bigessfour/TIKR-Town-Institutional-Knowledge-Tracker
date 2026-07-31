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
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<KnowledgeEntry> KnowledgeEntries => Set<KnowledgeEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RequirementDocument> RequirementDocuments => Set<RequirementDocument>();
    public DbSet<EmbeddingChunk> EmbeddingChunks => Set<EmbeddingChunk>();
    public DbSet<LibraryImportRecord> LibraryImportRecords => Set<LibraryImportRecord>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessageRecord> ChatMessages => Set<ChatMessageRecord>();
    public DbSet<UserMemoryFact> UserMemoryFacts => Set<UserMemoryFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.SubmitTo).HasMaxLength(300);
            entity.Property(e => e.ContactName).HasMaxLength(200);
            entity.Property(e => e.ContactEmail).HasMaxLength(200);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.HasIndex(e => e.DueDate);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => e.DeletedAt);
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasIndex(e => new { e.DocumentId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => e.DocumentId);
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

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(4000).IsRequired();
        });

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(4000);
            entity.HasIndex(e => new { e.UserId, e.IsArchived, e.UpdatedAtUtc });
            // At most one active (non-archived) conversation per clerk.
            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasFilter("IsArchived = 0")
                .HasDatabaseName("IX_ChatConversations_UserId_Active");
            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessageRecord>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.ConversationId, e.CreatedAtUtc });
        });

        modelBuilder.Entity<UserMemoryFact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Key }).IsUnique();
        });
    }
}
