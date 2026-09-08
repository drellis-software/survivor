namespace BTSurvivorPool.Models;

public class League
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public Guid AdminUserId { get; set; }
    public User AdminUser { get; set; } = null!;
    public int DefaultLives { get; set; } = 1;
    public bool IsPublic { get; set; }
    public string? InviteCode { get; set; }
    public int RegistrationDeadlineWeek { get; set; } = 1;
    public LeagueStatus Status { get; set; } = LeagueStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserLeague> UserLeagues { get; set; } = new List<UserLeague>();
    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}

public enum LeagueStatus
{
    Pending,
    Active,
    Completed
}
