# BTSurvivorPool — Ralph Loop Build Prompt

You are building a production-ready NFL Survivor Pool application from scratch.
Read the git log and all existing files FIRST each iteration to understand what is already built,
then continue from exactly where the previous iteration left off.
Do NOT rewrite files that are already correct. Only add what is missing or fix what is broken.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8 ASP.NET Core Web API (single project at `/backend`) |
| ORM | EF Core 8 + Npgsql (PostgreSQL) |
| Auth | JWT (8hr expiry) + BCrypt PIN hashing |
| Jobs | Hangfire (PostgreSQL storage) |
| Rate limiting | ASP.NET Core built-in rate limiting middleware |
| Frontend | React 18 + Vite + TypeScript + Tailwind CSS v3 + React Query v5 + React Router v6 |
| Testing | xUnit + Moq (backend), Vitest + React Testing Library (frontend) |

---

## Repository Structure

```
/backend          .NET 8 API
  /Controllers
  /Services
  /Models         EF Core entities
  /DTOs
  /Data           ApplicationDbContext + migrations
  /Jobs           Hangfire background jobs
  /Middleware
  Program.cs
  backend.csproj

/frontend         React + Vite app
  /src
    /components
    /pages
    /hooks
    /lib
    /types
  package.json
  vite.config.ts
  tailwind.config.ts
  tsconfig.json
```

---

## Complete Data Model

### Entities (EF Core)

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }        // unique, indexed
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }            // unique, indexed
    public string PinHash { get; set; }          // BCrypt of 4-6 digit PIN
    public UserRole Role { get; set; }           // Player, LeagueAdmin, SuperAdmin
    public DateTime CreatedAt { get; set; }
    public ICollection<UserLeague> UserLeagues { get; set; }
    public ICollection<Entry> Entries { get; set; }
}

public enum UserRole { Player, LeagueAdmin, SuperAdmin }

public class Season
{
    public int Id { get; set; }
    public int Year { get; set; }               // unique
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int WeekCount { get; set; }           // 18 for regular season
    public ICollection<League> Leagues { get; set; }
    public ICollection<NFLGame> Games { get; set; }
}

public class League
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; }
    public Guid AdminUserId { get; set; }
    public User AdminUser { get; set; }
    public int DefaultLives { get; set; }        // 1-5
    public bool IsPublic { get; set; }
    public string? InviteCode { get; set; }      // 8-char alphanumeric, null if public
    public int RegistrationDeadlineWeek { get; set; }  // no joins after this week
    public LeagueStatus Status { get; set; }     // Pending, Active, Completed
    public DateTime CreatedAt { get; set; }
    public ICollection<UserLeague> UserLeagues { get; set; }
    public ICollection<Entry> Entries { get; set; }
}

public enum LeagueStatus { Pending, Active, Completed }

public class UserLeague
{
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid LeagueId { get; set; }
    public League League { get; set; }
    public int LivesGranted { get; set; }        // admin override (defaults to League.DefaultLives)
    public DateTime JoinedAt { get; set; }
    public bool IsApproved { get; set; }         // admin must approve before league activation
}

// Entry = one "life" for a player in a league
public class Entry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public Guid LeagueId { get; set; }
    public League League { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; }
    public int LifeNumber { get; set; }          // 1, 2, 3...
    public bool IsActive { get; set; }
    public int? EliminatedWeek { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Pick> Picks { get; set; }
}

public class Pick
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public Entry Entry { get; set; }
    public int NFLTeamId { get; set; }
    public NFLTeam NFLTeam { get; set; }
    public int Week { get; set; }
    public int SeasonId { get; set; }
    public PickResult Result { get; set; }       // Pending, Won, Lost, Tie
    public DateTime PickedAt { get; set; }
    public DateTime LockedAt { get; set; }       // computed at pick time
    public DateTime? ProcessedAt { get; set; }
}

public enum PickResult { Pending, Won, Lost, Tie }

