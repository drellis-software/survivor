using BTSurvivorPool.Data;
using BTSurvivorPool.DTOs;
using BTSurvivorPool.Models;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Services;

public class PickService(ApplicationDbContext db) : IPickService
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

    public async Task<PickDto> SubmitPickAsync(SubmitPickDto dto, Guid userId)
    {
        var entry = await db.Entries
            .Include(e => e.League)
            .FirstOrDefaultAsync(e => e.Id == dto.EntryId)
            ?? throw new KeyNotFoundException("Entry not found.");

        if (entry.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this entry.");

        if (!entry.IsActive)
            throw new InvalidOperationException("This life has been eliminated.");

        var team = await db.NFLTeams.FindAsync(dto.NFLTeamId)
            ?? throw new KeyNotFoundException("Team not found.");

        // Get the game for this team this week
        var season = await db.Seasons.FirstOrDefaultAsync(s => s.IsActive)
            ?? throw new InvalidOperationException("No active season.");

        var game = await db.NFLGames
            .FirstOrDefaultAsync(g => g.SeasonId == season.Id
                && g.Week == dto.Week
                && (g.HomeTeamId == dto.NFLTeamId || g.AwayTeamId == dto.NFLTeamId));

        if (game == null)
            throw new InvalidOperationException("This team has a bye week and cannot be picked.");

        // Compute lock time
        var lockTime = ComputeLockTime(game);

        if (DateTime.UtcNow >= lockTime)
            throw new InvalidOperationException("Picks are locked for this team.");

        // Check if this team was already used (Won or Tie) on this entry
        var isUsed = await db.Picks.AnyAsync(p =>
            p.EntryId == dto.EntryId
            && p.NFLTeamId == dto.NFLTeamId
            && (p.Result == PickResult.Won || p.Result == PickResult.Tie));

        if (isUsed)
            throw new InvalidOperationException("You already used this team on this life.");

        // Remove existing pick for this week if any (allow change before lock)
        var existing = await db.Picks
            .FirstOrDefaultAsync(p => p.EntryId == dto.EntryId && p.Week == dto.Week);

        if (existing != null)
        {
            if (DateTime.UtcNow >= existing.LockedAt)
                throw new InvalidOperationException("Your previous pick for this week is already locked.");
            db.Picks.Remove(existing);
        }

        var pick = new Pick
        {
            Id = Guid.NewGuid(),
            EntryId = dto.EntryId,
            NFLTeamId = dto.NFLTeamId,
            Week = dto.Week,
            SeasonId = season.Id,
            Result = PickResult.Pending,
            PickedAt = DateTime.UtcNow,
            LockedAt = lockTime
        };

        db.Picks.Add(pick);
        await db.SaveChangesAsync();

        return ToPickDto(pick, team, entry.LifeNumber);
    }

    public async Task<PickDto?> GetPickAsync(Guid entryId, int week, Guid userId)
    {
        var entry = await db.Entries.FindAsync(entryId)
            ?? throw new KeyNotFoundException("Entry not found.");

        if (entry.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this entry.");

        var pick = await db.Picks
            .Include(p => p.NFLTeam)
            .FirstOrDefaultAsync(p => p.EntryId == entryId && p.Week == week);

        if (pick == null) return null;

        return ToPickDto(pick, pick.NFLTeam, entry.LifeNumber);
    }

    public async Task<List<AvailableTeamDto>> GetAvailableTeamsAsync(Guid entryId, int week, Guid userId)
    {
        var entry = await db.Entries.FindAsync(entryId)
            ?? throw new KeyNotFoundException("Entry not found.");

        if (entry.UserId != userId)
            throw new UnauthorizedAccessException("You do not own this entry.");

        var season = await db.Seasons.FirstOrDefaultAsync(s => s.IsActive)
            ?? throw new InvalidOperationException("No active season.");

        var allTeams = await db.NFLTeams.ToListAsync();

        var gamesThisWeek = await db.NFLGames
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .Where(g => g.SeasonId == season.Id && g.Week == week)
            .ToListAsync();

        // Teams used (Won or Tie) on this specific entry
        var usedTeamIdsList = await db.Picks
            .Where(p => p.EntryId == entryId
                && (p.Result == PickResult.Won || p.Result == PickResult.Tie))
            .Select(p => p.NFLTeamId)
            .ToListAsync();
        var usedTeamIds = usedTeamIdsList.ToHashSet();

        // Current pick this week for this entry
        var currentPick = await db.Picks
            .FirstOrDefaultAsync(p => p.EntryId == entryId && p.Week == week);

        var now = DateTime.UtcNow;

        return allTeams.Select(team =>
        {
            var game = gamesThisWeek.FirstOrDefault(g =>
                g.HomeTeamId == team.Id || g.AwayTeamId == team.Id);

            bool isOnBye = game == null;
            DateTime? lockTimeUtc = game != null ? ComputeLockTime(game) : null;
            bool isLocked = lockTimeUtc.HasValue && now >= lockTimeUtc.Value;
            bool isUsed = usedTeamIds.Contains(team.Id);
            bool isSelected = currentPick?.NFLTeamId == team.Id;

            var opponent = game != null
                ? (game.HomeTeamId == team.Id ? game.AwayTeam : game.HomeTeam)
                : null;

            string? weekResult = null;
            if (currentPick?.NFLTeamId == team.Id)
                weekResult = currentPick.Result.ToString();

            return new AvailableTeamDto
            {
                TeamId = team.Id,
                Name = team.Name,
                City = team.City,
                Abbreviation = team.Abbreviation,
                LogoUrl = team.LogoUrl,
                PrimaryColor = team.PrimaryColor,
                OpponentName = opponent?.Name,
                OpponentAbbreviation = opponent?.Abbreviation,
                GameTimeUtc = game?.GameTimeUtc,
                LockTimeUtc = lockTimeUtc,
                IsUsed = isUsed,
                IsOnBye = isOnBye,
                IsLocked = isLocked,
                IsSelectedThisWeek = isSelected,
                WeekResult = weekResult
            };
        })
        .OrderBy(t => t.IsUsed || t.IsOnBye)
        .ThenBy(t => t.GameTimeUtc)
        .ThenBy(t => t.Name)
        .ToList();
    }

    public static DateTime ComputeLockTime(NFLGame game)
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

        var gameEt = TimeZoneInfo.ConvertTimeFromUtc(game.GameTimeUtc, eastern);
        var dayNum = (int)gameEt.DayOfWeek; // 0=Sun,1=Mon,...,6=Sat
        // Thu/Fri/Sat → next Sunday; Sun → same day; Mon/Tue → previous Sunday
        int daysToSunday = dayNum == 0 ? 0 : (dayNum <= 2 ? -dayNum : 7 - dayNum);
        var weekSundayEt = gameEt.Date.AddDays(daysToSunday);
        var sundayLockEt = weekSundayEt.AddHours(12).AddMinutes(59);
        var sundayLockUtc = TimeZoneInfo.ConvertTimeToUtc(sundayLockEt, eastern);

        return game.GameTimeUtc < sundayLockUtc
            ? game.GameTimeUtc.AddMinutes(-1)
            : sundayLockUtc;
    }

    private static PickDto ToPickDto(Pick pick, NFLTeam team, int lifeNumber) => new()
    {
        Id = pick.Id,
        EntryId = pick.EntryId,
        LifeNumber = lifeNumber,
        NFLTeamId = pick.NFLTeamId,
        TeamName = team.Name,
        TeamAbbreviation = team.Abbreviation,
        TeamLogoUrl = team.LogoUrl,
        Week = pick.Week,
        Result = pick.Result.ToString(),
        PickedAt = pick.PickedAt,
        LockedAt = pick.LockedAt
    };
}
