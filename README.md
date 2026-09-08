# BT Survivor Pool

NFL Survivor Pool — pick one team per week per life. Win = life continues. Lose = life gone. Last player with a life wins.

## Stack

- **Backend**: .NET 8 ASP.NET Core API
- **Database**: PostgreSQL + EF Core
- **Jobs**: Hangfire (score processing every Tuesday 9 AM ET)
- **Frontend**: React 18 + Vite + TypeScript + Tailwind CSS
- **Auth**: JWT (8-hour) + 4–6 digit PIN

## Local Development

### 1. Start PostgreSQL

```bash
docker-compose up -d
```

### 2. Backend

```bash
cd backend
# Copy and configure secrets
cp appsettings.json appsettings.Development.json
# Edit appsettings.Development.json with your JWT secret

dotnet run
# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
# Hangfire dashboard at http://localhost:5000/hangfire
```

Database migrations run automatically on startup in development.

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
# App available at http://localhost:5173
```

Set `VITE_API_URL=http://localhost:5000` in a `.env.local` file if needed.

## First-Time Setup

1. Register the first user — they automatically get the `SuperAdmin` role
2. Log in and go to `/admin`
3. Create a season (e.g. Year: 2025, Start: 2025-09-04)
4. Activate the season
5. Click "Seed Schedule" — fetches all games from ESPN API (~18 weeks)
6. Create a league (as LeagueAdmin role user)
7. Share the invite code with players

## Running Tests

```bash
# Backend (13 tests)
cd backend.Tests
dotnet test

# Frontend (12 tests)
cd frontend
npm test
```

## Game Rules

- **Lives**: Each player gets N lives (set by league admin). Each life picks one team per week.
- **Same team, different lives**: Allowed in the same week across different lives.
- **Used teams**: A team Won on Life 1 cannot be picked again on Life 1 (but can on Life 2).
- **Tie = Win**: Tied games keep the life active and consume the team.
- **Pick locks**: Thursday/early games lock 1 min before kickoff. All others lock Sunday 12:59 PM ET.
- **Tiebreaker**: If all remaining players lose the same week, they replay the following week.

## Hosting (Recommended)

| Service | Cost | Notes |
|---|---|---|
| Railway (API + DB) | ~$5–10/month | Easiest .NET deployment |
| Vercel (Frontend) | Free | Static React deploy |
| **Total** | **~$5–10/month** | |

Environment variables needed in production:
- `ConnectionStrings__DefaultConnection` — PostgreSQL connection string
- `JwtSettings__SecretKey` — 32+ character random secret
- `Cors__AllowedOrigins__0` — your frontend URL
