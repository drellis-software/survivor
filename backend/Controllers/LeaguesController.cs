using System.Security.Claims;
using BTSurvivorPool.DTOs;
using BTSurvivorPool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BTSurvivorPool.Controllers;

[ApiController]
[Route("api/leagues")]
[Authorize]
public class LeaguesController(ILeagueService leagues) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic() => Ok(await leagues.GetPublicLeaguesAsync());

    [HttpPost]
    [Authorize(Roles = "LeagueAdmin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateLeagueDto dto)
    {
        try
        {
            var result = await leagues.CreateAsync(dto, UserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var league = await leagues.GetByIdAsync(id);
        return league == null ? NotFound() : Ok(league);
    }

    [HttpGet("{id:guid}/standings")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStandings(Guid id) =>
        Ok(await leagues.GetStandingsAsync(id));

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, [FromBody] JoinLeagueDto dto)
    {
        try
        {
            await leagues.JoinAsync(id, UserId, dto.InviteCode);
            return Ok(new { message = "Joined league successfully." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        try
        {
            return Ok(await leagues.GetMembersAsync(id, UserId));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{id:guid}/members/{userId:guid}/lives")]
    public async Task<IActionResult> UpdateLives(Guid id, Guid userId, [FromBody] UpdateMemberLivesDto dto)
    {
        try
        {
            await leagues.UpdateMemberLivesAsync(id, userId, dto.Lives, UserId);
            return Ok(new { message = "Lives updated." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/members/{userId:guid}/approve")]
    public async Task<IActionResult> ApproveMember(Guid id, Guid userId)
    {
        try
        {
            await leagues.ApproveMemberAsync(id, userId, UserId);
            return Ok(new { message = "Member approved." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        try
        {
            await leagues.ActivateLeagueAsync(id, UserId);
            return Ok(new { message = "League activated. Entries created for all approved members." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
}
