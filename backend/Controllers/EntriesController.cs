using System.Security.Claims;
using BTSurvivorPool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTSurvivorPool.Controllers;

[ApiController]
[Route("api/entries")]
[Authorize]
public class EntriesController(IEntryService entries) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("my")]
    public async Task<IActionResult> GetMyEntries([FromQuery] Guid leagueId) =>
        Ok(await entries.GetMyEntriesAsync(leagueId, UserId));
}
