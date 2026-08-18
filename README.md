# JobRadar

Personal job-discovery app: aggregates listings from multiple sources once a day
(and on demand), stores them in SQLite, detects what's **NEW** since the last
run, scores every job against your keyword profile (.NET / C# / Blazor / …),
and shows a dashboard where each job's interaction state is obvious at a glance
— new jobs glow, applied/dismissed jobs calm down.

```
JobRadar/
├── backend/JobRadar.Api/   ASP.NET Core 8 Web API + EF Core (SQLite) + Hangfire
└── frontend/               React + Vite + TypeScript + Tailwind + lucide-react
```

## Run it

**Backend** (terminal 1) — http://localhost:5085

```
cd backend/JobRadar.Api
dotnet run
```

Migrations auto-apply on startup; `jobradar.db` and `hangfire.db` are created
next to the project. Swagger UI: http://localhost:5085/swagger · Hangfire
dashboard: http://localhost:5085/hangfire

**Frontend** (terminal 2) — http://localhost:5173

```
cd frontend
npm install
npm run dev
```

Then open http://localhost:5173 and hit **Scan now**. Arbeitnow needs **no API
key**, so real jobs appear immediately on a fresh database.

> Node note: Node 22 (portable) was installed to `%USERPROFILE%\.local\node`
> and added to your user PATH — open a **new** terminal for `npm` to resolve.

## Adzuna API keys (optional but recommended)

Register free at https://developer.adzuna.com, then put the keys in
`backend/JobRadar.Api/appsettings.Local.json` (gitignored, loaded automatically):

```json
{
  "Adzuna": {
    "AppId": "your-app-id",
    "AppKey": "your-app-key"
  }
}
```

Environment variables (`Adzuna__AppId` / `Adzuna__AppKey`) work too. Without
keys the Adzuna fetcher logs a warning and is skipped — nothing else breaks.
If Adzuna doesn't cover Romania for your account, change `Adzuna:Country`
(and optionally `Adzuna:Queries` / `Adzuna:Where`) in `appsettings.json`.

## How it works

**Pipeline** (`ScanService`, same path for POST `/api/scan` and the daily job):

1. Every registered `IJobFetcher` runs; a fetcher that throws is logged and
   skipped — it never takes the others down.
2. Raw jobs are normalized (HTML stripped, whitespace cleaned).
3. Dedup hash = SHA-256 of normalized `company | title | city` (diacritics
   removed, so *Timișoara* ≡ *Timisoara*; only the part before the first comma
   of the location is used so formatting differences still collapse).
4. Language detected heuristically — `ro` / `en` / **`de`** (German added
   because Arbeitnow is German-heavy; without it those jobs would pollute the
   English filter).
5. `IsRemote` derived from source hint + keywords; **hybrid counts as NOT
   remote** and wins over everything.
6. Relevance = weighted keyword matches (title hits ×3), plus small boosts for
   Timișoara / fully-remote jobs. The whole profile lives in
   `appsettings.json → Profile` — tune it freely.
7. Upsert: brand-new hashes are inserted with `FirstSeenAt`, `Notified=false`,
   state `New`. A job counts as **NEW while `Notified == false`**.

**Scheduling** — Hangfire recurring job `daily-job-scan` at 08:00 local time
(`Scan:Cron`). Sources are called with a real User-Agent, 20 s timeouts and
small delays between requests.

**Sources**

| Source | Type | Config |
|---|---|---|
| Arbeitnow | API, live, no auth | `Arbeitnow:MaxPages`, `Arbeitnow:TechFilter` |
| Adzuna | API, live, needs free keys | `Adzuna:*` (queries are a broad dev/software/IT net — scoring, not fetching, ranks .NET first) |
| Greenhouse | API, live, no auth | `Greenhouse:Boards` — list of board tokens, `Greenhouse:TechFilter` |
| eJobs.ro | **Scraping**, live | `Ejobs:Searches`, `Ejobs:MaxPagesPerSearch` |
| Telegram | TODO stub | `Fetching/Stubs/TelegramFetcher.cs` shows the extension point |

- **Greenhouse** iterates the configured board tokens
  (`boards-api.greenhouse.io/v1/boards/{token}/jobs?content=true`); one broken
  board is logged and skipped without affecting the rest. Pre-seeded with four
  live, remote-EU-friendly boards (`gitlab`, `grafanalabs`, `canonical`,
  `remotecom`) — **swap in Timișoara/local companies** as you find ones that
  use Greenhouse (the token is the tail of `boards.greenhouse.io/{token}`).
- **eJobs is scraping-based and will eventually break** when the site changes:
  expect to do occasional selector maintenance. The header comment in
  `Fetching/EjobsFetcher.cs` documents every selector/URL pattern in use
  (primary: `script#__NUXT_DATA__` — the visible cards are hydration
  placeholders, so data is read from the SSR payload). When it breaks it logs
  a loud *"layout may have changed"* warning and returns nothing — it never
  takes the pipeline down. Searches are path-scoped
  (`timisoara/it-software`, `remote/it-software`), which is also how each
  job's location is derived; the results page carries no description snippet,
  so eJobs descriptions are empty by design (no deep-crawling of detail pages).

**Testing one source in isolation** — the scan endpoint takes an optional
`source` filter:

```
curl -X POST "http://localhost:5085/api/scan?source=Greenhouse"
curl -X POST "http://localhost:5085/api/scan?source=eJobs"
```

An unknown name returns 400 listing the available fetchers. Dedup is
source-agnostic (the hash contains no source field), so the same job arriving
from two sources collapses into one row — verified: re-scanning any single
source yields `new: 0` for everything already stored.

Adding a source = implement `IJobFetcher`, register it in `Program.cs`, done.

## API

| Endpoint | What it does |
|---|---|
| `POST /api/scan` | Run the pipeline now; returns per-source fetched/new/errors. Optional `?source=eJobs` runs one fetcher in isolation |
| `GET /api/jobs` | Filters: `state, source, remote, language, place (timisoara/remote/either), q, sort (newest/relevance), skip, take` |
| `GET /api/jobs/{id}` | Full detail (list responses truncate descriptions) |
| `PATCH /api/jobs/{id}/state` | Body `{ "state": "saved" }` — new/seen/saved/applied/dismissed |
| `POST /api/jobs/mark-notified` | Flip `Notified=true` on all current NEW jobs |
| `GET /api/stats` | Counts (new/today/total/saved/applied), per-source, last scan |

## Frontend notes

- **State is the design system**: NEW cards get an accent border + glowing
  pulsing badge; Seen is neutral; Saved gets an amber edge; Applied is calmer
  (green edge, reduced emphasis); Dismissed is faded and desaturated.
- Every card flags **Remote** and/or **🇷🇴 Timișoara / România** so you can see
  instantly whether a job is actually takeable.
- LIST ⇄ BOARD toggle; the board is a kanban of the five states with native
  drag-and-drop (the state dropdown is disabled there — columns *are* states).
- Opening a job automatically moves it New → Seen; the "N new" pill in the top
  bar calls `mark-notified` when clicked.
- Quick actions and drag-drop PATCH optimistically and revert on failure.
- Dark mode is the default; the toggle persists in `localStorage`.
- In dev, Vite proxies `/api` → `localhost:5085` (no CORS needed); CORS for
  `localhost:5173` is enabled anyway, and `VITE_API_URL` overrides the base.

## Decisions & defaults (things you didn't specify, chosen sensibly)

- **The fetch is broad, the ranking is personal**: sources are queried for
  dev/software/IT jobs in general (not just .NET/C#); your keyword profile
  then scores .NET-fit jobs to the top of the "Most relevant" sort.
- **Arbeitnow/Greenhouse tech filters** (`TechFilter`, default on): those feeds
  carry many non-IT roles; only software-looking listings are stored. Set to
  `false` to keep everything.
- **Descriptions are stored as plain text** (HTML stripped at normalization) —
  safe to render, good enough for search/highlighting.
- **Board view ignores the state filter** (columns already are the states).
- **One scan at a time**: concurrent scans return HTTP 409.
- **Reset**: stop the backend and delete `jobradar.db*` / `hangfire.db*`.

## Swapping SQLite → Postgres

1. `dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL`
2. In `Program.cs`, uncomment the `"postgres"` case.
3. Set `Database:Provider` to `Postgres` and point `ConnectionStrings:Jobs` at
   your server.
4. Regenerate migrations (they're provider-specific):
   `dotnet ef migrations remove` … `dotnet ef migrations add InitialCreate`.
