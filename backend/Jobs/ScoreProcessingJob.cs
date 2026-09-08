using BTSurvivorPool.Data;
using BTSurvivorPool.Models;
using BTSurvivorPool.Services;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Jobs;

public class ScoreProcessingJob(
    ApplicationDbContext db,
    IEspnService espn,
    ILogger<ScoreProcessingJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var season = await db.Seasons.FirstOrDefaultAsync(s => s.IsActive, ct);
        if (season == null)
        {
            logger.LogWarning("No active season found. Skipping score processing.");
            return;
        }

        // Determine the last completed week
        var eastern = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");
        var nowEt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, eastern);
        // Tuesday = process week that ended on Sunday
        var lastSunday = nowEt.AddDays(-(int)nowEt.DayOfWeek - 1);

        var pendingGames = await db.NFLGames
            .Where(g => g.SeasonId == season.Id && g.Status != GameStatus.Final
                && g.GameTimeUtc < DateTime.UtcNow.AddHours(-6))
            .OrderBy(g => g.Week)
            .ToListAsync(ct);

        if (!pendingGames.Any())
        {
            logger.LogInformation("No pending games to process.");
            return;
        }

        var weeksToProcess = pendingGames.Select(g => g.Week).Distinct().OrderBy(w => w).ToList();

        foreach (var week in weeksToProcess)
        {
            await ProcessWeekAsync(season, week, ct);
        }
    }

    public async Task ProcessWeekAsync(Season season, int week, CancellationToken ct = default)
    {
        logger.LogInformation("Processing scores for Season {Year} Week {Week}", season.Year, week);

        var espnGames = await espn.GetWeekScheduleAsync(season.Year, week);
        var completedGames = espnGames.Where(g => g.IsCompleted).ToList();

        if (!completedGames.Any())
        {
            logger.LogWarning("No completed games from ESPN for Week {Week}", week);
            return;
        }

        var teams = await db.NFLTeams.ToDictionaryAsync(t => t.Abbreviation, ct);

        // Update NFLGame records
        foreach (var eg in completedGames)
        {
            var game = await db.NFLGames
                .FirstOrDefaultAsync(g => g.EspnGameId == eg.EspnId, ct);

            if (game == null) continue;

            game.HomeScore = eg.HomeScore;
            game.AwayScore = eg.AwayScore;
            game.Status = GameStatus.Final;

            if (eg.HomeScore.HasValue && eg.AwayScore.HasValue)
            {
                if (eg.HomeScore > eg.AwayScore)
                    game.WinningTeamId = game.HomeTeamId;
                else if (eg.AwayScore > eg.HomeScore)
                    game.WinningTeamId = game.AwayTeamId;
                // null WinningTeamId = tie
            }
        }

        await db.SaveChangesAsync(ct);

        // Process all pending picks for this week
        var pendingPicks = await db.Picks
            .Include(p => p.Entry)
            .Include(p => p.NFLTeam)
            .Where(p => p.SeasonId == season.Id && p.Week == week && p.Result == PickResult.Pending)
            .ToListAsync(ct);

        var processedGames = await db.NFLGames
            .Where(g => g.SeasonId == season.Id && g.Week == week && g.Status == GameStatus.Final)
            .ToListAsync(ct);

        foreach (var pick in pendingPicks)
        {
            var game = processedGames.FirstOrDefault(g =>
                g.HomeTeamId == pick.NFLTeamId || g.AwayTeamId == pick.NFLTeamId);

            if (game == null || game.Status != GameStatus.Final) continue;

            pick.ProcessedAt = DateTime.UtcNow;

            if (game.HomeScore == game.AwayScore)
            {
                // Tie: life survives, team consumed (same as Won)
                pick.Result = PickResult.Tie;
            }
            else if (game.WinningTeamId == pick.NFLTeamId)
            {
                pick.Result = PickResult.Won;
            }
            else
            {
                pick.Result = PickResult.Lost;
                pick.Entry.IsActive = false;
                pick.Entry.EliminatedWeek = week;
                logger.LogInformation("Entry {EntryId} eliminated in Week {Week}", pick.EntryId, week);
            }
        }

        await db.SaveChangesAsync(ct);

        // Check league completion
        await CheckLeagueCompletionAsync(season.Id, week, ct);
    }

    private async Task CheckLeagueCompletionAsync(int seasonId, int week, CancellationToken ct)
    {
        var activeLeagues = await db.Leagues
            .Where(l => l.Status == LeagueStatus.Active)
            .ToListAsync(ct);

        foreach (var league in activeLeagues)
        {
            var entries = await db.Entries
                .Where(e => e.LeagueId == league.Id && e.SeasonId == seasonId)
                .ToListAsync(ct);

            if (!entries.Any()) continue;

            var activeEntries = entries.Where(e => e.IsActive).ToList();
            var eliminatedThisWeek = entries
                .Where(e => e.EliminatedWeek == week && !e.IsActive)
                .ToList();

            // Count distinct players (users) with active lives vs eliminated this week
            var activePlayers = activeEntries.Select(e => e.UserId).Distinct().Count();
            var eliminatedPlayersThisWeek = eliminatedThisWeek.Select(e => e.UserId).Distinct().Count();

            if (activePlayers == 0 && eliminatedPlayersThisWeek > 1)
            {
                // Multiple players all eliminated same week → tiebreaker round
                logger.LogInformation("League {LeagueId}: all {Count} players eliminated in Week {Week}. Tiebreaker!", league.Id, eliminatedPlayersThisWeek, week);

                foreach (var entry in eliminatedThisWeek)
                {
                    entry.IsActive = true;
                    entry.EliminatedWeek = null;
                }

                await db.SaveChangesAsync(ct);
            }
            else if (activePlayers <= 1)
            {
                // 0 or 1 player(s) remaining — league is over
                league.Status = LeagueStatus.Completed;
                logger.LogInformation("League {LeagueId} completed after Week {Week}", league.Id, week);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
