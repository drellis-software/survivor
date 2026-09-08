using System.Security.Claims;
using BTSurvivorPool.DTOs;
using BTSurvivorPool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTSurvivorPool.Controllers;

[ApiController]
[Route("api/picks")]
[Authorize]
public class PicksController(IPickService picks) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> SubmitPick([FromBody] SubmitPickDto dto)
    {
        try
        {
            var result = await picks.SubmitPickAsync(dto, UserId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetPick([FromQuery] Guid entryId, [FromQuery] int week)
    {
        try
        {
            var pick = await picks.GetPickAsync(entryId, week, UserId);
            return pick == null ? NoContent() : Ok(pick);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("available-teams")]
    public async Task<IActionResult> GetAvailableTeams([FromQuery] Guid entryId, [FromQuery] int week)
    {
        try
        {
            var teams = await picks.GetAvailableTeamsAsync(entryId, week, UserId);
            return Ok(teams);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
