using BTSurvivorPool.DTOs;

namespace BTSurvivorPool.Services;

public interface ILeagueService
{
    Task<List<LeagueDto>> GetPublicLeaguesAsync();
    Task<LeagueDto> CreateAsync(CreateLeagueDto dto, Guid adminUserId);
    Task<LeagueDto?> GetByIdAsync(Guid id);
    Task<List<LeagueStandingDto>> GetStandingsAsync(Guid leagueId);
    Task JoinAsync(Guid leagueId, Guid userId, string? inviteCode);
    Task<List<LeagueMemberDto>> GetMembersAsync(Guid leagueId, Guid requestingUserId);
    Task UpdateMemberLivesAsync(Guid leagueId, Guid userId, int lives, Guid adminUserId);
    Task ApproveMemberAsync(Guid leagueId, Guid userId, Guid adminUserId);
    Task ActivateLeagueAsync(Guid leagueId, Guid adminUserId);
}
