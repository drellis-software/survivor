using BTSurvivorPool.Data;
using BTSurvivorPool.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Controllers;

[ApiController]
[Route("api/schedule")]
public class ScheduleController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("week/{week:int}")]
    public async Task<IActionResult> GetWeek(int week, [FromQuery] int? seasonId)
    {
        var query = db.NFLGames
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .Where(g => g.Week == week);

        if (seasonId.HasValue)
            query = query.Where(g => g.SeasonId == seasonId.Value);
        else
            query = query.Where(g => g.Season.IsActive);

        var games = await query
            .OrderBy(g => g.GameTimeUtc)
            .Select(g => new GameScheduleDto
            {
                Id = g.Id,
                Week = g.Week,
                HomeTeamId = g.HomeTeamId,
                HomeTeamName = g.HomeTeam.Name,
                HomeTeamAbbreviation = g.HomeTeam.Abbreviation,
                HomeTeamLogo = g.HomeTeam.LogoUrl,
                AwayTeamId = g.AwayTeamId,
                AwayTeamName = g.AwayTeam.Name,
                AwayTeamAbbreviation = g.AwayTeam.Abbreviation,
                AwayTeamLogo = g.AwayTeam.LogoUrl,
                GameTimeUtc = g.GameTimeUtc,
                IsThursdayGame = g.IsThursdayGame,
                Status = g.Status.ToString(),
                HomeScore = g.HomeScore,
                AwayScore = g.AwayScore
            })
            .ToListAsync();

        return Ok(games);
    }
}
