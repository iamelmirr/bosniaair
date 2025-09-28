docker-compose up --build
# BosniaAir – Simplified Air Quality Monitoring

[![FSnapshots of Sarajevo's AQI are stored in `bosniaair-aqi.db` (auto-created). The hosted worker keeps that table up to date every 10 minutes.ontend](https://img.shields.io/badge/Frontend-Next.js%2014-blue)](https://nextjs.org/)
[![Backend](https://img.shields.io/badge/Backend-.NET%208-purple)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/Database-SQLite-green)](https://www.sqlite.org/)

ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5000" dotnet run --project /Users/elmirbesirovic/Desktop/projects/sarajevoairvibe/backend/src/BosniaAir.Api/BosniaAir.Api.csproj

sqlite3 bosniaair-aqi.db "SELECT * FROM SimpleAqiRecords ORDER BY Timestamp DESC LIMIT 10;"

sqlite3 bosniaair-aqi.db "SELECT * FROM SarajevoMeasurements ORDER BY Timestamp DESC LIMIT 5;"

sqlite3 bosniaair-aqi.db "SELECT * FROM SarajevoForecasts ORDER BY Date DESC LIMIT 10;"


Lightweight full-stack project that tracks Sarajevo’s air quality, built to demonstrate a clear repository → service → controller flow in ASP.NET Core with a matching Next.js UI.

## ✨ What’s Included

- Live Sarajevo AQI, refreshed every 10 minutes
- 5‑day AQI outlook built from live data snapshots
- Health advice for critical groups (Sportisti, Djeca, Stariji, Astmatičari)
- City comparison screen that always fetches fresh AQI (no caching)
- SQLite persistence for Sarajevo’s live AQI history
- Minimal service layer that mirrors the classic `Repository → Service → Controller` stack Matej described

## 🧱 Architecture at a Glance

```
Next.js frontend
          │
ASP.NET Core API (BosniaAir.Api)
          ├── Controllers  → HTTP endpoints
          ├── Services     → Business logic + orchestrations
          ├── Repository   → EF Core over SQLite
          ├── DTOs         → Responses to the frontend
          └── Hosted Worker→ 10 min refresh of Sarajevo live AQI
```

All former `Domain`, `Application`, `Infrastructure`, and external worker projects were removed. Everything now lives inside `BosniaAir.Api`, keeping the focus on the repository/service/controller pattern with a single data model (`SimpleAqiRecord`).

## 🗂️ Repository Layout

```
backend/
├── BosniaAir.sln            # Solution with Api + Tests
├── src/
│   └── BosniaAir.Api/       # Controllers, services, repository, DTOs, hosted refresh
└── tests/
     └── BosniaAir.Tests/     # Unit tests for services

frontend/
└── …                          # Next.js app (unchanged)
```

Backups of the original clean-architecture projects are kept under `backend_BACKUP_*` if you ever need to reference the older layout.

## ⚙️ Backend Quickstart

1. **Set your API key**
    ```bash
    export AQICN_API_KEY=your_key_here
    ```

2. **Run the API**
    ```bash
    cd backend/src/BosniaAir.Api
    dotnet run
    ```

3. **Available endpoints** (Swagger at `http://localhost:5000/swagger`)
    - `GET /live` – Sarajevo live AQI (force refresh via `?refresh=true`)
    - `GET /forecast` – 5-day outlook based on stored snapshots
    - `GET /groups` – Health guidance for critical groups
    - `GET /daily` – 7-day timeline built from SQLite history
    - `GET /compare?cities=Sarajevo,Tuzla` – Always hits the upstream API, no caching
    - `GET /admin/snapshots` – Inspect/remove stored snapshots (for debugging)

Snapshots of Sarajevo’s AQI are stored in `sarajevoair-aqi.db` (auto-created). The hosted worker keeps that table up to date every 10 minutes.

## 🧪 Tests

Unit tests exercise the new services directly (repository fallbacks, group advice logic, city comparison error handling).

```bash
cd backend
dotnet test
```

## 🖥️ Frontend Quickstart

```bash
cd frontend
pnpm install
pnpm dev
```

Set `NEXT_PUBLIC_API_BASE_URL` to your backend URL (defaults to `http://localhost:5000`).

## ✅ What Changed (and Why)

- **One project**: everything runs from `BosniaAir.Api`; the extra projects were deleted to keep focus on the core flow.
- **SQLite persistence**: a single `SimpleAqiRecord` entity powers history, forecast, and admin views.
- **Clear layering**: controllers → services → repository; DTOs are explicitly separated from EF entities.
- **City comparison**: now always calls the upstream API (`forceFresh: true`), matching the “no cache” requirement.
- **Background refresh**: implemented as an `IHostedService` inside the API; no separate worker project needed.

This matches the guidance Matej gave: repository for data, service for logic, controller for HTTP, and DTOs just for what the client sees.

## 📄 License

MIT – see [LICENSE](LICENSE).