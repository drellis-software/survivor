using System.ComponentModel.DataAnnotations;
using BTSurvivorPool.Models;

namespace BTSurvivorPool.DTOs;

public class SubmitPickDto
{
    [Required] public Guid EntryId { get; set; }
    [Required] public int NFLTeamId { get; set; }
    [Range(1, 22)] public int Week { get; set; }
}

public class PickDto
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public int LifeNumber { get; set; }
    public int NFLTeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamAbbreviation { get; set; } = string.Empty;
    public string? TeamLogoUrl { get; set; }
    public int Week { get; set; }
    public string Result { get; set; } = string.Empty;
    public DateTime PickedAt { get; set; }
    public DateTime LockedAt { get; set; }
}

public class AvailableTeamDto
{
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? OpponentName { get; set; }
    public string? OpponentAbbreviation { get; set; }
    public DateTime? GameTimeUtc { get; set; }
    public DateTime? LockTimeUtc { get; set; }
    public bool IsUsed { get; set; }
    public bool IsOnBye { get; set; }
    public bool IsLocked { get; set; }
    public bool IsSelectedThisWeek { get; set; }
    public string? WeekResult { get; set; }
}
