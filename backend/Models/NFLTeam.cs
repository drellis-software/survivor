namespace BTSurvivorPool.Models;

public class NFLTeam
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string Conference { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }

    public ICollection<NFLGame> HomeGames { get; set; } = new List<NFLGame>();
    public ICollection<NFLGame> AwayGames { get; set; } = new List<NFLGame>();
    public ICollection<Pick> Picks { get; set; } = new List<Pick>();
}
