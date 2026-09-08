using BTSurvivorPool.Data;
using BTSurvivorPool.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Services;

public class EntryService(ApplicationDbContext db) : IEntryService
{
    public async Task<List<EntryDto>> GetMyEntriesAsync(Guid leagueId, Guid userId) =>
        await db.Entries
            .Include(e => e.User)
            .Where(e => e.LeagueId == leagueId && e.UserId == userId)
            .OrderBy(e => e.LifeNumber)
            .Select(e => new EntryDto
            {
                Id = e.Id,
                UserId = e.UserId,
                Username = e.User.Username,
                LeagueId = e.LeagueId,
                LifeNumber = e.LifeNumber,
                IsActive = e.IsActive,
                EliminatedWeek = e.EliminatedWeek
            })
            .ToListAsync();
}
