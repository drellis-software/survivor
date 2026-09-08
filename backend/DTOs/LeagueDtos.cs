using System.ComponentModel.DataAnnotations;
using BTSurvivorPool.Models;

namespace BTSurvivorPool.DTOs;

public class CreateLeagueDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    [Range(1, 5)] public int DefaultLives { get; set; } = 1;
    public bool IsPublic { get; set; }
    public string? InviteCode { get; set; }
    [Range(1, 18)] public int RegistrationDeadlineWeek { get; set; } = 1;
}

public class LeagueDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public int SeasonYear { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public int DefaultLives { get; set; }
    public bool IsPublic { get; set; }
    public string? InviteCode { get; set; }
    public int RegistrationDeadlineWeek { get; set; }
    public string Status { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeagueStandingDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int LivesRemaining { get; set; }
    public int LivesGranted { get; set; }
    public int WeeksSurvived { get; set; }
    public bool IsEliminated { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class JoinLeagueDto
{
    public string? InviteCode { get; set; }
}

public class UpdateMemberLivesDto
{
    [Range(1, 10)] public int Lives { get; set; }
}

public class LeagueMemberDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int LivesGranted { get; set; }
    public bool IsApproved { get; set; }
    public DateTime JoinedAt { get; set; }
}
