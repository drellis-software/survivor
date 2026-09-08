using BTSurvivorPool.Data;
using BTSurvivorPool.Models;
using BTSurvivorPool.Services;
using BTSurvivorPool.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Tests;

public class PickServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static (ApplicationDbContext db, Guid userId, Guid entryId, int teamId) SetupBasicScenario()
    {
        var db = CreateDb();

        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var team = new NFLTeam { Id = 1, Name = "Chiefs", City = "Kansas City", Abbreviation = "KC", Conference = "AFC", Division = "West" };
        var teamOpponent = new NFLTeam { Id = 2, Name = "Ravens", City = "Baltimore", Abbreviation = "BAL", Conference = "AFC", Division = "North" };
        db.NFLTeams.AddRange(team, teamOpponent);

        var season = new Season { Id = 1, Year = 2025, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(100), WeekCount = 18 };
        db.Seasons.Add(season);

        var user = new User { Id = userId, Username = "testuser", PinHash = "x", Email = "t@t.com" };
        db.Users.Add(user);

        var league = new League { Id = leagueId, Name = "Test", SeasonId = 1, AdminUserId = userId, DefaultLives = 1, Status = LeagueStatus.Active };
        db.Leagues.Add(league);

        var entry = new Entry { Id = Guid.NewGuid(), UserId = userId, LeagueId = leagueId, SeasonId = 1, LifeNumber = 1, IsActive = true };
        db.Entries.Add(entry);

        // Sunday game at 1 PM ET = 18:00 UTC on a Sunday
        var nextSunday = DateTime.UtcNow.AddDays((7 - (int)DateTime.UtcNow.DayOfWeek) % 7);
        var gameTime = new DateTime(nextSunday.Year, nextSunday.Month, nextSunday.Day, 18, 0, 0, DateTimeKind.Utc);

        var game = new NFLGame
        {
            Id = Guid.NewGuid(),
            EspnGameId = "test-game-1",
            SeasonId = 1,
            Week = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            GameTimeUtc = gameTime,
            IsThursdayGame = false,
            Status = GameStatus.Scheduled
        };
        db.NFLGames.Add(game);
        db.SaveChanges();

        return (db, userId, entry.Id, 1);
    }

    [Fact]
    public async Task SubmitPick_RejectsWhenGameAlreadyStarted()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var team = new NFLTeam { Id = 1, Name = "Chiefs", City = "Kansas City", Abbreviation = "KC", Conference = "AFC", Division = "West" };
        var opp = new NFLTeam { Id = 2, Name = "Ravens", City = "Baltimore", Abbreviation = "BAL", Conference = "AFC", Division = "North" };
        db.NFLTeams.AddRange(team, opp);

        var season = new Season { Id = 1, Year = 2025, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(100), WeekCount = 18 };
        db.Seasons.Add(season);

        var user = new User { Id = userId, Username = "u", PinHash = "x", Email = "u@u.com" };
        db.Users.Add(user);

        var league = new League { Id = leagueId, Name = "L", SeasonId = 1, AdminUserId = userId, DefaultLives = 1, Status = LeagueStatus.Active };
        db.Leagues.Add(league);

        var entryId = Guid.NewGuid();
        db.Entries.Add(new Entry { Id = entryId, UserId = userId, LeagueId = leagueId, SeasonId = 1, LifeNumber = 1, IsActive = true });

        // Thursday game that already started (in the past)
        var game = new NFLGame
        {
            Id = Guid.NewGuid(),
            EspnGameId = "thu-game",
            SeasonId = 1,
            Week = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            GameTimeUtc = DateTime.UtcNow.AddHours(-2), // started 2 hours ago
            IsThursdayGame = true,
            Status = GameStatus.InProgress
        };
        db.NFLGames.Add(game);
        await db.SaveChangesAsync();

        var service = new PickService(db);
        var dto = new SubmitPickDto { EntryId = entryId, NFLTeamId = 1, Week = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitPickAsync(dto, userId));
    }

    [Fact]
    public async Task SubmitPick_RejectsUsedTeamOnSameLife()
    {
        var (db, userId, entryId, teamId) = SetupBasicScenario();

        // Mark team as already Won on this entry
        var existingPick = new Pick
        {
            Id = Guid.NewGuid(),
            EntryId = entryId,
            NFLTeamId = teamId,
            Week = 1,
            SeasonId = 1,
            Result = PickResult.Won,
            PickedAt = DateTime.UtcNow.AddDays(-7),
            LockedAt = DateTime.UtcNow.AddDays(-7)
        };
        db.Picks.Add(existingPick);
        await db.SaveChangesAsync();

        var service = new PickService(db);

        // Try to pick same team on same entry (week 2)
        var game2 = new NFLGame
        {
            Id = Guid.NewGuid(),
            EspnGameId = "game-w2",
            SeasonId = 1,
            Week = 2,
            HomeTeamId = teamId,
            AwayTeamId = 2,
            GameTimeUtc = DateTime.UtcNow.AddHours(48),
            Status = GameStatus.Scheduled
        };
        db.NFLGames.Add(game2);
        await db.SaveChangesAsync();

        var dto = new SubmitPickDto { EntryId = entryId, NFLTeamId = teamId, Week = 2 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitPickAsync(dto, userId));
    }

    [Fact]
    public async Task GetAvailableTeams_ExcludesByeTeams()
    {
        var (db, userId, entryId, teamId) = SetupBasicScenario();

        var service = new PickService(db);
        var teams = await service.GetAvailableTeamsAsync(entryId, 99, userId); // week 99 = no games

        Assert.All(teams, t => Assert.True(t.IsOnBye));
    }

    [Fact]
    public async Task GetAvailableTeams_ExcludesUsedTeams()
    {
        var (db, userId, entryId, teamId) = SetupBasicScenario();

        var wonPick = new Pick
        {
            Id = Guid.NewGuid(),
            EntryId = entryId,
            NFLTeamId = teamId,
            Week = 1,
            SeasonId = 1,
            Result = PickResult.Won,
            PickedAt = DateTime.UtcNow.AddDays(-7),
            LockedAt = DateTime.UtcNow.AddDays(-7)
        };
        db.Picks.Add(wonPick);
        await db.SaveChangesAsync();

        var service = new PickService(db);
        var teams = await service.GetAvailableTeamsAsync(entryId, 1, userId);

        var usedTeam = teams.FirstOrDefault(t => t.TeamId == teamId);
        Assert.NotNull(usedTeam);
        Assert.True(usedTeam.IsUsed);
    }

    [Fact]
    public void LockTime_ThursdayGame_LocksOneMinuteBeforeKickoff()
    {
        // NFL Week 1 2025: Thursday Sept 4 at 8:20 PM EDT = Sept 5 00:20 UTC
        var thursdayGameTime = new DateTime(2025, 9, 5, 0, 20, 0, DateTimeKind.Utc);
        var game = new NFLGame
        {
            Id = Guid.NewGuid(),
            EspnGameId = "thu",
            SeasonId = 1,
            Week = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            GameTimeUtc = thursdayGameTime,
            IsThursdayGame = true,
            Status = GameStatus.Scheduled
        };

        var lockTime = PickService.ComputeLockTime(game);
        // Should lock 1 minute before kickoff
        Assert.Equal(thursdayGameTime.AddMinutes(-1), lockTime);
    }

    [Fact]
    public void LockTime_SundayAfternoonGame_LocksSundayAtNoon59()
    {
        // Sunday Sept 7 at 1 PM EDT = 17:00 UTC
        var sundayGameTime = new DateTime(2025, 9, 7, 17, 0, 0, DateTimeKind.Utc);
        var game = new NFLGame
        {
            Id = Guid.NewGuid(),
            EspnGameId = "sun",
            SeasonId = 1,
            Week = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            GameTimeUtc = sundayGameTime,
            IsThursdayGame = false,
            Status = GameStatus.Scheduled
        };

        var lockTime = PickService.ComputeLockTime(game);
        // 12:59 PM EDT = 16:59 UTC (EDT = UTC-4)
        var expectedUtc = new DateTime(2025, 9, 7, 16, 59, 0, DateTimeKind.Utc);
        Assert.Equal(expectedUtc, lockTime);
    }

    [Fact]
    public async Task SubmitPick_RejectsUnauthorizedUser()
    {
        var (db, userId, entryId, teamId) = SetupBasicScenario();
        var service = new PickService(db);
        var otherUser = Guid.NewGuid();
        var dto = new SubmitPickDto { EntryId = entryId, NFLTeamId = teamId, Week = 1 };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SubmitPickAsync(dto, otherUser));
    }

    [Fact]
    public async Task SubmitPick_RejectsEliminatedEntry()
    {
        var (db, userId, entryId, teamId) = SetupBasicScenario();
        var entry = await db.Entries.FindAsync(entryId);
        entry!.IsActive = false;
        entry.EliminatedWeek = 1;
        await db.SaveChangesAsync();

        var service = new PickService(db);
        var dto = new SubmitPickDto { EntryId = entryId, NFLTeamId = teamId, Week = 1 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitPickAsync(dto, userId));
    }
}
