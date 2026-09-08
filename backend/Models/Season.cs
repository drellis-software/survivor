namespace BTSurvivorPool.Models;

public class Season
{
    public int Id { get; set; }
    public int Year { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WeekCount { get; set; } = 18;

    public ICollection<League> Leagues { get; set; } = new List<League>();
    public ICollection<NFLGame> Games { get; set; } = new List<NFLGame>();
    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public ICollection<Pick> Picks { get; set; } = new List<Pick>();
}
