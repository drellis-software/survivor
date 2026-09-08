using BTSurvivorPool.Data;
using BTSurvivorPool.Jobs;
using BTSurvivorPool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Controllers;

[ApiController]
[Route("api/admin/jobs")]
[Authorize(Roles = "SuperAdmin")]
public class AdminJobsController(
    ScoreProcessingJob scoreJob,
    ISeasonService seasonService,
    ApplicationDbContext db) : ControllerBase
{
    [HttpPost("process-scores")]
    public async Task<IActionResult> ProcessScores([FromBody] ProcessScoresDto dto)
    {
        var season = await db.Seasons.FindAsync(dto.SeasonId);
        if (season == null) return NotFound(new { error = "Season not found." });

        await scoreJob.ProcessWeekAsync(season, dto.Week);
        return Ok(new { message = $"Scores processed for Week {dto.Week}." });
    }

    [HttpPost("seed-schedule")]
    public async Task<IActionResult> SeedSchedule([FromBody] SeedScheduleDto dto)
    {
        await seasonService.SeedScheduleAsync(dto.SeasonId);
        return Ok(new { message = "Schedule seeded." });
    }
}

public record ProcessScoresDto(int SeasonId, int Week);
public record SeedScheduleDto(int SeasonId);
