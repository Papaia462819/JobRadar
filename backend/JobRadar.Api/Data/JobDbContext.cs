using JobRadar.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JobRadar.Api.Data;

public class JobDbContext(DbContextOptions<JobDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<ScanRun> ScanRuns => Set<ScanRun>();

    // SQLite stores DateTime as text without offset and reads it back as
    // Kind=Unspecified; JSON then serializes it without the "Z" suffix and the
    // browser interprets UTC values as LOCAL time (the "last scan 3h ago right
    // after a scan" bug). We only ever store UTC, so restamp the kind on read.
    private static readonly ValueConverter<DateTime, DateTime> Utc =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullable =
        new(v => v, v => v == null ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var job = modelBuilder.Entity<Job>();

        job.Property(j => j.PostedDate).HasConversion(UtcNullable);
        job.Property(j => j.FirstSeenAt).HasConversion(Utc);

        var run = modelBuilder.Entity<ScanRun>();
        run.Property(r => r.StartedAt).HasConversion(Utc);
        run.Property(r => r.FinishedAt).HasConversion(Utc);

        job.Property(j => j.SourceName).HasMaxLength(100);
        job.Property(j => j.ExternalId).HasMaxLength(300);
        job.Property(j => j.Title).HasMaxLength(500);
        job.Property(j => j.Company).HasMaxLength(300);
        job.Property(j => j.Location).HasMaxLength(300);
        job.Property(j => j.Url).HasMaxLength(2000);
        job.Property(j => j.Language).HasMaxLength(8);
        job.Property(j => j.DedupHash).HasMaxLength(64);
        // Stored as text ("New", "Saved", ...) so the DB stays readable/greppable.
        job.Property(j => j.InteractionState).HasConversion<string>().HasMaxLength(20);

        job.HasIndex(j => j.DedupHash).IsUnique();
        job.HasIndex(j => j.Notified);
        job.HasIndex(j => j.InteractionState);
        job.HasIndex(j => j.FirstSeenAt);
    }
}
