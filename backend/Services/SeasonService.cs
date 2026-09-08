using BTSurvivorPool.Data;
using BTSurvivorPool.DTOs;
using BTSurvivorPool.Models;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Services;

public class SeasonService(ApplicationDbContext db, IEspnService espn, ILogger<SeasonService> logger) : ISeasonService
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

    public async Task<List<SeasonDto>> GetAllAsync() =>
        await db.Seasons.OrderByDescending(s => s.Year)
            .Select(s => ToDto(s)).ToListAsync();

    public async Task<SeasonDto?> GetActiveAsync() =>
        await db.Seasons.Where(s => s.IsActive).Select(s => ToDto(s)).FirstOrDefaultAsync();

    public async Task<SeasonDto> CreateAsync(CreateSeasonDto dto)
    {
        if (await db.Seasons.AnyAsync(s => s.Year == dto.Year))
            throw new InvalidOperationException($"Season {dto.Year} already exists.");

        var season = new Season
        {
            Year = dto.Year,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            WeekCount = dto.WeekCount
        };
        db.Seasons.Add(season);
        await db.SaveChangesAsync();
        return ToDto(season);
    }

    public async Task SeedScheduleAsync(int seasonId)
    {
        var season = await db.Seasons.FindAsync(seasonId)
            ?? throw new KeyNotFoundException("Season not found.");

        // Seed teams if not present
        if (!await db.NFLTeams.AnyAsync())
        {
            var espnTeams = await espn.GetTeamsAsync();
            int idCounter = 1;
            foreach (var t in espnTeams)
            {
                db.NFLTeams.Add(new NFLTeam
                {
                    Id = idCounter++,
                    Name = t.Name,
                    City = t.City,
                    Abbreviation = t.Abbreviation,
                    Conference = t.Conference,
                    Division = t.Division,
                    LogoUrl = t.LogoUrl,
                    PrimaryColor = t.Color
                });
            }
            await db.SaveChangesAsync();
        }

        var teams = await db.NFLTeams.ToDictionaryAsync(t => t.Abbreviation);

        // Remove existing games for this season to re-seed cleanly
        var existing = await db.NFLGames.Where(g => g.SeasonId == seasonId).ToListAsync();
        db.NFLGames.RemoveRange(existing);
        await db.SaveChangesAsync();

        for (int week = 1; week <= season.WeekCount; week++)
        {
            var games = await espn.GetWeekScheduleAsync(season.Year, week);
            foreach (var g in games)
            {
                if (!teams.TryGetValue(g.HomeTeamAbbr, out var homeTeam) ||
                    !teams.TryGetValue(g.AwayTeamAbbr, out var awayTeam))
                {
                    logger.LogWarning("Unknown team in game {EspnId}: {Home} vs {Away}", g.EspnId, g.HomeTeamAbbr, g.AwayTeamAbbr);
                    continue;
                }

                var sundayLock = GetSundayLockUtc(g.GameTimeUtc);
                var isThursday = g.GameTimeUtc < sundayLock;

                db.NFLGames.Add(new NFLGame
                {
                    Id = Guid.NewGuid(),
                    EspnGameId = g.EspnId,
                    SeasonId = seasonId,
                    Week = week,
                    HomeTeamId = homeTeam.Id,
                    AwayTeamId = awayTeam.Id,
                    GameTimeUtc = g.GameTimeUtc,
                    IsThursdayGame = isThursday,
                    Status = GameStatus.Scheduled
                });
            }
            await db.SaveChangesAsync();
        }
    }

    public async Task ActivateAsync(int seasonId)
    {
        await db.Seasons.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        await db.Seasons.Where(s => s.Id == seasonId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true));
    }

    private static DateTime GetSundayLockUtc(DateTime gameTimeUtc)
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");
        var gameEt = TimeZoneInfo.ConvertTimeFromUtc(gameTimeUtc, eastern);
        var weekStart = gameEt.AddDays(-(int)gameEt.DayOfWeek); // Sunday
        var sundayLockEt = weekStart.Date.AddHours(12).AddMinutes(59);
        return TimeZoneInfo.ConvertTimeToUtc(sundayLockEt, eastern);
    }

    private static SeasonDto ToDto(Season s) => new()
    {
        Id = s.Id,
        Year = s.Year,
        IsActive = s.IsActive,
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        WeekCount = s.WeekCount
    };
}
