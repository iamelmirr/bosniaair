# SarajevoAirVibe – Rebuild Handbook for Beginners

> **Goal:** Recreate the complete SarajevoAirVibe stack (ASP.NET Core backend + Next.js frontend + SQLite data store) from scratch and understand how every moving part works. This guide walks you through the exact order of steps, explains the architecture, and tells you what every file and function is responsible for.

---

## 1. Prerequisites & Tooling

| Tool | Version | Why you need it | Install tips |
|------|---------|-----------------|--------------|
| [.NET SDK](https://dotnet.microsoft.com/en-us/download) | 8.0.x | Builds & runs the backend API | macOS: `brew install --cask dotnet-sdk` |
| [Node.js](https://nodejs.org/) | ≥ 18.x | Runs the Next.js frontend | macOS: `brew install node` |
| [pnpm](https://pnpm.io/) | ≥ 8.x | Frontend package manager (faster, lockfile-friendly) | `npm install -g pnpm` |
| [SQLite CLI](https://www.sqlite.org/download.html) | 3.x | Inspect the local database | macOS: `brew install sqlite` |
| curl / HTTP client | latest | Quick API smoke checks | Already on macOS |
| Any code editor | latest | Editing source files | VS Code recommended |

> **Environment Variables:** Obtain a WAQI API token from https://aqicn.org/api/ (free signup). You can temporarily use the sample token checked into `appsettings.json`, but create your own for production use.

---

## 2. Understand the Architecture Before Building

```
┌──────────────────────────────┐
│          Frontend            │
│  Next.js 14 + React + SWR    │
│  TailwindCSS for styling     │
│  Fetches cached data from    │
│  /api/v1/air-quality/...     │
└──────────────▲──────────────┘
               │ JSON REST
┌──────────────┴──────────────┐
│           Backend           │
│  ASP.NET Core 8 Web API     │
│  Layers: Controller →       │
│  Service → Repository → DB  │
│  Background scheduler hits  │
│  WAQI every 10 minutes      │
└──────────────▲──────────────┘
               │ EF Core
┌──────────────┴──────────────┐
│          Database           │
│  SQLite file (local dev)    │
│  Table: AirQualityRecords   │
│  Stores live snapshots +    │
│  single forecast row/city   │
└──────────────────────────────┘
```

### Key runtime flow
1. **Scheduler (`AirQualityScheduler`)** wakes up every 10 minutes, looping through the configured cities.
2. For each city, **`AirQualityService.RefreshCityAsync`** calls the WAQI API, normalizes the payload, stores a live snapshot row, and upserts the forecast JSON blob.
3. **`AirQualityController`** exposes `/live`, `/forecast`, and `/complete` endpoints that read cached data; they never call WAQI directly.
4. The **Next.js frontend** polls these endpoints (SWR + manual observable) to show live AQI cards, timeline, per-group advice, and comparison views.

Keep this picture in mind while building—you’re wiring up a background data pipeline plus a read-only API.

---

## 3. Recreate the Backend (ASP.NET Core)

### 3.1. Project skeleton

```bash
mkdir -p sarajevoairvibe/backend
cd sarajevoairvibe/backend

dotnet new sln -n SarajevoAir
mkdir -p src/SarajevoAir.Api
cd src/SarajevoAir.Api

dotnet new webapi -n SarajevoAir.Api --no-https
cd ../..

dotnet sln SarajevoAir.sln add src/SarajevoAir.Api/SarajevoAir.Api.csproj
```

Remove the auto-generated WeatherForecast files (`WeatherForecast.cs`, `Controllers/WeatherForecastController.cs`). We are building a clean REST API.

### 3.2. NuGet packages

Inside `src/SarajevoAir.Api` install the packages used in the existing project:

```bash
cd src/SarajevoAir.Api

# Logging & tooling
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.1

# Database
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.9
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0

# HTTP resilience
dotnet add package Microsoft.Extensions.Http.Resilience --version 9.0.0

# Swagger UI
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

The resulting `<ItemGroup>` inside `SarajevoAir.Api.csproj` should match the repository (packages + EF tooling).

### 3.3. Directory layout

Create the folders:
```
Configuration/
Controllers/
Data/
Dtos/
Entities/
Enums/
Middleware/
Migrations/
Repositories/
Services/
Utilities/
Aqi/ (already referenced for static assets)
```

### 3.4. Configuration files

1. **`appsettings.json`** – copy the structure below and insert your own WAQI token.
   ```json
   {
     "Logging": { ... },
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=sarajevoair-aqi.db"
     },
     "Serilog": { ... console sink config ... },
     "Aqicn": {
       "ApiUrl": "https://api.waqi.info",
       "ApiToken": "<your-token>"
     },
     "Worker": {
       "FetchIntervalMinutes": 10
     }
   }
   ```
2. **`appsettings.Development.json`** – bump log levels to Debug (copy from repo).

### 3.5. Configuration helpers

- `Configuration/AqicnConfiguration.cs`: simple POCO with `ApiUrl` and `ApiToken` strings used by `IOptions<T>`.
- `Configuration/UtcDateTimeConverter.cs`: custom System.Text.Json converter ensuring all outgoing `DateTime` values end with `Z` (UTC) so the frontend interprets timestamps correctly.

### 3.6. Utility layer

- `Utilities/TimeZoneHelper.cs`: wraps `TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time")`. Exposes:
  - `GetSarajevoTime()` → DateTime in local Sarajevo zone (CET/CEST).
  - `ConvertToSarajevoTime(DateTime utc)` → convert WAQI timestamps.

### 3.7. Domain model + enums

- `Enums/City.cs`: enum values representing WAQI station numeric IDs (Sarajevo = 10557, Tuzla = 8739, etc.).
- `Enums/CityExtensions.cs`: helper to map city enum to WAQI station strings (`"@10557"` etc.). Default fallback prefixes enum value with `@`.
- `Enums/AirQualityRecordType.cs`: distinguishes `LiveSnapshot` vs `Forecast` rows.
- `Entities/AirQualityRecord.cs`: EF entity with live data columns, serialized `ForecastJson`, audit columns (`CreatedAt`, `UpdatedAt`).

### 3.8. EF Core DbContext

`Data/AppDbContext.cs`:
- Registers `DbSet<AirQualityRecord>`.
- In `OnModelCreating` configure:
  - String conversions for `City` and `AirQualityRecordType` enums.
  - Composite index `(City, RecordType, Timestamp)` for fast “latest snapshot” queries.
  - Unique index `(City, RecordType)` filtered to `Forecast` so each city has at most one forecast row.

### 3.9. Repository layer

`Repositories/AirQualityRepository.cs` implements `IAirQualityRepository`:
- `AddLiveSnapshotAsync`: sets `RecordType = LiveSnapshot`, stamps `CreatedAt`, saves row.
- `GetLatestSnapshotAsync`: returns most recent snapshot for a city.
- `UpsertForecastAsync`: create/update forecast row with JSON payload and timestamp.
- `GetForecastAsync`: read cached forecast.

All DB access is routed through this repository; services don’t touch `AppDbContext` directly.

### 3.10. DTO contracts (`Dtos/`)

Use record types so JSON serialization is consistent:
- `MeasurementDto`, `LiveAqiResponse` → live data payload.
- `PollutantRangeDto`, `ForecastDayDto`, `ForecastResponse` → forecast payload.
- `CompleteAqiResponse` → combined live + forecast.
- `WaqiApiResponse` etc. → raw WAQI API mapping for deserialization.

### 3.11. Service layer

`Services/AirQualityService.cs` is the heart of the backend. Important methods:
- **Constructor**: accepts `HttpClient`, repository, `IOptions<AqicnConfiguration>`, logger. Throws if token missing.
- **`GetLiveAsync(City city)`**: fetch latest snapshot from repository; throws `DataUnavailableException` if cache empty.
- **`GetForecastAsync`**: similar for forecast; deserializes cached JSON into `ForecastResponse`.
- **`GetCompleteAsync`**: wraps `GetLiveAsync` + `GetForecastAsync`. If forecast missing, returns empty array rather than failing the whole response.
- **`RefreshCityAsync`**: entry point for scheduler. Calls `RefreshInternalAsync`.
- **`RefreshInternalAsync`** flow:
  1. Log start.
  2. `FetchWaqiDataAsync` → call `feed/{station}?token=...`.
  3. Map WAQI data into `AirQualityRecord` and save snapshot.
  4. `BuildForecastDays` to transform WAQI daily arrays into friendly DTOs (keeps all available days ≥ today).
  5. Serialize forecast to JSON and `UpsertForecastAsync`.
- **`BuildForecastDays`**: merges `pm25`, `pm10`, `o3` arrays by date, calculates representative AQI, attaches pollutant ranges.
- **Helpers**: `ParseTimestamp`, `MapDominantPollutant`, `GetAqiInfo`, `BuildMeasurements`.

`Services/DataUnavailableException.cs`: custom exception for cache misses.

### 3.12. Background scheduler

`Services/AirQualityScheduler.cs` derives from `BackgroundService`:
- Reads `Worker:FetchIntervalMinutes` and optional city overrides from configuration.
- Logs startup, immediately runs a refresh cycle, then sleeps for configured interval.
- `RefreshCityAsync` is run in parallel for each city using DI scope to resolve `IAirQualityService`.

### 3.13. Middleware

`Middleware/ExceptionHandlingMiddleware.cs` wraps the pipeline:
- Catches unhandled exceptions, maps them to sensible HTTP status codes (400/404/408/500 etc.).
- Responds with RFC 7807-style JSON body.
- Logs the original exception via `ILogger`.

### 3.14. Controller layer

`Controllers/AirQualityController.cs` exposes three endpoints at `api/v1/air-quality/{city}`:
- `GET .../live` → returns `LiveAqiResponse` from cache.
- `GET .../forecast` → returns `ForecastResponse` (503 if forecast missing, e.g., Tuzla).
- `GET .../complete` → returns combined data (`LiveData`, `ForecastData`, `RetrievedAt`).

Each action handles `DataUnavailableException` (returns 503) and logs warnings/errors.

### 3.15. Program.cs wiring

The top-level `Program.cs` sets up everything:
1. Configure Serilog logging (reader-friendly console output).
2. Add CORS policy `FrontendOnly` for `localhost` ports and env-provided origin.
3. Register controllers + JSON options (`UtcDateTimeConverter`, enum as strings).
4. Enable Swagger in Development.
5. Register `AppDbContext` with SQLite connection string fallback chain.
6. Bind `AqicnConfiguration` to `Aqicn` section.
7. Register `HttpClient` with resilience handler + base address.
8. Register repository (`AddScoped<IAirQualityRepository, AirQualityRepository>`), service, and hosted scheduler.
9. Add health checks.
10. Build pipeline: Serilog request logging → custom exception middleware → CORS → Swagger (dev) → static files → routing → controllers → health checks.
11. On startup, create a DI scope and call `context.Database.EnsureCreatedAsync()`.
12. Run the app.

### 3.16. Database migration

The repo already has an initial migration (`Migrations/20250927180447_InitialAirQualityRecord.cs`). To regenerate it:

```bash
cd backend/src/SarajevoAir.Api

dotnet tool install --global dotnet-ef

dotnet ef migrations add InitialAirQualityRecord
```

SQLite database file (`sarajevoair-aqi.db`) sits next to the project. Delete it to reset the cache; EF will recreate it at runtime.

### 3.17. Running the backend

```bash
cd backend/src/SarajevoAir.Api

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://localhost:5000"
# Optionally override token
# export Aqicn__ApiToken="<your-token>"

dotnet run
```

Health check: `curl http://localhost:5000/health`

Live data: `curl http://localhost:5000/api/v1/air-quality/Sarajevo/live`

Forecast data: `curl http://localhost:5000/api/v1/air-quality/Sarajevo/forecast`

SQLite inspection:
```bash
sqlite3 sarajevoair-aqi.db "SELECT City, RecordType, Timestamp FROM AirQualityRecords ORDER BY Timestamp DESC LIMIT 10;"
```
Expect two record types per city: multiple `LiveSnapshot` rows (historical) and one `Forecast` row with the latest serialized days.

---

## 4. Recreate the Frontend (Next.js 14 + TypeScript)

### 4.1. Bootstrap project

```bash
cd ../../frontend
pnpm create next-app@latest sarajevoair-frontend \
  --typescript --tailwind --eslint --src-dir --app --import-alias "@/*"

# Move generated content into existing frontend folder if necessary
```

The repository already keeps everything inside `frontend/` (app router, TypeScript enabled).

### 4.2. Dependencies

Install runtime deps listed in `package.json`:
```bash
pnpm add swr framer-motion react-chartjs-2 chart.js react-leaflet leaflet @heroicons/react clsx
```

development deps (if not already present):
```bash
pnpm add -D @types/node @types/react @types/react-dom @types/leaflet autoprefixer eslint eslint-config-next postcss tailwindcss typescript
```

### 4.3. Project config files

- `next.config.ts`: enable typed routes, expose `NEXT_PUBLIC_API_BASE_URL`, add security headers.
- `tailwind.config.ts`: extended color palette for AQI states + dark mode.
- `app/globals.css`: Tailwind base + custom CSS variables/animations for card interactions (light/dark theme CSS vars + AQI color classes).
- `next-env.d.ts`, `tsconfig.json`: use defaults generated by Next.js.

### 4.4. Global layout (`app/layout.tsx`)

- Imports `globals.css`.
- Configures Inter font.
- Exposes SEO metadata (OpenGraph, Twitter card, manifest, etc.).
- Wraps children with theming container and `modal-root` for portals.

### 4.5. Home page (`app/page.tsx`)

This single page orchestrates the UI:
- `useLiveAqi(primaryCity)` fetches cached live data.
- `usePeriodicRefresh(60_000)` triggers SWR cache invalidation via the custom observable.
- Local state manages mobile detection, city modal, and preferences.
- Renders: `Header`, `LiveAqiCard`, pollutant grid (`PollutantCard`), `DailyTimeline`, `GroupCard`, `CityComparison`, and `CitySelectorModal`.

### 4.6. Shared lib modules (`frontend/lib`)

- `api-client.ts`
  - Encapsulates base URL resolution (env var fallback to `http://localhost:5000`).
  - `request<T>` handles JSON fetch + recursive ISO date string → `Date` conversion.
  - `getLive` & `getComplete` call backend endpoints.
  - Exports TypeScript interfaces mirroring backend DTOs.
- `hooks.ts`
  - SWR-powered hooks `useLiveAqi`, `useComplete`, `useRefreshAll`, `usePeriodicRefresh`.
  - Subscribes to `airQualityObservable` for manual refresh events.
- `observable.ts`
  - Lightweight event bus (EventTarget-based) to fan out refresh notifications and manage polling interval.
- `utils.ts`
  - City metadata (`CITY_OPTIONS`, `CityId`), label helpers, CSS `classNames`, and `getAqiCategoryClass`.
- `health-advice.ts`
  - Static health advice logic by AQI thresholds for groups (Astmatičari, Sportisti, Djeca, Stariji).
  - Returns risk levels, icons, UI helper maps.
- `theme.ts`
  - Custom React hook `useTheme()` storing user preference in `localStorage`, syncing with `prefers-color-scheme`, exposing `toggleTheme` and `setTheme`.

### 4.7. Components (`frontend/components`)

- `Header.tsx`: sticky header with theme toggle, city selector, responsive menu.
- `LiveAqiCard.tsx`: shows current AQI, health tip, timestamp, share button; handles loading/error states.
- `PollutantCard.tsx`: per-parameter value card with EPA breakpoints, color-coded backgrounds.
- `DailyTimeline.tsx`: uses `useComplete` forecast data to render timeline (mobile slider + desktop grid).
- `GroupCard.tsx`: uses `getAllHealthAdvice` to show per-group guidance.
- `CityComparison.tsx`: compare primary city vs a selected secondary city, caching results to avoid flicker.
- `CitySelectorModal.tsx`: modal to choose primary city, stored in localStorage.

All components are client-side (`'use client'`) because they rely on browser APIs (localStorage, window sizing).

### 4.8. Running the frontend

```bash
cd frontend
pnpm install

export NEXT_PUBLIC_API_URL="http://localhost:5000"
pnpm dev
```

The dev server starts on `http://localhost:3000`. Open the browser, pick a city in the modal, and you should see the live dashboard with cards and charts.

---

## 5. How the Scheduler & Database Work Together

1. **Configure interval**: `Worker:FetchIntervalMinutes` in `appsettings.json`. Minimum enforced in code is 1 minute.
2. **Initial run**: Scheduler kicks off immediately when the API starts, so the database is hydrated without waiting for the first interval.
3. **Live snapshots**: Every scheduler cycle inserts a `LiveSnapshot` row per city, preserving history.
4. **Forecast row**: Each cycle upserts the serialized forecast JSON for that city (one row per city).
5. **Verifying updates**:
   ```bash
   sqlite3 backend/src/SarajevoAir.Api/sarajevoair-aqi.db "SELECT City, RecordType, datetime(Timestamp,'localtime'), UpdatedAt FROM AirQualityRecords ORDER BY UpdatedAt DESC LIMIT 12;"
   ```
   Expect 6 `Forecast` rows (one per city except Tuzla which might be empty) and continuously growing `LiveSnapshot` rows.
6. **Handling cities without forecast**: Tuzla returns an empty forecast; the service catches the exception, and the frontend simply renders no forecast days.

---

## 6. End-to-End Manual Test Plan

1. **Start backend**:
   ```bash
   cd backend/src/SarajevoAir.Api
   dotnet run --urls "http://localhost:5000"
   ```
2. **Wait for scheduler** (watch logs or check DB timestamps).
3. **Smoke test endpoints**:
   ```bash
   curl http://localhost:5000/api/v1/air-quality/Sarajevo/live | jq
   curl http://localhost:5000/api/v1/air-quality/Sarajevo/forecast | jq
   curl http://localhost:5000/api/v1/air-quality/Sarajevo/complete | jq
   ```
4. **Inspect database**:
   ```bash
   sqlite3 sarajevoair-aqi.db "SELECT City, RecordType, Timestamp FROM AirQualityRecords ORDER BY Timestamp DESC LIMIT 5;"
   ```
5. **Start frontend** (`pnpm dev`) and load `http://localhost:3000`.
6. **Pick a city** (Sarajevo by default), confirm cards populate and refresh.
7. **Compare cities**: Select Tuzla or Mostar in the comparison widget.
8. **Toggle dark mode**: Click the theme toggle in the header—Tailwind dark styles should apply instantly.

Optional: run Postman collection or integrate with `test-system.sh` script for a full system check.

---

## 7. Additional Notes & Gotchas

- **Tokens & secrets**: Never ship real WAQI tokens. Move your token to environment variables (`Aqicn__ApiToken`).
- **Dockerfile**: The checked-in Dockerfile references legacy projects (`SarajevoAir.Domain`, etc.). If you plan to containerize, update it to copy only `SarajevoAir.Api` files.
- **Migrations**: SQLite handles schema automatically via `EnsureCreatedAsync`. For production-grade DBs (PostgreSQL), switch to `context.Database.MigrateAsync()` and manage migrations explicitly.
- **Logging**: Serilog writes to console; adjust `appsettings` if you want JSON logs or different sinks.
- **Rate limits**: WAQI API has quotas; keep the 10-minute interval or implement caching/conditional requests.
- **Timezone**: Everything is converted to Sarajevo local time before persisting. Frontend receives ISO timestamps with `Z` suffix and formats them via `Intl.DateTimeFormat`.
- **Tuzla forecast**: Upstream API often omits forecast data—frontend handles empty arrays gracefully.

---

## 8. Checklist – you know it’s rebuilt correctly when…

- ✅ Running `dotnet run` shows "Air quality scheduler starting with 6 cities".
- ✅ SQLite table `AirQualityRecords` fills with both snapshot and forecast rows within a couple of minutes.
- ✅ `curl http://localhost:5000/api/v1/air-quality/Sarajevo/complete` returns both `liveData` and `forecastData` (6 days).
- ✅ Frontend dashboard displays AQI cards, timeline, health advice, and city comparison.
- ✅ Theme toggle works, and city preferences persist across reloads.

Once you can reproduce all of the above starting from an empty folder, you not only rebuilt the project—you also understand every component in the stack.

Good luck, and have fun experimenting with the data! 🚀
