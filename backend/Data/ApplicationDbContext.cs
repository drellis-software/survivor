using BTSurvivorPool.Models;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<UserLeague> UserLeagues => Set<UserLeague>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Pick> Picks => Set<Pick>();
    public DbSet<NFLTeam> NFLTeams => Set<NFLTeam>();
    public DbSet<NFLGame> NFLGames => Set<NFLGame>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
        });

        // Season
        modelBuilder.Entity<Season>(e =>
        {
            e.HasIndex(s => s.Year).IsUnique();
        });

        // League
        modelBuilder.Entity<League>(e =>
        {
            e.Property(l => l.Status).HasConversion<string>();
            e.HasOne(l => l.AdminUser)
                .WithMany(u => u.AdminLeagues)
                .HasForeignKey(l => l.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Season)
                .WithMany(s => s.Leagues)
                .HasForeignKey(l => l.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserLeague (composite PK)
        modelBuilder.Entity<UserLeague>(e =>
        {
            e.HasKey(ul => new { ul.UserId, ul.LeagueId });
            e.HasOne(ul => ul.User)
                .WithMany(u => u.UserLeagues)
                .HasForeignKey(ul => ul.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ul => ul.League)
                .WithMany(l => l.UserLeagues)
                .HasForeignKey(ul => ul.LeagueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Entry
        modelBuilder.Entity<Entry>(e =>
        {
            e.HasIndex(en => new { en.UserId, en.LeagueId, en.LifeNumber }).IsUnique();
            e.HasOne(en => en.User)
                .WithMany(u => u.Entries)
                .HasForeignKey(en => en.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(en => en.League)
                .WithMany(l => l.Entries)
                .HasForeignKey(en => en.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Season)
                .WithMany(s => s.Entries)
                .HasForeignKey(en => en.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Pick
        modelBuilder.Entity<Pick>(e =>
        {
            // One pick per life per week
            e.HasIndex(p => new { p.EntryId, p.Week }).IsUnique();
            e.Property(p => p.Result).HasConversion<string>();
            e.HasOne(p => p.Entry)
                .WithMany(en => en.Picks)
                .HasForeignKey(p => p.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.NFLTeam)
                .WithMany(t => t.Picks)
                .HasForeignKey(p => p.NFLTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Season)
                .WithMany(s => s.Picks)
                .HasForeignKey(p => p.SeasonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // NFLTeam
        modelBuilder.Entity<NFLTeam>(e =>
        {
            e.HasIndex(t => t.Abbreviation).IsUnique();
        });

        // NFLGame
        modelBuilder.Entity<NFLGame>(e =>
        {
            e.HasIndex(g => g.EspnGameId).IsUnique();
            e.Property(g => g.Status).HasConversion<string>();
            e.HasOne(g => g.Season)
                .WithMany(s => s.Games)
                .HasForeignKey(g => g.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.HomeTeam)
                .WithMany(t => t.HomeGames)
                .HasForeignKey(g => g.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(g => g.AwayTeam)
                .WithMany(t => t.AwayGames)
                .HasForeignKey(g => g.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(g => g.WinningTeam)
                .WithMany()
                .HasForeignKey(g => g.WinningTeamId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });
    }
}
