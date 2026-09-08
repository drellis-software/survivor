namespace BTSurvivorPool.Services;

public interface IEspnService
{
    Task<List<EspnGame>> GetWeekScheduleAsync(int year, int week);
    Task<List<EspnTeam>> GetTeamsAsync();
}

public record EspnGame(
    string EspnId,
    int Week,
    string HomeTeamEspnId,
    string HomeTeamAbbr,
    string HomeTeamName,
    string AwayTeamEspnId,
    string AwayTeamAbbr,
    string AwayTeamName,
    DateTime GameTimeUtc,
    bool IsCompleted,
    int? HomeScore,
    int? AwayScore,
    string Status
);

public record EspnTeam(
    string EspnId,
    string Abbreviation,
    string Name,
    string City,
    string Conference,
    string Division,
    string? LogoUrl,
    string? Color
);
