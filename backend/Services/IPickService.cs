using BTSurvivorPool.DTOs;

namespace BTSurvivorPool.Services;

public interface IPickService
{
    Task<PickDto> SubmitPickAsync(SubmitPickDto dto, Guid userId);
    Task<PickDto?> GetPickAsync(Guid entryId, int week, Guid userId);
    Task<List<AvailableTeamDto>> GetAvailableTeamsAsync(Guid entryId, int week, Guid userId);
}
