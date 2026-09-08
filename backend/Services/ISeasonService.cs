using BTSurvivorPool.DTOs;

namespace BTSurvivorPool.Services;

public interface ISeasonService
{
    Task<List<SeasonDto>> GetAllAsync();
    Task<SeasonDto?> GetActiveAsync();
    Task<SeasonDto> CreateAsync(CreateSeasonDto dto);
    Task SeedScheduleAsync(int seasonId);
    Task ActivateAsync(int seasonId);
}