public class NFLTeam
{
    public int Id { get; set; }
    public string Name { get; set; }             // e.g. "Chiefs"
    public string City { get; set; }             // e.g. "Kansas City"
    public string Abbreviation { get; set; }     // e.g. "KC"
    public string Conference { get; set; }       // AFC, NFC
    public string Division { get; set; }         // e.g. "West"
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }    // hex, e.g. "#E31837"
    public ICollection<NFLGame> HomeGames { get; set; }
    public ICollection<NFLGame> AwayGames { get; set; }
    public ICollection<Pick> Picks { get; set; }
}

public class NFLGame
{
    public Guid Id { get; set; }
    public string EspnGameId { get; set; }       // unique, from ESPN API
    public int SeasonId { get; set; }
    public Season Season { get; set; }
    public int Week { get; set; }
    public int HomeTeamId { get; set; }
    public NFLTeam HomeTeam { get; set; }
    public int AwayTeamId { get; set; }
    public NFLTeam AwayTeam { get; set; }
    public DateTime GameTimeUtc { get; set; }
    public bool IsThursdayGame { get; set; }     // any game before Sunday 12:59 PM ET
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public int? WinningTeamId { get; set; }
    public NFLTeam? WinningTeam { get; set; }
    public GameStatus Status { get; set; }       // Scheduled, InProgress, Final, Postponed
}

public enum GameStatus { Scheduled, InProgress, Final, Postponed }
```

### EF Core Constraints
- `User.Username` unique index
- `User.Email` unique index
- `Season.Year` unique index
- `NFLGame.EspnGameId` unique index
- `UserLeague` composite PK: (UserId, LeagueId)
- `Entry` unique index: (UserId, LeagueId, LifeNumber)
- `Pick` unique index: (EntryId, Week) — one pick per life per week
- `Pick` unique index: (EntryId, NFLTeamId) where Result IN (Won, Tie) — enforced in service layer

---

## Game Rules (implement exactly)

1. **One pick per life per week**: `Pick` has unique constraint on (EntryId, Week).
2. **Same team across different lives**: allowed — the unique constraint is per Entry, not per User.
3. **Used teams per life**: A team is "used" on a life when its Pick.Result is Won or Tie. It cannot be picked again on that same Entry. Different Entry = different used-list.
4. **Results**:
   - Won → Entry stays active, team becomes used on that life
   - Lost → Entry.IsActive = false, Entry.EliminatedWeek = week
   - Tie → same as Won (life survives, team consumed)
5. **Pick locking logic** (compute `LockedAt` at time of pick submission):
   ```
   Eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
   gameLocalTime = TimeZoneInfo.ConvertTimeFromUtc(game.GameTimeUtc, Eastern)
   sundayLock = that week's Sunday at 12:59 PM ET (converted to UTC)
   
   if (game.GameTimeUtc < sundayLock) {
       LockedAt = game.GameTimeUtc - 1 minute   // Thursday/international game
   } else {
       LockedAt = sundayLock                     // all other games
   }
   ```
6. **Bye weeks**: Teams with no NFLGame in a given week are excluded from available picks.
7. **Co-winner / tiebreaker**: If all remaining active Entries lose the same week, those players get a new Pick slot the following week (a "tiebreaker week"). Their used-teams lists remain. Repeat if needed.
8. **League completion**: League.Status = Completed when ≤1 Entry is active after a score processing run, OR a tiebreaker has been resolved.

---

## All API Endpoints

### Auth — `/api/auth`
- `POST /register` → `{ username, firstName, lastName, email, pin }` → 201 + `{ token, userId }`
- `POST /login` → `{ username, pin }` → 200 + `{ token, userId, role }`

### Seasons — `/api/seasons` (SuperAdmin only except GET)
- `GET /` → list seasons
- `GET /active` → current active season
- `POST /` → create season `{ year, startDate, endDate, weekCount }`
- `POST /{id}/seed` → seed schedule from ESPN API (weeks 1–WeekCount) + seed 32 NFL teams if missing
- `PUT /{id}/activate` → set IsActive = true, deactivate others

### Leagues — `/api/leagues`
- `GET /` → public leagues list (or all for SuperAdmin)
- `POST /` → create league (LeagueAdmin+) `{ name, seasonId, defaultLives, isPublic, inviteCode?, registrationDeadlineWeek }`
- `GET /{id}` → league detail + standings
- `GET /{id}/standings` → ordered list of players with lives remaining, weeks survived
- `POST /{id}/join` → join by invite code or if public `{ inviteCode? }`
- `GET /{id}/members` → list members (admin only)
- `PUT /{id}/members/{userId}/lives` → override lives granted (admin only) `{ lives }`
- `POST /{id}/members/{userId}/approve` → approve a member (admin only)
- `POST /{id}/activate` → admin activates league: creates Entry records for all approved members

### Entries — `/api/entries`
- `GET /my?leagueId=` → current user's entries (lives) in a league

### Picks — `/api/picks`
- `POST /` → submit pick `{ entryId, nflTeamId, week }` → validates lock, used teams, bye
- `GET /?entryId=&week=` → get pick for an entry this week
- `GET /available-teams?entryId=&week=` → teams available for a specific life this week

### Schedule — `/api/schedule`
- `GET /week/{week}?seasonId=` → NFLGames for a week with team info and lock times

### Admin Jobs — `/api/admin/jobs` (SuperAdmin only)
- `POST /process-scores` → `{ week, seasonId }` → manually trigger score processing
- `POST /seed-schedule` → `{ seasonId }` → re-seed from ESPN

---

## ESPN API Integration

Free unofficial API, no key required.

**Scoreboard endpoint:**
```
GET https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard
    ?seasontype=2&week={week}&dates={year}
