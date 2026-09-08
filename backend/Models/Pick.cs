namespace BTSurvivorPool.Models;

public class Pick
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public Entry Entry { get; set; } = null!;
    public int NFLTeamId { get; set; }
    public NFLTeam NFLTeam { get; set; } = null!;
    public int Week { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public PickResult Result { get; set; } = PickResult.Pending;
    public DateTime PickedAt { get; set; } = DateTime.UtcNow;
    public DateTime LockedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public enum PickResult
{
    Pending,
    Won,
    Lost,
    Tie
}
