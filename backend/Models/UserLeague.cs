namespace BTSurvivorPool.Models;

public class UserLeague
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;
    public int LivesGranted { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; }
}