```

**Parse response:**
```json
{
  "events": [
    {
      "id": "401671773",
      "date": "2024-09-05T00:20:00Z",
      "week": { "number": 1 },
      "competitions": [{
        "competitors": [
          { "homeAway": "home", "team": { "id": "12", "abbreviation": "KC", "displayName": "Kansas City Chiefs" }, "score": "27" },
          { "homeAway": "away", "team": { "id": "8", "abbreviation": "BAL", "displayName": "Baltimore Ravens" }, "score": "20" }
        ],
        "status": { "type": { "completed": true, "name": "STATUS_FINAL" } }
      }]
    }
  ]
}
```

**EspnService methods:**
- `GetWeekScheduleAsync(int year, int week)` → list of game data
- `GetTeamsAsync()` → list of all NFL teams (use `https://site.api.espn.com/apis/site/v2/sports/football/nfl/teams`)

---

## Hangfire Jobs

**ScoreProcessingJob** (scheduled: Tuesday 9:00 AM ET = "0 14 * * 2" UTC):
1. Determine last completed week (current week - 1)
2. Fetch ESPN scoreboard for that week
3. Update NFLGame records (scores, WinningTeamId, Status = Final)
4. For each Pending Pick in that week:
   - Get the game for the picked team
   - If game.Status != Final → skip (postponed)
   - If WinningTeamId == pick.NFLTeamId → Result = Won
   - If WinningTeamId != pick.NFLTeamId AND WinningTeamId != null → Result = Lost, deactivate Entry
   - If HomeScore == AwayScore (tie) → Result = Tie
5. After processing all picks:
   - Check each active League: count active Entries
   - If league active entry count == 0 → check tiebreaker logic (see game rules)
   - If league active entry count == 1 → set League.Status = Completed

**Register with Hangfire:** `RecurringJob.AddOrUpdate<ScoreProcessingJob>("process-scores", j => j.ExecuteAsync(), "0 14 * * 2");`

---

## Frontend Pages & Components

### Pages
| Route | Component | Description |
|---|---|---|
| `/` | `HomePage` | Landing page with hero, how it works |
| `/login` | `LoginPage` | Username + PIN form |
| `/register` | `RegisterPage` | Create account form |
| `/dashboard` | `DashboardPage` | My leagues, current week summary |
| `/leagues` | `LeaguesPage` | Browse public leagues |
| `/leagues/:id` | `LeaguePage` | Lobby: standings, members, admin panel |
| `/leagues/:id/pick` | `PickPage` | **Main pick screen** |
| `/admin` | `AdminPage` | SuperAdmin: seasons, score processing |

### Pick Screen — `PickPage` (most important UI)

