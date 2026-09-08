using System.Text.Json;

namespace BTSurvivorPool.Services;

public class EspnService(HttpClient http, ILogger<EspnService> logger) : IEspnService
{
    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports/football/nfl";

    public async Task<List<EspnGame>> GetWeekScheduleAsync(int year, int week)
    {
        var url = $"{BaseUrl}/scoreboard?seasontype=2&week={week}&dates={year}";
        try
        {
            var response = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var events = doc.RootElement.GetProperty("events");
            var games = new List<EspnGame>();

            foreach (var ev in events.EnumerateArray())
            {
                var espnId = ev.GetProperty("id").GetString()!;
                var dateStr = ev.GetProperty("date").GetString()!;
                var gameTime = DateTime.Parse(dateStr, null, System.Globalization.DateTimeStyles.RoundtripKind);
                var competition = ev.GetProperty("competitions")[0];
                var competitors = competition.GetProperty("competitors");
                var statusName = competition.GetProperty("status").GetProperty("type").GetProperty("name").GetString()!;
                var isCompleted = competition.GetProperty("status").GetProperty("type").GetProperty("completed").GetBoolean();

                string homeAbbr = "", homeName = "", homeId = "", awayAbbr = "", awayName = "", awayId = "";
                int? homeScore = null, awayScore = null;

                foreach (var comp in competitors.EnumerateArray())
                {
                    var isHome = comp.GetProperty("homeAway").GetString() == "home";
                    var team = comp.GetProperty("team");
                    var abbr = team.GetProperty("abbreviation").GetString()!;
                    var name = team.GetProperty("displayName").GetString()!;
                    var id = team.GetProperty("id").GetString()!;
                    var scoreStr = comp.TryGetProperty("score", out var sc) ? sc.GetString() : null;
                    var score = int.TryParse(scoreStr, out var s) ? (int?)s : null;

                    if (isHome) { homeAbbr = abbr; homeName = name; homeId = id; homeScore = score; }
                    else { awayAbbr = abbr; awayName = name; awayId = id; awayScore = score; }
                }

                games.Add(new EspnGame(espnId, week, homeId, homeAbbr, homeName, awayId, awayAbbr, awayName,
                    gameTime, isCompleted, homeScore, awayScore, statusName));
            }

            return games;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch ESPN schedule for year={Year} week={Week}", year, week);
            return new List<EspnGame>();
        }
    }

    public async Task<List<EspnTeam>> GetTeamsAsync()
    {
        var url = $"{BaseUrl}/teams?limit=100";
        try
        {
            var response = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var teams = doc.RootElement.GetProperty("sports")[0]
                .GetProperty("leagues")[0]
                .GetProperty("teams");

            var result = new List<EspnTeam>();
            foreach (var t in teams.EnumerateArray())
            {
                var team = t.GetProperty("team");
                var id = team.GetProperty("id").GetString()!;
                var abbr = team.GetProperty("abbreviation").GetString()!;
                var name = team.GetProperty("name").GetString()!;
                var city = team.GetProperty("location").GetString()!;

                string conf = "", div = "";
                if (team.TryGetProperty("groups", out var groups))
                {
                    conf = groups.TryGetProperty("parent", out var parent)
                        ? parent.GetProperty("shortName").GetString() ?? ""
                        : "";
                    div = groups.TryGetProperty("shortName", out var divName)
                        ? divName.GetString() ?? ""
                        : "";
                }

                string? logoUrl = null;
                if (team.TryGetProperty("logos", out var logos) && logos.GetArrayLength() > 0)
                    logoUrl = logos[0].GetProperty("href").GetString();

                string? color = team.TryGetProperty("color", out var col) ? $"#{col.GetString()}" : null;

                result.Add(new EspnTeam(id, abbr, name, city, conf, div, logoUrl, color));
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch ESPN teams");
            return new List<EspnTeam>();
        }
    }
}
