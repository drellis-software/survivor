using BTSurvivorPool.Data;
using BTSurvivorPool.DTOs;
using BTSurvivorPool.Models;
using Microsoft.EntityFrameworkCore;

namespace BTSurvivorPool.Services;

public class LeagueService(ApplicationDbContext db) : ILeagueService
{
    public async Task<List<LeagueDto>> GetPublicLeaguesAsync() =>
        await db.Leagues
            .Include(l => l.Season)
            .Include(l => l.AdminUser)
            .Include(l => l.UserLeagues)
            .Where(l => l.IsPublic)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => ToDto(l))
            .ToListAsync();

    public async Task<LeagueDto> CreateAsync(CreateLeagueDto dto, Guid adminUserId)
    {
        var season = await db.Seasons.FindAsync(dto.SeasonId)
            ?? throw new KeyNotFoundException("Season not found.");

        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            SeasonId = dto.SeasonId,
            AdminUserId = adminUserId,
            DefaultLives = dto.DefaultLives,
            IsPublic = dto.IsPublic,
            InviteCode = dto.IsPublic ? null : (dto.InviteCode ?? GenerateInviteCode()),
            RegistrationDeadlineWeek = dto.RegistrationDeadlineWeek,
            Status = LeagueStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        db.Leagues.Add(league);

        // Admin auto-joins and is approved
        db.UserLeagues.Add(new UserLeague
        {
            UserId = adminUserId,
            LeagueId = league.Id,
            LivesGranted = dto.DefaultLives,
            JoinedAt = DateTime.UtcNow,
            IsApproved = true
        });

        await db.SaveChangesAsync();

        return await GetByIdAsync(league.Id) ?? throw new Exception("Failed to create league.");
    }

    public async Task<LeagueDto?> GetByIdAsync(Guid id) =>
        await db.Leagues
            .Include(l => l.Season)
            .Include(l => l.AdminUser)
            .Include(l => l.UserLeagues)
            .Where(l => l.Id == id)
            .Select(l => ToDto(l))
            .FirstOrDefaultAsync();

    public async Task<List<LeagueStandingDto>> GetStandingsAsync(Guid leagueId)
    {
        var members = await db.UserLeagues
            .Include(ul => ul.User)
            .Where(ul => ul.LeagueId == leagueId && ul.IsApproved)
            .ToListAsync();

        var entries = await db.Entries
            .Where(e => e.LeagueId == leagueId)
            .ToListAsync();

        return members.Select(m =>
        {
            var userEntries = entries.Where(e => e.UserId == m.UserId).ToList();
            var activeCount = userEntries.Count(e => e.IsActive);
            var maxWeekSurvived = userEntries.Any()
                ? userEntries.Max(e => e.IsActive
                    ? int.MaxValue
                    : e.EliminatedWeek ?? 0)
                : 0;
            if (maxWeekSurvived == int.MaxValue) maxWeekSurvived = 999;

            return new LeagueStandingDto
            {
                UserId = m.UserId,
                Username = m.User.Username,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                LivesRemaining = activeCount,
                LivesGranted = m.LivesGranted,
                WeeksSurvived = maxWeekSurvived == 999 ? 0 : maxWeekSurvived,
                IsEliminated = activeCount == 0,
                JoinedAt = m.JoinedAt
            };
        })
        .OrderByDescending(s => s.LivesRemaining)
        .ThenByDescending(s => s.WeeksSurvived)
        .ThenBy(s => s.JoinedAt)
        .ToList();
    }

    public async Task JoinAsync(Guid leagueId, Guid userId, string? inviteCode)
    {
        var league = await db.Leagues.FindAsync(leagueId)
            ?? throw new KeyNotFoundException("League not found.");

        if (!league.IsPublic)
        {
            if (string.IsNullOrEmpty(inviteCode) ||
                !string.Equals(league.InviteCode, inviteCode, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Invalid invite code.");
        }

        if (await db.UserLeagues.AnyAsync(ul => ul.UserId == userId && ul.LeagueId == leagueId))
            throw new InvalidOperationException("Already a member of this league.");

        db.UserLeagues.Add(new UserLeague
        {
            UserId = userId,
            LeagueId = leagueId,
            LivesGranted = league.DefaultLives,
            JoinedAt = DateTime.UtcNow,
            IsApproved = league.IsPublic // auto-approve public leagues
        });

        await db.SaveChangesAsync();
    }

    public async Task<List<LeagueMemberDto>> GetMembersAsync(Guid leagueId, Guid requestingUserId)
    {
        var league = await db.Leagues.FindAsync(leagueId)
            ?? throw new KeyNotFoundException("League not found.");

        if (league.AdminUserId != requestingUserId)
            throw new UnauthorizedAccessException("Only the league admin can view members.");

        return await db.UserLeagues
            .Include(ul => ul.User)
            .Where(ul => ul.LeagueId == leagueId)
            .Select(ul => new LeagueMemberDto
            {
                UserId = ul.UserId,
                Username = ul.User.Username,
                FirstName = ul.User.FirstName,
                LastName = ul.User.LastName,
                LivesGranted = ul.LivesGranted,
                IsApproved = ul.IsApproved,
                JoinedAt = ul.JoinedAt
            })
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();
    }

    public async Task UpdateMemberLivesAsync(Guid leagueId, Guid userId, int lives, Guid adminUserId)
    {
        var league = await db.Leagues.FindAsync(leagueId)
            ?? throw new KeyNotFoundException("League not found.");

        if (league.AdminUserId != adminUserId)
            throw new UnauthorizedAccessException("Only the league admin can update lives.");

        var membership = await db.UserLeagues
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeagueId == leagueId)
            ?? throw new KeyNotFoundException("Member not found.");

        membership.LivesGranted = lives;
        await db.SaveChangesAsync();
    }

    public async Task ApproveMemberAsync(Guid leagueId, Guid userId, Guid adminUserId)
    {
        var league = await db.Leagues.FindAsync(leagueId)
            ?? throw new KeyNotFoundException("League not found.");

        if (league.AdminUserId != adminUserId)
            throw new UnauthorizedAccessException("Only the league admin can approve members.");

        var membership = await db.UserLeagues
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeagueId == leagueId)
            ?? throw new KeyNotFoundException("Member not found.");

        membership.IsApproved = true;
        await db.SaveChangesAsync();
    }

    public async Task ActivateLeagueAsync(Guid leagueId, Guid adminUserId)
    {
        var league = await db.Leagues
            .Include(l => l.Season)
            .Include(l => l.UserLeagues)
            .FirstOrDefaultAsync(l => l.Id == leagueId)
            ?? throw new KeyNotFoundException("League not found.");

        if (league.AdminUserId != adminUserId)
            throw new UnauthorizedAccessException("Only the league admin can activate the league.");

        if (league.Status != LeagueStatus.Pending)
            throw new InvalidOperationException("League is already active or completed.");

        // Create Entry records for all approved members
        var approvedMembers = league.UserLeagues.Where(ul => ul.IsApproved).ToList();

        foreach (var member in approvedMembers)
        {
            for (int life = 1; life <= member.LivesGranted; life++)
            {
                db.Entries.Add(new Entry
                {
                    Id = Guid.NewGuid(),
                    UserId = member.UserId,
                    LeagueId = leagueId,
                    SeasonId = league.SeasonId,
                    LifeNumber = life,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        league.Status = LeagueStatus.Active;
        await db.SaveChangesAsync();
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 8)
            .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private static LeagueDto ToDto(League l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        SeasonId = l.SeasonId,
        SeasonYear = l.Season?.Year ?? 0,
        AdminUsername = l.AdminUser?.Username ?? "",
        DefaultLives = l.DefaultLives,
        IsPublic = l.IsPublic,
        InviteCode = l.InviteCode,
        RegistrationDeadlineWeek = l.RegistrationDeadlineWeek,
        Status = l.Status.ToString(),
        MemberCount = l.UserLeagues?.Count ?? 0,
        CreatedAt = l.CreatedAt
    };
}
