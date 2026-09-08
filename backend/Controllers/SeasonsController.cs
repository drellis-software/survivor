using BTSurvivorPool.DTOs;
using BTSurvivorPool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTSurvivorPool.Controllers;

[ApiController]
[Route("api/seasons")]
public class SeasonsController(ISeasonService seasons) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await seasons.GetAllAsync());

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var s = await seasons.GetActiveAsync();
        return s == null ? NotFound() : Ok(s);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateSeasonDto dto)
    {
        try
        {
            var result = await seasons.CreateAsync(dto);
            return CreatedAtAction(nameof(GetActive), result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/seed")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SeedSchedule(int id)
    {
        try
        {
            await seasons.SeedScheduleAsync(id);
            return Ok(new { message = "Schedule seeded successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}/activate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Activate(int id)
    {
        try
        {
            await seasons.ActivateAsync(id);
            return Ok(new { message = "Season activated." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
