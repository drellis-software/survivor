using BTSurvivorPool.DTOs;

namespace BTSurvivorPool.Services;

public interface IEntryService
{
    Task<List<EntryDto>> GetMyEntriesAsync(Guid leagueId, Guid userId);
}