Layout:
```
[Life 1 Tab] [Life 2 Tab] [Life 3 Tab]   ← active life tabs
                                            green dot = active, gray = eliminated

[Week 3 Picks]  [Locked: Sunday 12:59 PM ET]

[ Team Grid — 32 cards ]
┌─────────────────┐  ┌─────────────────┐
│  🏈  KC Chiefs  │  │  🏈  Ravens     │  ← available: white bg, hover green
│  vs BAL         │  │  vs KC          │
│  Sun 1:00 PM    │  │  Sun 1:00 PM    │
└─────────────────┘  └─────────────────┘

┌─────────────────┐  ┌─────────────────┐
│  ~~Patriots~~   │  │  🔒 Bears       │  ← used: gray+strikethrough | locked: red border
│  (used wk 4)    │  │  Game started   │
└─────────────────┘  └─────────────────┘

┌─────────────────┐  ← selected this week: teal/green border + checkmark
│  ✓ Eagles      │
│  vs DAL         │
│  Sun 4:25 PM    │
└─────────────────┘
```

Team card states (CSS classes via Tailwind):
- `available`: white bg, border-gray-200, hover:border-green-500 hover:shadow-md, cursor-pointer
- `selected`: border-green-500 bg-green-50, ring-2 ring-green-500
- `used`: bg-gray-100 border-gray-200, team name has line-through text-gray-400, cursor-not-allowed
- `bye`: bg-gray-50 border-gray-100, "BYE WEEK" label, cursor-not-allowed
- `locked`: bg-red-50 border-red-200, lock icon, cursor-not-allowed
- `won`: bg-green-100 border-green-500, ✓ icon
- `lost`: bg-red-100 border-red-500, ✗ icon, this entry eliminated

Life tabs: show life number, color-coded (green=active, red=eliminated, gray=not yet playing)
Clicking a team: show confirmation modal "Pick Kansas City Chiefs for Life 2?" → Confirm → optimistic update via React Query mutation

### Key Components
- `TeamCard` — reusable, accepts `team`, `state`, `gameInfo`, `onClick`
- `LifeTab` — tab button with status indicator
- `PickConfirmModal` — confirm before submitting
- `StandingsTable` — sortable table of players + lives
- `CountdownTimer` — live countdown to pick lock time
- `AdminPanel` — lives management, approve members

---

## Auth & JWT

JWT claims:
```json
{ "sub": "userId", "username": "...", "role": "Player|LeagueAdmin|SuperAdmin", "exp": ... }
```

Rate limiting (built-in ASP.NET Core):
- Login endpoint: 5 requests per 15 minutes per IP
- All other endpoints: 100 requests per minute per IP

PIN validation:
- 4–6 characters, numeric only
- BCrypt hash stored in `User.PinHash`

---

## NFL Teams Seed Data

The 32 NFL teams with their exact abbreviations, colors, and ESPN IDs must be seeded once.
Use `EspnService.GetTeamsAsync()` to fetch from ESPN and seed into the NFLTeam table.
Only seed if `NFLTeams` table is empty.

---

## Security Checklist

