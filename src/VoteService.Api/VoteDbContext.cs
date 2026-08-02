using Microsoft.EntityFrameworkCore;

namespace VoteService.Api;

public class VoteDbContext(DbContextOptions<VoteDbContext> options) : DbContext(options)
{
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vote>(entity =>
        {
            // One voter token can only vote once per poll - this is what stops double voting.
            entity.HasIndex(v => new { v.PollCode, v.VoterToken }).IsUnique();
            entity.HasIndex(v => v.PollCode); // speeds up "count votes for this poll"
        });
    }
}
