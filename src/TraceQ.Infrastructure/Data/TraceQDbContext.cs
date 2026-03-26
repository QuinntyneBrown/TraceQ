using Microsoft.EntityFrameworkCore;
using TraceQ.Core.Models;

namespace TraceQ.Infrastructure.Data;

public class TraceQDbContext : DbContext
{
    public TraceQDbContext(DbContextOptions<TraceQDbContext> options) : base(options)
    {
    }

    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportRecord> ImportRecords => Set<ImportRecord>();
    public DbSet<DashboardLayout> DashboardLayouts => Set<DashboardLayout>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Requirement configuration
        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RequirementNumber).IsUnique();
            entity.Property(e => e.RequirementNumber).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(10000);
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Priority).HasMaxLength(100);
            entity.Property(e => e.Owner).HasMaxLength(200);
            entity.Property(e => e.Module).HasMaxLength(200);
            entity.Property(e => e.ParentNumber).HasMaxLength(200);
            entity.Property(e => e.TracedTo).HasMaxLength(2000);

            entity.HasOne(e => e.ImportBatch)
                .WithMany()
                .HasForeignKey(e => e.ImportBatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ImportBatch configuration
        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
        });

        // ImportRecord configuration
        modelBuilder.Entity<ImportRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequirementNumber).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasOne(e => e.ImportBatch)
                .WithMany(b => b.Records)
                .HasForeignKey(e => e.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DashboardLayout configuration
        modelBuilder.Entity<DashboardLayout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // AuditLogEntry configuration
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
