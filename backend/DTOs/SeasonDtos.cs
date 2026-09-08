using System.ComponentModel.DataAnnotations;

namespace BTSurvivorPool.DTOs;

public class CreateSeasonDto
{
    [Range(2020, 2050)] public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [Range(1, 22)] public int WeekCount { get; set; } = 18;
}

public class SeasonDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WeekCount { get; set; }
}

public class EntryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid LeagueId { get; set; }
    public int LifeNumber { get; set; }
    public bool IsActive { get; set; }
    public int? EliminatedWeek { get; set; }
}

public class GameScheduleDto
{
    public Guid Id { get; set; }
    public int Week { get; set; }
    public int HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public string HomeTeamAbbreviation { get; set; } = string.Empty;
    public string? HomeTeamLogo { get; set; }
    public int AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = string.Empty;
    public string AwayTeamAbbreviation { get; set; } = string.Empty;
    public string? AwayTeamLogo { get; set; }
    public DateTime GameTimeUtc { get; set; }
    public bool IsThursdayGame { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}
