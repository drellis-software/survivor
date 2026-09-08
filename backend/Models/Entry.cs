namespace BTSurvivorPool.Models;

public class Entry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public int LifeNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public int? EliminatedWeek { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Pick> Picks { get; set; } = new List<Pick>();
}
