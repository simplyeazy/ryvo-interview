using Microsoft.EntityFrameworkCore;
using ryvo_interview_be.Data.Entities;

namespace ryvo_interview_be.Data;

public sealed class RyvoDbContext(DbContextOptions<RyvoDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<EngagementEntity> Engagements => Set<EngagementEntity>();
    public DbSet<TimesheetWeekEntity> TimesheetWeeks => Set<TimesheetWeekEntity>();
    public DbSet<TimeEntryEntity> TimeEntries => Set<TimeEntryEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceLineEntity> InvoiceLines => Set<InvoiceLineEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot translate DateTimeOffset ordering; ticks are exact and sortable.
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToUtcTicksConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<EngagementEntity>(entity =>
        {
            entity.ToTable("Engagements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HourlyRateUsd).HasPrecision(18, 2);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(x => x.ContractorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TimesheetWeekEntity>(entity =>
        {
            entity.ToTable("TimesheetWeeks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ContractorTimeZoneId).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.RejectionComment).HasMaxLength(1000);

            // One week per engagement, keyed by the contractor-local Monday.
            entity.HasIndex(x => new { x.EngagementId, x.WeekStartLocal }).IsUnique();
            entity.HasIndex(x => new { x.ClientId, x.WeekStartLocal });

            entity.HasOne<EngagementEntity>()
                .WithMany()
                .HasForeignKey(x => x.EngagementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Entries)
                .WithOne(x => x.Week)
                .HasForeignKey(x => x.WeekId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TimeEntryEntity>(entity =>
        {
            entity.ToTable("TimeEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(1000);
            entity.HasIndex(x => new { x.WeekId, x.StartUtc });
        });

        modelBuilder.Entity<InvoiceEntity>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TotalApprovedHours).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmountUsd).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.ClientId, x.GeneratedAtUtc });

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.LineItems)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLineEntity>(entity =>
        {
            entity.ToTable("InvoiceLines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ContractorName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.ApprovedHours).HasPrecision(18, 2);
            entity.Property(x => x.HourlyRateUsd).HasPrecision(18, 2);
            entity.Property(x => x.LineTotalUsd).HasPrecision(18, 2);
            entity.HasIndex(x => x.InvoiceId);
        });

        SeedData.Apply(modelBuilder);
    }
}
