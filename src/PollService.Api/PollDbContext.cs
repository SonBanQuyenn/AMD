using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace PollService.Api;

public class PollDbContext(DbContextOptions<PollDbContext> options) : DbContext(options)
{
    public DbSet<Poll> Polls => Set<Poll>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poll>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Question).HasMaxLength(500).IsRequired();
            entity.Property(p => p.CreatorToken).HasMaxLength(64).IsRequired();

            // SQL Server has no native array type (unlike Postgres' text[]), so
            // Options is stored as a JSON string in an nvarchar(max) column here.
            // The C# property is still a plain string[] everywhere else in the code.
            entity.Property(p => p.Options)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>())
                .Metadata.SetValueComparer(new ValueComparer<string[]>(
                    (a, b) => a!.SequenceEqual(b!),
                    a => a.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                    a => a.ToArray()));
        });
    }
}
