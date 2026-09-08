# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BT Survivor Pool is a full-stack NFL survivor pool application (pick one team per week; lose a pick, you're eliminated). It uses a **microservices .NET 8 backend** with a **Node.js/Express frontend** that proxies all API calls.

## Running the Application

**Start all backend APIs** (each in its own terminal from the solution root):
```bash
cd BTSurvivorPool/BTSurvivorPool && dotnet run        # Users API
cd BTSurvivorPool/SP.API.EntryPicks && dotnet run     # Entry Picks API
cd BTSurvivorPool/SP.API.Leagues && dotnet run        # Leagues API
cd BTSurvivorPool/SP.API.Picks && dotnet run          # Entries API
```

**Start the frontend** (from `nodejs-frontend/`):
```bash
npm install        # first time only
npm run dev        # nodemon with auto-reload
npm start          # production
```

App is available at `http://localhost:3000`. The Express server proxies all `/api/*` calls to `http://localhost:5000`.

## Build & Database

```bash
# Build entire solution
dotnet build BTSurvivorPool.sln

# EF Core migrations (run from SP.Entities project)
cd BTSurvivorPool/SP.Entities
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

SQL Server is required. Set the `DefaultConnection` string in each API's `appsettings.Development.json`.

## Architecture

```
BTSurvivorPool/
├── BTSurvivorPool/         # SP.API.Users — user registration & JWT login
├── SP.API.EntryPicks/      # Entry picks per week (CRUD)
├── SP.API.Leagues/         # League creation & management
├── SP.API.Picks/           # Entry (participant slot) management
├── Common.SP/              # Shared: TokenService, ServiceResult<T>
├── Dto.SP/                 # Shared DTOs across all APIs
├── SP.Entities/            # EF Core DbContext + entity models (shared DB)
└── nodejs-frontend/        # Express server + HTML/CSS/JS UI
```

All four .NET APIs share the same `ApplicationDbContext` (defined in `SP.Entities`) and the same SQL Server database. They share DTOs from `Dto.SP` and utilities from `Common.SP`.

## Key Patterns

**API response wrapper** — every endpoint returns `ServiceResult<T>`:
```json
{ "isSuccess": true, "data": { ... }, "errorMessage": null }
```
Defined in `Common.SP/ServiceResult.cs`.

**CQRS via MediatR** — business logic lives in handler classes, not controllers. Controllers dispatch commands; handlers execute them. Example: `PostUserCommand` → `PostUserCommandHandler`.

**JWT auth** — the secret key lives in `secrets/secrets.json` (gitignored). Each API reads it from `JwtSettings:SecretKey` in configuration. Tokens include claims for `UserId`, `Username`, `Email`, `FirstName`, `LastName`, and `Role`. Protect endpoints with `[Authorize]`.

**CORS** — backends only allow `http://localhost:3000`. The frontend's Express server is the sole browser-facing layer; it proxies to the .NET APIs.

## Secrets Setup

Create `secrets/secrets.json` (gitignored) in each API project that requires JWT:
```json
{
  "JwtSettings": {
    "SecretKey": "<your-secret>"
  },
  "ConnectionStrings": {
    "DefaultConnection": "<your-sql-server-connection-string>"
  }
}
```

## No Tests

There are currently no test projects. All testing is done manually via the UI or the `.http` request files in each API project.
