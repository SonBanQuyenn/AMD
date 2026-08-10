# Poll & Survey Builder — Microservice Solution (AMD201)

An online polling and survey system built on a microservices architecture. An
API Gateway (Ocelot) sits in front of a Poll Service, a Vote Service, and a
Realtime Service (SignalR); each service is independent, owns its own
database, and communicates over REST. See [Architecture](#architecture) below
for the full request flow.

## Table of Contents

- [Current Status](#current-status)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Running with Docker Compose](#running-with-docker-compose)
- [Running Locally (without Docker)](#running-locally-without-docker)
- [Database](#database)
- [API Reference](#api-reference)
- [Testing the Services End-to-End](#testing-the-services-end-to-end)
- [Automated Tests and CI](#automated-tests-and-ci)
- [Known Limitations](#known-limitations)
- [Planned Next Steps](#planned-next-steps)
- [Technical Notes](#technical-notes)

## Current Status

| Service | Status | Description |
|---|---|---|
| Poll Service | Complete | Create, view, and close polls; exposes a `/status` endpoint for other services to call. |
| Vote Service | Complete | Accepts votes, validates the poll against the Poll Service, blocks duplicate votes, and triggers a broadcast to the Realtime Service. |
| Realtime Service | Complete | SignalR hub that clients join by poll code; receives broadcast requests from the Vote Service and pushes live results to every connected client. |
| API Gateway | Complete | Ocelot gateway routing `/api/polls/**` to the Poll Service and `/api/votes/**` to the Vote Service. SignalR proxying through the gateway is available but treated as experimental — see [Technical Notes](#technical-notes). |
| Frontend | Complete | React (Vite) single-page app with three pages: create a poll, vote, and view live results. |
| Unit tests | Partial | Test projects exist for the Poll Service and Vote Service; coverage is not yet complete. |
| CI/CD | Partial | GitHub Actions builds and runs tests on every push/PR to `main`. There is no automated deployment step yet. |
| Cloud deployment | Not started | The system currently runs locally or via Docker Compose only. |

All four backend services follow the standard **controller-based ASP.NET
Core Web API** structure (`Controllers/`, `[ApiController]`, Swagger) rather
than Minimal APIs. The database engine is **SQL Server** — LocalDB for local
development, a SQL Server container for Docker Compose.

## Architecture

```
Frontend (React SPA)
  |-- REST requests -----------------> API Gateway (Ocelot, port 5000)
  |-- SignalR (direct connection) ---> Realtime Service (port 5003)

API Gateway
  |-- /api/polls/**  --> Poll Service      (own database, port 5001)
  |-- /api/votes/**  --> Vote Service      (own database, port 5002)
  |-- /hubs/**       --> Realtime Service  (SignalR hub, port 5003, experimental route)

Vote Service
  |-- GET  /polls/{code}/status  --> Poll Service      (validates the poll before saving a vote)
  |-- POST /broadcast/{code}     --> Realtime Service  (best-effort, after the vote is saved)
```

The Vote Service never validates a poll on its own — it always asks the Poll
Service over REST before writing a vote. This is the most important design
decision to highlight in a presentation: the services have a clear boundary
of responsibility and communicate through real service-to-service calls,
rather than simply splitting code into separate folders.

Each service owns an independent database (`PollSurveyDb` for the Poll
Service, `PollSurveyVoteDb` for the Vote Service), even though both run on
the same SQL Server instance in this setup.

## Technology Stack

- **Backend:** ASP.NET Core Web API, C#, Entity Framework Core (Migrations)
- **Database:** SQL Server (LocalDB for local development, containerized for Docker)
- **Real-time:** SignalR
- **API Gateway:** Ocelot
- **Frontend:** React, Vite
- **DevOps:** Docker Compose, GitHub Actions, Swagger UI

## Project Structure

```
PollSurveyApp/
├── src/
│   ├── PollService.Api/        Poll creation, retrieval, closing (port 5001)
│   ├── VoteService.Api/        Vote submission and validation (port 5002)
│   ├── RealtimeService.Api/    SignalR hub and broadcast endpoint (port 5003)
│   ├── ApiGateway.Api/         Ocelot gateway (port 5000)
│   └── Shared.Contracts/       DTOs shared between services
├── tests/
│   ├── PollService.Api.Tests/
│   └── VoteService.Api.Tests/
├── frontend/                   React + Vite single-page app
├── docker-compose.yml
├── PollSurveyApp.sln
└── README.md
```

## Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- SQL Server LocalDB (installed automatically with Visual Studio) for local development, **or**
- Docker and Docker Compose for a containerized run
- Visual Studio 2022 (recommended) or any editor with a C# extension

## Running with Docker Compose

This is the simplest way to start the full system — backend, database, and
frontend — with a single command:

```bash
docker compose up --build
```

This starts SQL Server, all four backend services, and the frontend (running
the Vite dev server inside its container, with the source code mounted so
local edits still hot-reload).

- Frontend: `http://localhost:5173`
- API Gateway: `http://localhost:5000`
- Poll Service: `http://localhost:5001` (directly reachable for debugging)
- Vote Service: `http://localhost:5002`
- Realtime Service: `http://localhost:5003`
- SQL Server: `localhost:1433`

SQL Server takes a few seconds longer to become ready than the .NET
services. Each backend service is configured with `restart: on-failure`, so
if a container fails on its first attempt because SQL Server was not ready
yet, Docker restarts it automatically — no manual action is needed, just
wait 10–20 seconds after `docker compose up`.

To stop everything: `Ctrl+C`, then `docker compose down` (add `-v` to also
delete the SQL Server data volume).

## Running Locally (without Docker)

### 1. Open the solution

Open `PollSurveyApp.sln` in Visual Studio. NuGet packages restore
automatically; if not, right-click the solution and choose **Restore NuGet
Packages**.

### 2. Create the database migrations (first run only)

```bash
cd src/PollService.Api
dotnet ef migrations add InitialCreate --context PollDbContext --output-dir Migrations
dotnet ef database update --context PollDbContext

cd ../VoteService.Api
dotnet ef migrations add InitialCreate --context VoteDbContext --output-dir Migrations
dotnet ef database update --context VoteDbContext
```

This step does not require Docker or SQL Server to be running separately —
LocalDB starts automatically when needed.

> If you already ran the `InitialCreate` migration for `PollDbContext`
> before the `CreatorToken` column was added, run an additional migration
> and update the database again:
> ```bash
> cd src/PollService.Api
> dotnet ef migrations add AddCreatorToken --context PollDbContext --output-dir Migrations
> dotnet ef database update --context PollDbContext
> ```

### 3. Run the backend services

In Visual Studio, set **Multiple Startup Projects** (or start each project
individually with `dotnet run`):

1. `PollService.Api` — port 5001
2. `VoteService.Api` — port 5002 (requires the Poll Service to be running)
3. `RealtimeService.Api` — port 5003
4. `ApiGateway.Api` — port 5000 (requires the three services above)

Swagger UI opens automatically at `/swagger` for each service.

### 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The `.env` file is already configured with:

- `VITE_GATEWAY_URL=http://localhost:5000/api` — all REST requests go through the gateway.
- `VITE_REALTIME_URL=http://localhost:5003` — SignalR connects directly to the Realtime Service (not through the gateway; see [Technical Notes](#technical-notes)).

All four backend services must be running before the frontend is opened.

**Pages:**

- `/` — create a poll (optionally set an expiry time). On success, the user
  is redirected to that poll's results page.
- `/poll/:code` — the voting page. Voters pick one option to submit their
  vote. Duplicate votes are blocked using a token stored in
  `localStorage` (see `getVoterToken()` in `src/api.js`). Voting is blocked
  once the poll has been closed manually or has passed its expiry time.
- `/poll/:code/results` — the results page, visible only to the poll's
  creator. Shows live results via SignalR, a shareable voting link, and a
  button to close the poll immediately.

**Restricting the results page to the poll's creator:** this project has no
login/account system in scope, so access is controlled with a secret
`CreatorToken`. The Poll Service generates this token when a poll is
created (`POST /polls`) and returns it exactly once, in that response only.
The frontend stores it in `localStorage` (`saveCreatorToken` in
`src/api.js`). Every subsequent request —
`GET /polls/{code}?creatorToken=...` and `POST /polls/{code}/close` —
must include this token; only a matching token grants access to the
results page or the ability to close the poll. Because the token lives in
the browser's `localStorage`, only the browser that created the poll has
this access — anyone with just the voting link cannot see the results page.

**Closing a poll early or letting it expire automatically:**

- `Poll.ExpiresAt` (optional) is the automatic close time, set when the poll is created.
- `PollsController.Get` / `GetStatus` compute `IsExpired` on every call by comparing against `DateTime.UtcNow` — no background job is needed; the poll "closes" itself the moment it expires.
- The creator can also close a poll manually at any time from the results page (calls `POST /polls/{code}/close` with the `CreatorToken`; returns 403 if the token does not match).

## Database

- **Local development (Visual Studio):** uses **LocalDB**, which ships with
  Visual Studio and requires no separate installation. The connection
  string in each service's `appsettings.json` already points to
  `(localdb)\mssqllocaldb`.
- **Docker Compose:** uses a `mcr.microsoft.com/mssql/server:2022-latest`
  container; the connection string is overridden through environment
  variables in `docker-compose.yml`.
- The Poll Service uses the `PollSurveyDb` database and the Vote Service
  uses `PollSurveyVoteDb` — each microservice still owns a separate
  database, even though both run on the same SQL Server instance.
- Databases are created automatically the first time
  `dotnet ef database update` runs (unlike PostgreSQL, SQL Server creates
  the database itself if it does not already exist) — no separate init
  script is required.

### Inspecting data with Server Explorer (Visual Studio)

1. **View → Server Explorer**, right-click **Data Connections**, choose **Add Connection**.
2. Data source: **Microsoft SQL Server**. Server name: `(localdb)\mssqllocaldb`.
3. Select the `PollSurveyDb` database (add a second connection for `PollSurveyVoteDb`).
4. Expand the tree, open **Tables**, and double-click a table to view or edit its data directly.

## API Reference

### Poll Service (port 5001)

```bash
# Create a poll
curl -X POST http://localhost:5001/polls \
  -H "Content-Type: application/json" \
  -d '{"question":"Which language do you prefer?","options":["C#","Python","JavaScript"]}'

# View a poll (replace YOUR_CODE with the code returned above).
# The create-poll response includes "creatorToken" — it is returned exactly once.
curl http://localhost:5001/polls/YOUR_CODE

# View a poll including creator status (isCreator=true if the token matches)
curl "http://localhost:5001/polls/YOUR_CODE?creatorToken=YOUR_CREATOR_TOKEN"

# Status endpoint used by other services (the Vote Service calls this)
curl http://localhost:5001/polls/YOUR_CODE/status

# Close a poll early — requires the correct creatorToken, returns 403 otherwise
curl -X POST http://localhost:5001/polls/YOUR_CODE/close \
  -H "Content-Type: application/json" \
  -d '{"creatorToken":"YOUR_CREATOR_TOKEN"}'
```

### Vote Service (port 5002)

The Vote Service requires the Poll Service to be running, since it calls
`/status` on the Poll Service before accepting a vote.

```bash
# Submit a vote
curl -X POST http://localhost:5002/votes \
  -H "Content-Type: application/json" \
  -d '{"pollCode":"YOUR_CODE","optionIndex":0,"voterToken":"test-voter-1"}'

# Submit a second vote with the same voterToken -> returns 409 Conflict
curl -X POST http://localhost:5002/votes \
  -H "Content-Type: application/json" \
  -d '{"pollCode":"YOUR_CODE","optionIndex":1,"voterToken":"test-voter-1"}'

# View results
curl http://localhost:5002/votes/YOUR_CODE/results
```

## Testing the Services End-to-End

Start all four backend services (multiple startup projects in Visual
Studio, or simply `docker compose up --build`), then:

```bash
# 1. Create a poll through the Poll Service
curl -X POST http://localhost:5001/polls \
  -H "Content-Type: application/json" \
  -d '{"question":"Which language do you prefer?","options":["C#","Python","JavaScript"]}'
# Take the "code" from the response as YOUR_CODE

# 2. Vote through the Vote Service — it will call the Poll Service to validate,
#    then call the Realtime Service to broadcast the result
curl -X POST http://localhost:5002/votes \
  -H "Content-Type: application/json" \
  -d '{"pollCode":"YOUR_CODE","optionIndex":0,"voterToken":"test-voter-1"}'

# 3. Check the Realtime Service console — a "POST /broadcast/YOUR_CODE" request
#    appearing there confirms the three services communicated successfully.
```

To see this visually rather than in logs alone, connect a client (Postman,
or any JavaScript console) to `http://localhost:5003/hubs/results` using
the `@microsoft/signalr` library, call
`connection.invoke("JoinPoll", "YOUR_CODE")`, then repeat the vote step
above — a `ResultsUpdated` event should arrive immediately.

### Testing through the API Gateway

With all four services running, call the gateway instead of a service
directly:

```bash
curl -X POST http://localhost:5000/api/polls \
  -H "Content-Type: application/json" \
  -d '{"question":"Test through the gateway?","options":["Yes","No"]}'
```

The result should be identical to calling `localhost:5001/polls` directly.
SignalR through the gateway (`ws://localhost:5000/hubs/results`) uses a
feature Ocelot itself marks as experimental — if this causes issues, the
frontend can connect directly to `ws://localhost:5003/hubs/results`
instead, which is still architecturally valid and simply bypasses the
gateway for this one protocol.

## Automated Tests and CI

Unit test projects exist for the Poll Service and Vote Service
(`tests/PollService.Api.Tests`, `tests/VoteService.Api.Tests`), covering
core validation logic such as poll creation, option-count limits, and
duplicate-vote handling.

A GitHub Actions workflow (`.github/workflows/ci.yml`) runs automatically
on every push and pull request to `main`:

1. Checkout code
2. Set up .NET 8
3. Restore dependencies
4. Build (Release configuration)
5. Run unit tests
6. Publish test results as a workflow artifact (`.trx`)

The pipeline currently builds and tests the solution only — it does not yet
build or push a Docker image, and there is no automated deployment step.

## Known Limitations

- No cloud deployment yet — the system currently runs locally or via Docker Compose only.
- No JWT-based authentication or user accounts. Access control relies on lightweight, single-use tokens (`CreatorToken` for poll creators, `VoterToken` for voters) rather than a full identity system.
- Unit test coverage is partial; not every validation path is covered yet.
- CI builds and tests the solution but does not deploy it.
- SignalR routing through the API Gateway relies on an Ocelot feature that is still considered experimental; the frontend currently connects to the Realtime Service directly as a result.

## Planned Next Steps

1. Expand unit test coverage (empty questions, option counts outside the 2–6 range, duplicate votes, expired polls, status calls for a poll that does not exist).
2. Extend the CI pipeline to build and push Docker images for all four services to a container registry.
3. Add an automated deployment step (e.g. to Render or Railway), with each service deployed independently and environment variables pointing to the others' public URLs — similar to how `docker-compose.yml` currently points services at each other by container name, but using public domains and an updated `ocelot.Docker.json`.
4. Introduce JWT-based authentication in place of the current lightweight token system.
5. Add monitoring/alerting and consider a message queue (RabbitMQ or Kafka) for service-to-service events as the system grows.

## Technical Notes

- The database uses **EF Core Migrations** (`Database.Migrate()` runs automatically at application startup) rather than `EnsureCreated()`, reflecting a standard, production-style workflow.
- The `Poll` entity's `Options` property is a `string[]` in C#, but SQL Server has no native array type (unlike PostgreSQL). An EF Core value converter in `PollDbContext.cs` transparently serializes it to JSON on save and deserializes it on read, with no impact on the rest of the code.
- Local development uses LocalDB, while Docker Compose uses a SQL Server container — only the connection string changes between the two; no code changes are needed when switching.