- [ ] All endpoints except /auth/* require `[Authorize]`
- [ ] Admin endpoints use `[Authorize(Roles = "SuperAdmin")]`
- [ ] LeagueAdmin endpoints check that the requesting user is the league admin
- [ ] Picks endpoint validates that the entryId belongs to the current user
- [ ] Rate limiting on /api/auth/login
- [ ] CORS only allows frontend origin (configurable via appsettings)
- [ ] Secrets (JWT key, DB connection) via environment variables / user secrets

---

## Testing Requirements

Backend (xUnit):
1. `PickService_SubmitPick_ThrowsWhenLocked` — pick submitted after lock time is rejected
2. `PickService_SubmitPick_ThrowsWhenTeamUsed` — used team cannot be repicked on same life
3. `PickService_AvailableTeams_ExcludesByeWeekTeams`
4. `PickService_AvailableTeams_ExcludesUsedTeams`
5. `ScoreProcessingJob_ProcessesTieAsWon` — tie result keeps entry active
6. `ScoreProcessingJob_ProcessesLossCorrectly` — loss deactivates entry
7. `AuthService_Login_RejectsWrongPin`
8. `LockTime_ThursdayGame_LocksAtGameTime` — Thursday game locks at gametime - 1 min

Frontend (Vitest):
1. `TeamCard_rendersUsedState` — used team shows strikethrough
2. `TeamCard_rendersLockedState` — locked team shows lock icon
3. `CountdownTimer_displaysCorrectTime`

---

## Program.cs Configuration

```csharp
// JWT Auth
// EF Core + Npgsql
// Hangfire + PostgreSQL storage
// Rate limiting (fixed window)
// CORS (allow frontend origin from config)
// Swagger/OpenAPI
// Health checks: /health
```

---

## appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=survivorpool;Username=postgres;Password=..."
  },
  "JwtSettings": {
    "SecretKey": "REPLACE_WITH_ENV_VAR",
    "ExpiryHours": 8,
    "Issuer": "BTSurvivorPool",
    "Audience": "BTSurvivorPool"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  },
  "Hangfire": {
    "Dashboard": true
  }
}
```

---

## Build Checklist (check off as you complete each item)

### Phase 1 — Backend Foundation
- [ ] `/backend/backend.csproj` with all NuGet packages (Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.AspNetCore.Authentication.JwtBearer, BCrypt.Net-Next, Hangfire, Hangfire.PostgreSql, Swashbuckle)
- [ ] All entity models in `/backend/Models/`
- [ ] `ApplicationDbContext` with all DbSets, relationships, and indexes
- [ ] `appsettings.json` (with placeholder secrets)
- [ ] `Program.cs` with JWT, EF Core, Hangfire, CORS, rate limiting, Swagger configured
- [ ] Initial EF Core migration

### Phase 2 — Backend Core Services
- [ ] `AuthService` — register (PIN validation, BCrypt hash), login (verify PIN, issue JWT)
- [ ] `AuthController` — POST /api/auth/register, POST /api/auth/login
- [ ] `EspnService` — GetWeekScheduleAsync, GetTeamsAsync
- [ ] `SeasonService` + `SeasonsController` — CRUD + seed schedule from ESPN
- [ ] `LeagueService` + `LeaguesController` — CRUD, join, approve, activate, standings
- [ ] `EntryService` + `EntriesController` — create entries on activation, get my entries
- [ ] `PickService` + `PicksController` — submit pick (with full validation), available teams, get pick
- [ ] `ScheduleController` — GET /api/schedule/week/{week}

### Phase 3 — Background Jobs
- [ ] `ScoreProcessingJob` with full result logic (Won/Lost/Tie, deactivate entries, league completion)
- [ ] `AdminJobsController` — manual trigger endpoints
- [ ] Hangfire recurring job registered in Program.cs

### Phase 4 — Frontend
- [ ] Vite + React 18 + TypeScript scaffold
- [ ] Tailwind CSS configured
- [ ] React Router v6 routes
- [ ] React Query v5 setup with auth token injection
- [ ] API client (`/src/lib/api.ts`) — typed fetch wrapper
- [ ] Auth context + useAuth hook
- [ ] `LoginPage` — username + PIN form
- [ ] `RegisterPage`
- [ ] `DashboardPage` — league cards, current picks
- [ ] `LeaguePage` — standings + member list
- [ ] `PickPage` — life tabs + team grid (all card states)
- [ ] `TeamCard` component (all 7 states)
- [ ] `LifeTab` component
- [ ] `PickConfirmModal`
- [ ] `StandingsTable`
- [ ] `CountdownTimer`
- [ ] `AdminPage` (SuperAdmin)
- [ ] `AdminPanel` component for league admins

### Phase 5 — Tests & Polish
- [ ] 8 xUnit backend tests listed above
- [ ] 3 Vitest frontend tests listed above
- [ ] README.md with local dev setup instructions
- [ ] `docker-compose.yml` for local PostgreSQL

---

## Completion

When ALL items in the Build Checklist above are checked off, the project builds clean, and tests pass, output:

<promise>BUILD COMPLETE</promise>
