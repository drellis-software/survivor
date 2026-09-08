using BTSurvivorPool.Data;
using BTSurvivorPool.Jobs;
using BTSurvivorPool.Models;
using BTSurvivorPool.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BTSurvivorPool.Tests;

public class ScoreProcessingJobTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ProcessWeek_TieGame_KeepsEntryActiveAndConsumesteam()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var homeTeam = new NFLTeam { Id = 1, Name = "Chiefs", City = "KC", Abbreviation = "KC", Conference = "AFC", Division = "West" };
        var awayTeam = new NFLTeam { Id = 2, Name = "Ravens", City = "BAL", Abbreviation = "BAL", Conference = "AFC", Division = "North" };
        db.NFLTeams.AddRange(homeTeam, awayTeam);

        var season = new Season { Id = 1, Year = 2025, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow.AddDays(100), WeekCount = 18 };
        db.Seasons.Add(season);

        var user = new User { Id = userId, Username = "u", PinHash = "x", Email = "u@u.com" };
        db.Users.Add(user);

        var league = new League { Id = leagueId, Name = "L", SeasonId = 1, AdminUserId = userId, DefaultLives = 1, Status = LeagueStatus.Active };
        db.Leagues.Add(league);

        var entryId = Guid.NewGuid();
        var entry = new Entry { Id = entryId, UserId = userId, LeagueId = leagueId, SeasonId = 1, LifeNumber = 1, IsActive = true };
        db.Entries.Add(entry);

        var game = new NFLGame
        {
            Id = Guid.NewGuid(),
            EspnGameId = "tie-game",
            SeasonId = 1, Week = 1,
            HomeTeamId = 1, AwayTeamId = 2,
            GameTimeUtc = DateTime.UtcNow.AddHours(-3),
            Status = GameStatus.Scheduled
        };
        db.NFLGames.Add(game);

        var pick = new Pick
        {
            Id = Guid.NewGuid(),
            EntryId = entryId,
            NFLTeamId = 1,
            Week = 1, SeasonId = 1,
            Result = PickResult.Pending,
            PickedAt = DateTime.UtcNow.AddHours(-4),
            LockedAt = DateTime.UtcNow.AddHours(-4)
        };
        db.Picks.Add(pick);
        await db.SaveChangesAsync();

        // Mock ESPN to return a tied game
        var espnMock = new Mock<IEspnService>();
        espnMock.Setup(e => e.GetWeekScheduleAsync(2025, 1)).ReturnsAsync(new List<EspnGame>
        {
            new("tie-game", 1, "1", "KC", "Chiefs", "2", "BAL", "Ravens", DateTime.UtcNow.AddHours(-3), true, 17, 17, "STATUS_FINAL")
        });

        var job = new ScoreProcessingJob(db, espnMock.Object, NullLogger<ScoreProcessingJob>.Instance);
        await job.ProcessWeekAsync(season, 1);

        var updatedPick = await db.Picks.FindAsync(pick.Id);
        var updatedEntry = await db.Entries.FindAsync(entryId);

        Assert.Equal(PickResult.Tie, updatedPick!.Result);
        Assert.True(updatedEntry!.IsActive); // Tie = life survives
    }

    [Fact]
    public async Task ProcessWeek_Loss_DeactivatesEntry()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var homeTeam = new NFLTeam { Id = 1, Name = "Chiefs", City = "KC", Abbreviation = "KC", Conference = "AFC", Division = "West" };
        var awayTeam = new NFLTeam { Id = 2, Name = "Ravens", City = "BAL", Abbreviation = "BAL", Conference = "AFC", Division = "North" };
        db.NFLTeams.AddRange(homeTeam, awayTeam);

        var season = new Season { Id = 1, Year = 2025, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-7), EndDate = DateTime.UtcNow.AddDays(100), WeekCount = 18 };
        db.Seasons.Add(season);
        var user = new User { Id = userId, Username = "u", PinHash = "x", Email = "u@u.com" };
        db.Users.Add(user);
        var league = new League { Id = leagueId, Name = "L", SeasonId = 1, AdminUserId = userId, DefaultLives = 1, Status = LeagueStatus.Active };
        db.Leagues.Add(league);

        var entryId = Guid.NewGuid();
        var entry = new Entry { Id = entryId, UserId = userId, LeagueId = leagueId, SeasonId = 1, LifeNumber = 1, IsActive = true };
        db.Entries.Add(entry);

        var game = new NFLGame
        {
            Id = Guid.NewGuid(), EspnGameId = "loss-game", SeasonId = 1, Week = 1,
            HomeTeamId = 1, AwayTeamId = 2, GameTimeUtc = DateTime.UtcNow.AddHours(-3), Status = GameStatus.Scheduled
        };
        db.NFLGames.Add(game);

        // Player picked team 1 (home), but team 2 wins
        var pick = new Pick
        {
            Id = Guid.NewGuid(), EntryId = entryId, NFLTeamId = 1,
            Week = 1, SeasonId = 1, Result = PickResult.Pending,
            PickedAt = DateTime.UtcNow.AddHours(-4), LockedAt = DateTime.UtcNow.AddHours(-4)
        };
        db.Picks.Add(pick);
        await db.SaveChangesAsync();

        var espnMock = new Mock<IEspnService>();
        espnMock.Setup(e => e.GetWeekScheduleAsync(2025, 1)).ReturnsAsync(new List<EspnGame>
        {
            new("loss-game", 1, "1", "KC", "Chiefs", "2", "BAL", "Ravens", DateTime.UtcNow.AddHours(-3), true, 14, 27, "STATUS_FINAL")
        });

        var job = new ScoreProcessingJob(db, espnMock.Object, NullLogger<ScoreProcessingJob>.Instance);
        await job.ProcessWeekAsync(season, 1);

        var updatedPick = await db.Picks.FindAsync(pick.Id);
        var updatedEntry = await db.Entries.FindAsync(entryId);

        Assert.Equal(PickResult.Lost, updatedPick!.Result);
        Assert.False(updatedEntry!.IsActive);
        Assert.Equal(1, updatedEntry.EliminatedWeek);
    }
}
