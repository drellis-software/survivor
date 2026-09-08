namespace BTSurvivorPool.Models;

public class NFLGame
{
    public Guid Id { get; set; }
    public string EspnGameId { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public int Week { get; set; }
    public int HomeTeamId { get; set; }
    public NFLTeam HomeTeam { get; set; } = null!;
    public int AwayTeamId { get; set; }
    public NFLTeam AwayTeam { get; set; } = null!;
    public DateTime GameTimeUtc { get; set; }
    public bool IsThursdayGame { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public int? WinningTeamId { get; set; }
    public NFLTeam? WinningTeam { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Scheduled;
}

public enum GameStatus
{
    Scheduled,
    InProgress,
    Final,
    Postponed
}
