namespace BTSurvivorPool.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Player;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserLeague> UserLeagues { get; set; } = new List<UserLeague>();
    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public ICollection<League> AdminLeagues { get; set; } = new List<League>();
}

public enum UserRole
{
    Player,
    LeagueAdmin,
    SuperAdmin
}
