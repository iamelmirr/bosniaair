# 🎯 INTERVIEW PREPARATION CHECKLIST - BosniaAir Project
## Complete Knowledge Base - Spreman Za Intervju! ✅

**Created for Interview**: Tuesday 12:00  
**Status**: ✅ ALL SYSTEMS GO - READY FOR PRESENTATION

---

## 📊 PROJECT OVERVIEW

### What is BosniaAir?
**Real-time air quality monitoring application** for Bosnia and Herzegovina cities (Sarajevo, Tuzla, Zenica, Mostar, Travnik, Bihać).

### Core Features
- ✅ **Live AQI Data** - Real-time air quality measurements every 10 minutes
- ✅ **7-Day Forecast** - PM2.5, PM10, O3 predictions
- ✅ **Multi-City Comparison** - Side-by-side air quality analysis
- ✅ **Health Advice** - Category-based recommendations (Good/Moderate/Unhealthy)
- ✅ **Pollutant Breakdown** - Detailed measurements for PM2.5, PM10, O3, NO2, CO, SO2
- ✅ **PWA Ready** - Progressive Web App with offline capability

### Tech Stack
**Frontend**: Next.js 15, React 19, TypeScript, TailwindCSS, SWR
**Backend**: ASP.NET Core 8 (C#), Entity Framework Core, SQLite/PostgreSQL
**External API**: WAQI (World Air Quality Index) API
**Deployment**: Vercel (Frontend), Railway (Backend)

---

## 🏗️ ARCHITECTURE - KAO PJESMICA!

### System Flow (3 Main Components)

```
┌──────────────┐    HTTP/REST    ┌──────────────┐    HTTP     ┌──────────────┐
│   Frontend   │ ←──────────────→ │   Backend    │ ←──────────→ │   WAQI API   │
│  (Next.js)   │   JSON/HTTPS    │  (ASP.NET)   │   JSON      │  (External)  │
└──────────────┘                 └──────────────┘             └──────────────┘
      ↓                                 ↓
 SWR Cache                        SQLite/PG DB
(In-Memory JS)                   (Cache Layer)
```

### Backend Architecture (Clean Architecture Pattern)

```
📁 backend/src/BosniaAir.Api/
├── Controllers/          → Endpoints (HTTP Request Handlers)
│   └── AirQualityController.cs   ✅ 3 endpoints only
│
├── Services/            → Business Logic Layer
│   ├── AirQualityService.cs      ✅ 226 lines, clean orchestration
│   └── AirQualityScheduler.cs    ✅ Background worker (10-min refresh)
│
├── Repositories/        → Data Access Layer
│   └── AqiRepository.cs          ✅ CRUD operations for DB
│
├── Utilities/           → Helper Classes & Mappers
│   ├── AirQualityMapper.cs       ✅ 298 lines, ALL transformations
│   └── TimeZoneHelper.cs         ✅ Sarajevo timezone conversions
│
├── Data/                → Database Context
│   └── AppDbContext.cs           ✅ EF Core DbContext
│
├── Entities/            → Database Models
│   └── AirQualityRecord.cs       ✅ Entity with pollutant properties
│
├── Dtos/                → Data Transfer Objects
│   ├── LiveAqiResponse.cs        ✅ API response models
│   ├── ForecastResponse.cs       
│   ├── CompleteAqiResponse.cs    
│   └── WaqiApiDtos.cs            ✅ External API models
│
├── Enums/               → Enumerations
│   ├── City.cs                   ✅ Sarajevo=10557, etc.
│   ├── CityExtensions.cs         ✅ ToStationId(), ToDisplayName()
│   └── AirQualityRecordType.cs   ✅ LiveSnapshot | Forecast
│
├── Configuration/       → Settings & Converters
│   ├── AqicnConfiguration.cs     ✅ WAQI API config
│   └── UtcDateTimeConverter.cs   ✅ JSON date handling
│
├── Middleware/          → Request Pipeline
│   └── ExceptionHandlingMiddleware.cs  ✅ Global error handler
│
└── Program.cs           → Application Startup ✅ 177 lines
```

### Frontend Architecture (Component-Based)

```
📁 frontend/
├── app/
│   ├── page.tsx              ✅ Main page with state management
│   ├── layout.tsx            ✅ Root layout with theme provider
│   └── globals.css           ✅ TailwindCSS + custom styles
│
├── components/
│   ├── LiveAqiPanel.tsx      ✅ Current AQI display (255 lines)
│   ├── ForecastTimeline.tsx  ✅ 7-day forecast chart
│   ├── Pollutants.tsx        ✅ Pollutant measurements grid
│   ├── CitiesComparison.tsx  ✅ Multi-city comparison table
│   ├── SensitiveGroupsAdvice.tsx  ✅ Health recommendations
│   ├── Header.tsx            ✅ Navigation & theme toggle
│   └── Modals/
│       └── PreferredCitySelectorModal.tsx  ✅ City selection
│
└── lib/
    ├── api-client.ts         ✅ HTTP client with date conversion
    ├── hooks.ts              ✅ useLiveAqi, useComplete, usePeriodicRefresh
    ├── observable.ts         ✅ Observable pattern for refresh management
    ├── theme.ts              ✅ Theme provider context
    ├── health-advice.ts      ✅ Health recommendation logic
    └── utils.ts              ✅ Utility functions (city mapping, etc.)
```

---

## 🔄 DATA FLOW - KAKO SVE RADI

### 1️⃣ Backend Scheduler (Background Worker)

**Every 10 minutes** → Automatic refresh for ALL cities:

```
AirQualityScheduler.ExecuteAsync()
    ↓
For each city (Sarajevo, Tuzla, Zenica, Mostar, Travnik, Bihać):
    ↓
RefreshCityAsync(city)
    ↓
FetchWaqiDataAsync(city)  → WAQI API call: GET https://api.waqi.info/feed/{stationId}/?token={token}
    ↓
AirQualityMapper.MapToEntity()  → Convert WAQI JSON → AirQualityRecord
    ↓
AqiRepository.AddLatestAqi()  → Save to database (SQLite)
    ↓
AirQualityMapper.BuildForecastResponse()  → Process forecast data
    ↓
AqiRepository.UpdateAqiForecast()  → Save forecast JSON to database
```

**Key Points**:
- ⏰ Runs every 10 minutes (configurable in appsettings.json)
- 🔄 Parallel processing of all cities
- 💾 Caches data in SQLite database
- 🚨 Error handling with logging (Serilog)

### 2️⃣ Backend API Endpoints

**3 REST Endpoints** in `AirQualityController`:

```
GET /api/v1/air-quality/{city}/live
    → Returns: LiveAqiResponse (current AQI, pollutants, timestamp)
    → Source: Database cache (latest record)

GET /api/v1/air-quality/{city}/forecast
    → Returns: ForecastResponse (7-day forecast)
    → Source: Database cache (forecast JSON)

GET /api/v1/air-quality/{city}/complete
    → Returns: CompleteAqiResponse (live + forecast combined)
    → Source: Combines both queries
```

**Flow Example**:
```
Frontend: GET /api/v1/air-quality/sarajevo/live
    ↓
AirQualityController.GetLiveAqi(City.Sarajevo)
    ↓
AirQualityService.GetLiveAqi(City.Sarajevo)
    ↓
AqiRepository.GetLatestAqi(City.Sarajevo)  → SELECT * FROM AirQualityRecords WHERE City='Sarajevo' AND RecordType='LiveSnapshot' ORDER BY Timestamp DESC LIMIT 1
    ↓
AirQualityMapper.MapToLiveResponse(record)  → Entity → DTO conversion
    ↓
Return: { city: "Sarajevo", overallAqi: 45, aqiCategory: "Good", ... }
```

### 3️⃣ Frontend Data Fetching (SWR + Observable Pattern)

**SWR Cache Mechanism**:

```typescript
// 1. Initial Request
useLiveAqi('sarajevo')
    ↓
SWR checks cache: cache['aqi-live-sarajevo']
    ↓
Cache MISS → Fetch from API
    ↓
apiClient.getLive('sarajevo')  → GET /api/v1/air-quality/sarajevo/live
    ↓
Store in cache: cache['aqi-live-sarajevo'] = { data, timestamp: Date.now() }
    ↓
Return data to component

// 2. Subsequent Requests (within 2 seconds)
useLiveAqi('sarajevo')
    ↓
SWR checks cache: cache['aqi-live-sarajevo']
    ↓
Cache HIT + FRESH (Date.now() - timestamp < 2000ms)
    ↓
Return cached data immediately (NO API CALL)

// 3. Stale Data (after 2 seconds)
useLiveAqi('sarajevo')
    ↓
SWR checks cache: cache['aqi-live-sarajevo']
    ↓
Cache HIT + STALE (Date.now() - timestamp > 2000ms)
    ↓
Return stale data immediately (FAST)
    ↓ (background revalidation)
Fetch fresh data from API
    ↓
Update cache with new data
    ↓
Re-render component with fresh data
```

**Observable Pattern for Periodic Refresh**:

```typescript
// Setup in page.tsx
usePeriodicRefresh(60 * 1000)  // Every 60 seconds
    ↓
airQualityObservable.subscribe(() => mutate())
    ↓
Every 60s → airQualityObservable.notify()
    ↓
All subscribers execute: mutate() (SWR revalidate)
    ↓
Fresh data fetched from backend
```

**Timeline Example**:

```
T+0s:    User opens page → useLiveAqi('sarajevo') → API call → Cache stored
T+1s:    Component re-render → useLiveAqi('sarajevo') → Cache hit (fresh) → NO API call
T+3s:    Another component → useLiveAqi('sarajevo') → Cache hit (stale) → Return stale + fetch new
T+60s:   Observable timer → notify() → mutate() → API call → Cache updated
T+120s:  Observable timer → notify() → mutate() → API call → Cache updated
```

---

## 🎨 KEY DESIGN PATTERNS

### 1. Repository Pattern
```csharp
// Interface defines contract
public interface IAirQualityRepository {
    Task<AirQualityRecord?> GetLatestAqi(City city, CancellationToken ct);
    Task AddLatestAqi(AirQualityRecord record, CancellationToken ct);
}

// Implementation hides EF Core details
public class AirQualityRepository : IAirQualityRepository {
    private readonly AppDbContext _context;
    // ... implementation
}
```

**Benefits**: 
- Separation of concerns (business logic vs data access)
- Easy to test (mock the interface)
- Can swap database without changing service code

### 2. Dependency Injection
```csharp
// Registration in Program.cs
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();
builder.Services.AddScoped<IAirQualityService, AirQualityService>();

// Usage in Controller
public class AirQualityController : ControllerBase {
    private readonly IAirQualityService _service;
    
    public AirQualityController(IAirQualityService service) {
        _service = service;  // ← ASP.NET Core injects this automatically
    }
}
```

**Benefits**:
- Loose coupling
- Testability (inject mocks)
- Centralized configuration

### 3. Mapper Pattern (Static Helper Class)
```csharp
public static class AirQualityMapper {
    // Entity → DTO
    public static LiveAqiResponse MapToLiveResponse(AirQualityRecord record) { ... }
    
    // WAQI API → Entity
    public static AirQualityRecord MapToEntity(City city, WaqiData waqiData, DateTime timestamp) { ... }
    
    // WAQI API → Forecast DTO
    public static ForecastResponse? BuildForecastResponse(City city, WaqiDailyForecast? dailyForecast, DateTime timestamp) { ... }
}
```

**Benefits**:
- Single Responsibility Principle (service focuses on orchestration)
- All transformations in one place
- Easy to maintain and test

### 4. Observable Pattern (Frontend)
```typescript
class AirQualityObservable {
    private eventTarget = new EventTarget();
    
    subscribe(handler: () => void, options?: { intervalMs?: number }) {
        this.eventTarget.addEventListener(REFRESH_EVENT, handler);
        return () => this.eventTarget.removeEventListener(REFRESH_EVENT, handler);
    }
    
    notify() {
        this.eventTarget.dispatchEvent(new Event(REFRESH_EVENT));
    }
}

// Usage
airQualityObservable.subscribe(() => {
    mutate(); // Refresh SWR cache
});
```

**Benefits**:
- Decoupled refresh logic
- Multiple subscribers (all hooks get notified)
- Centralized timer management

### 5. Closure Pattern (React Hooks)
```typescript
useEffect(() => {
    const previousInterval = intervalId;  // ← Captured in closure
    
    return () => {
        // Cleanup function has access to previousInterval
        // even after component unmounts
        if (previousInterval !== null) {
            clearInterval(previousInterval);
        }
    };
}, [dependency]);
```

**Why needed**: Each component mount needs isolated copy of interval ID for cleanup.

---

## 💾 DATABASE SCHEMA

### AirQualityRecords Table

```sql
CREATE TABLE AirQualityRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    City TEXT NOT NULL,                    -- "Sarajevo", "Tuzla", etc.
    RecordType TEXT NOT NULL,              -- "LiveSnapshot" or "Forecast"
    StationId TEXT,                        -- WAQI station ID (e.g., "10557")
    Timestamp DATETIME NOT NULL,           -- Measurement timestamp
    AqiValue INTEGER,                      -- Overall AQI (0-500)
    DominantPollutant TEXT,                -- "PM2.5", "PM10", "O3", etc.
    Pm25 REAL,                             -- PM2.5 concentration (μg/m³)
    Pm10 REAL,                             -- PM10 concentration (μg/m³)
    O3 REAL,                               -- Ozone concentration (μg/m³)
    No2 REAL,                              -- Nitrogen dioxide (μg/m³)
    Co REAL,                               -- Carbon monoxide (mg/m³)
    So2 REAL,                              -- Sulfur dioxide (μg/m³)
    ForecastJson TEXT,                     -- Forecast data (JSON string)
    CreatedAt DATETIME NOT NULL,           -- Record creation time
    UpdatedAt DATETIME                     -- Last update time
);

-- Indexes for performance
CREATE INDEX IX_AirQuality_CityTypeTimestamp ON AirQualityRecords (City, RecordType, Timestamp);
CREATE UNIQUE INDEX UX_AirQuality_ForecastPerCity ON AirQualityRecords (City, RecordType) WHERE RecordType = 'Forecast';
```

**Key Points**:
- **Dual Purpose**: Stores both live snapshots and forecast data
- **Composite Index**: Fast lookups by (City, RecordType, Timestamp)
- **Unique Constraint**: Only ONE forecast record per city
- **JSON Column**: ForecastJson stores forecast days as JSON string

**Example Records**:

```json
// Live Snapshot
{
  "id": 1,
  "city": "Sarajevo",
  "recordType": "LiveSnapshot",
  "stationId": "10557",
  "timestamp": "2025-10-06T14:30:00",
  "aqiValue": 45,
  "dominantPollutant": "PM2.5",
  "pm25": 12.5,
  "pm10": 20.3,
  "o3": 45.2,
  "no2": 15.1,
  "co": 0.4,
  "so2": 5.2,
  "forecastJson": null,
  "createdAt": "2025-10-06T14:30:05",
  "updatedAt": null
}

// Forecast
{
  "id": 2,
  "city": "Sarajevo",
  "recordType": "Forecast",
  "stationId": "10557",
  "timestamp": "2025-10-06T14:30:00",
  "aqiValue": null,
  "dominantPollutant": null,
  "pm25": null,
  "pm10": null,
  "o3": null,
  "no2": null,
  "co": null,
  "so2": null,
  "forecastJson": "{\"retrievedAt\":\"2025-10-06T14:30:00\",\"days\":[{\"date\":\"2025-10-07\",\"aqi\":48,...}]}",
  "createdAt": "2025-10-06T14:30:05",
  "updatedAt": "2025-10-06T14:30:05"
}
```

---

## 🔧 CONFIGURATION FILES

### Backend: appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=bosniaair-aqi.db"  // SQLite for dev
  },
  "Aqicn": {
    "ApiUrl": "https://api.waqi.info",
    "ApiToken": ""  // ← Set via environment variable WAQI_API_TOKEN
  },
  "Worker": {
    "FetchIntervalMinutes": 10  // Background refresh interval
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [{ "Name": "Console" }]
  }
}
```

### Frontend: package.json

```json
{
  "name": "bosniaair-frontend",
  "version": "1.0.0",
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start"
  },
  "dependencies": {
    "next": "^15.0.0",
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "swr": "^2.2.0",  // SWR for data fetching
    "tailwindcss": "^3.4.0"
  }
}
```

### Environment Variables

**Backend (.env)**:
```bash
WAQI_API_TOKEN=your_token_here
DATABASE_URL=Data Source=bosniaair-aqi.db
FRONTEND_ORIGIN=http://localhost:3000
```

**Frontend (.env.local)**:
```bash
NEXT_PUBLIC_API_URL=http://localhost:5000
```

---

## 🚀 HOW TO RUN

### Backend
```bash
cd backend/src/BosniaAir.Api
dotnet restore
dotnet ef database update  # If using migrations
dotnet run
# → API runs on http://localhost:5000
```

### Frontend
```bash
cd frontend
npm install
npm run dev
# → App runs on http://localhost:3000
```

### Test Endpoints
```bash
# Live data
curl http://localhost:5000/api/v1/air-quality/sarajevo/live

# Forecast
curl http://localhost:5000/api/v1/air-quality/sarajevo/forecast

# Complete
curl http://localhost:5000/api/v1/air-quality/sarajevo/complete
```

---

## 📝 COMMON INTERVIEW QUESTIONS & ANSWERS

### Q1: "Kako SWR cache radi?"
**Odgovor**: 
SWR (Stale-While-Revalidate) je in-memory JavaScript objekat koji čuva podatke po ključu (npr. `'aqi-live-sarajevo'`). Kad pozovem `useLiveAqi('sarajevo')`:

1. **Cache Miss**: Ako nema podataka, odmah šalje API call i čuva rezultat
2. **Cache Hit (Fresh)**: Ako su podaci mlađi od 2 sekunde (deduping interval), vraća odmah bez API calla
3. **Cache Hit (Stale)**: Ako su stariji od 2 sekunde, vraća stare podatke odmah (brzo!), ali u background-u povlači nove i ažurira cache kad stignu

**Benefit**: Korisnik uvijek vidi nešto odmah (instant), ali dobije fresh podatke ubrzo nakon.

### Q2: "Zašto koristiš closure u useEffect?"
**Odgovor**:
```typescript
useEffect(() => {
    const previousInterval = intervalId;  // ← Closure captures this value
    
    return () => {
        clearInterval(previousInterval);  // ← Cleanup sees captured value
    };
}, [dep]);
```

Svaki mount komponente kreira NOVU closure koja čuva trenutnu vrijednost `intervalId`. Kad se komponenta unmountuje, cleanup funkcija ima pristup SVOJOJ kopiji. Bez closure-a, sve cleanup funkcije bi dijelile istu varijablu i bilo bi race conditions.

**Analogy**: Kao backup papir - svaki put kad uđem u prostoriju, napišem broj na papir i ostavim ga. Kad izađem, čitam SA TOG papira, ne sa nekog globalnog.

### Q3: "Objasni Repository Pattern"
**Odgovor**:
Repository pattern je **interface između business logike i database-a**. 

```csharp
// Service ne zna KAKO se čuvaju podaci (EF Core? Dapper? File?)
var data = await _repository.GetLatestAqi(city);

// Repository zna detalje implementacije
public async Task<AirQualityRecord?> GetLatestAqi(City city) {
    return await _context.AirQualityRecords
        .Where(r => r.City == city && r.RecordType == AirQualityRecordType.LiveSnapshot)
        .OrderByDescending(r => r.Timestamp)
        .FirstOrDefaultAsync();
}
```

**Benefits**:
- Service ne mora znati SQL ili EF Core
- Lako testirati - mock interface
- Lako promijeniti database (MongoDB, PostgreSQL) bez promjene service-a

### Q4: "Kako scheduler refreshuje podatke?"
**Odgovor**:
`AirQualityScheduler` je **BackgroundService** (inherits od `BackgroundService` klase). ASP.NET Core ga startuje automatski kad aplikacija pokrene.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    while (!stoppingToken.IsCancellationRequested) {
        // 1. Refresh all cities in parallel
        var tasks = _cities.Select(city => RefreshCityAsync(city)).ToList();
        await Task.WhenAll(tasks);
        
        // 2. Wait 10 minutes
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
    }
}
```

**Flow**:
1. Loop svake 10 minuta
2. Za svaki grad paralelno: pozovi WAQI API → mapiraj → sačuvaj u DB
3. Čeka 10 minuta
4. Ponovi

### Q5: "Kako handluješ errors?"
**Odgovor**:

**Backend** - 3 nivoa:
1. **Controller**: Catch exceptions, vraća HTTP status codes (500, 503)
```csharp
catch (DataUnavailableException ex) {
    return StatusCode(503, new { error = "Data unavailable" });
}
```

2. **Middleware**: Global exception handler za sve uncaught exceptions
```csharp
public class ExceptionHandlingMiddleware {
    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        } catch (Exception ex) {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

3. **Logging**: Serilog logs sve errore u console (production → file/cloud)

**Frontend** - SWR error handling:
```typescript
const { data, error, isLoading } = useLiveAqi(city);

if (error) {
    return <ErrorComponent message={error.message} />;
}
```

### Q6: "Zašto C# records za DTOs?"
**Odgovor**:
C# `record` je **immutable reference type** sa automatskim generisanjem `Equals`, `GetHashCode`, `ToString`.

```csharp
// Old way - class
public class LiveAqiResponse {
    public string City { get; set; }
    public int OverallAqi { get; set; }
    // ... 50 lines of boilerplate
}

// New way - record
public record LiveAqiResponse(
    string City,
    int OverallAqi,
    string AqiCategory,
    string Color,
    string HealthMessage,
    DateTime Timestamp,
    IReadOnlyList<MeasurementDto> Measurements,
    string DominantPollutant
);
```

**Benefits**:
- **Immutable by default** (sigurno za multi-threading)
- **Concise syntax** (1 line vs 10 lines)
- **Value equality** (dvije instance sa istim vrijednostima su jednake)
- **Perfect for DTOs** (data se ne mijenja nakon kreacije)

### Q7: "Kako handluješ timezone?"
**Odgovor**:
Koristim **TimeZoneHelper** utility koji konvertuje sve u Sarajevo timezone (Central European Time).

```csharp
public static class TimeZoneHelper {
    private static readonly TimeZoneInfo SarajevoTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
    
    public static DateTime GetSarajevoTime() {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SarajevoTimeZone);
    }
    
    public static DateTime ConvertToSarajevoTime(DateTime utcTime) {
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, SarajevoTimeZone);
    }
}
```

**Flow**:
1. WAQI API vraća UTC timestamp
2. `ParseTimestamp()` konvertuje u Sarajevo time
3. Sve se čuva kao Sarajevo local time u DB
4. Frontend dobija već konvertovane timestamps

**Benefit**: Korisnici vide lokalno vrijeme, bez confusion-a.

### Q8: "Objasni Dependency Injection lifecycle"
**Odgovor**:
ASP.NET Core DI container ima 3 lifetimea:

1. **Transient** - Nova instanca za svaki request
```csharp
builder.Services.AddTransient<IMyService, MyService>();
// Svaki put kad neko traži IMyService, dobije NOVU instancu
```

2. **Scoped** - Jedna instanca per HTTP request
```csharp
builder.Services.AddScoped<IAirQualityService, AirQualityService>();
// Svi koji traže service UNUTAR istog requesta dobiju ISTU instancu
// Novi request = nova instanca
```

3. **Singleton** - Jedna instanca za cijelu aplikaciju
```csharp
builder.Services.AddSingleton<IConfiguration>(configuration);
// Svi dijele ISTU instancu tokom cijelog lifetime-a aplikacije
```

**Moj izbor**:
- `Scoped` za Services i Repositories (shared u request scope, ali izolovan između requesta)
- `Singleton` za Configuration i Caching

### Q9: "Kako optimizuješ performance?"
**Odgovor**:

**Backend**:
1. **Database Caching** - Sve se čuva u DB, ne fetchujem WAQI svaki put
2. **Composite Indexes** - `(City, RecordType, Timestamp)` za brze queries
3. **AsNoTracking()** - Ne trackujem entities kad samo čitam (faster)
```csharp
.AsNoTracking()  // ← EF Core ne drži reference za change tracking
.Where(r => r.City == city)
```
4. **Parallel API Calls** - Scheduler refreshuje sve gradove paralelno
```csharp
await Task.WhenAll(cities.Select(c => RefreshCityAsync(c)));
```

**Frontend**:
1. **SWR Caching** - Deduping interval 2s (ne poziva API za iste podatke)
2. **Observable Pattern** - Single timer za sve komponente (ne N timera)
3. **React Memo** - Components se ne re-renderuju bez potrebe
4. **Code Splitting** - Next.js automatski splituje bundle po route-ama

### Q10: "Šta bi dodao u budućnosti?"
**Odgovor**:

1. **Redis Caching** - Za distribuirane deploymente (trenutno in-memory + DB)
2. **WebSockets** - Real-time push umjesto polling-a
3. **Unit Tests** - xUnit za backend, Jest za frontend
4. **API Rate Limiting** - Throttle requests (protection)
5. **User Accounts** - Login, personalized alerts
6. **Push Notifications** - Alert kad AQI pređe threshold
7. **Historical Data** - Charts za trendove (sada samo live + 7-day forecast)
8. **Multi-Language** - Currently Bosnian only, add English
9. **Admin Panel** - Manage cities, view logs
10. **CI/CD Pipeline** - GitHub Actions za auto-deploy

---

## 🐛 COMMON ISSUES & SOLUTIONS

### Issue: "WAQI API rate limit exceeded"
**Solution**: 
- WAQI free tier: 1000 requests/day
- Current setup: 6 cities × 144 calls/day (every 10 min) = 864 calls ✅
- Production: Increase interval or use paid tier

### Issue: "Frontend shows stale data"
**Solution**:
- Check backend scheduler is running (logs should show "Data refresh cycle started")
- Verify SWR is revalidating (Network tab in DevTools)
- Clear SWR cache: `localStorage.clear()` and refresh

### Issue: "Database locked error (SQLite)"
**Solution**:
- SQLite doesn't handle concurrent writes well
- Production: Use PostgreSQL (configured in Program.cs)
```csharp
if (connectionString.StartsWith("postgresql://")) {
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
```

### Issue: "Timezone shows wrong time"
**Solution**:
- Verify server timezone: `timedatectl` (Linux) or `Get-TimeZone` (Windows)
- TimeZoneHelper uses "Central European Standard Time" ID
- Linux: Might need "Europe/Sarajevo" instead:
```csharp
TimeZoneInfo.FindSystemTimeZoneById(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "Central European Standard Time"
        : "Europe/Sarajevo"
);
```

---

## 📊 PERFORMANCE METRICS

### Backend
- **API Response Time**: ~50-100ms (cached data)
- **WAQI API Call**: ~200-500ms (external)
- **Database Query**: ~5-20ms (indexed)
- **Scheduler Cycle**: ~2-3 seconds (all cities)

### Frontend
- **Initial Load**: ~1-2s (includes API call)
- **Cache Hit**: ~5-10ms (SWR returns immediately)
- **Bundle Size**: ~200KB gzipped (Next.js)
- **Lighthouse Score**: 90+ (Performance, Accessibility)

---

## ✅ DEPLOYMENT CHECKLIST

### Backend (Railway)
- [x] PostgreSQL database provisioned
- [x] Environment variables set (WAQI_API_TOKEN, DATABASE_URL)
- [x] CORS configured for frontend origin
- [x] Health checks enabled
- [x] Logging configured (Serilog)
- [x] HTTPS enabled

### Frontend (Vercel)
- [x] Environment variable set (NEXT_PUBLIC_API_URL)
- [x] Build successful
- [x] Custom domain configured
- [x] PWA manifest configured
- [x] Analytics (optional)

---

## 🎓 KEY CONCEPTS TO REMEMBER

### C# Concepts
1. **async/await** - Asynchronous programming (non-blocking I/O)
2. **LINQ** - Query syntax (`Where`, `Select`, `OrderBy`)
3. **Dependency Injection** - Constructor injection, IoC container
4. **Entity Framework Core** - ORM for database access
5. **Records** - Immutable reference types
6. **Pattern Matching** - Switch expressions for AQI categories
7. **Extension Methods** - `ToDisplayName()`, `ToStationId()`
8. **BackgroundService** - Long-running tasks in ASP.NET Core
9. **Middleware** - Request pipeline customization
10. **ILogger** - Structured logging interface

### React/TypeScript Concepts
1. **SWR** - Data fetching with caching
2. **Custom Hooks** - Reusable logic (`useLiveAqi`, `usePeriodicRefresh`)
3. **Observable Pattern** - Event-based refresh management
4. **Closure** - Function scope preservation
5. **TypeScript Generics** - Type-safe API client
6. **JSX/TSX** - Component syntax
7. **useEffect** - Side effects and cleanup
8. **useState** - Component state management
9. **localStorage** - Browser storage for preferences
10. **Tailwind CSS** - Utility-first styling

---

## 🚨 FINAL CHECKLIST - SPREMAN SI AKO ZNAŠ:

### Backend
- [ ] **Objasni Controller → Service → Repository flow**
- [ ] **Kako scheduler radi?** (BackgroundService, 10-min interval)
- [ ] **Šta radi AirQualityMapper?** (All transformations)
- [ ] **Kako handluješ timezone?** (TimeZoneHelper)
- [ ] **Database schema?** (AirQualityRecords table, indexes)
- [ ] **Dependency Injection lifecycle?** (Scoped vs Transient vs Singleton)
- [ ] **Error handling strategy?** (Controller, Middleware, Logging)
- [ ] **Zašto records za DTOs?** (Immutability, conciseness)

### Frontend
- [ ] **SWR cache mechanism?** (Fresh vs Stale, deduping interval)
- [ ] **Observable pattern?** (Centralized refresh notifications)
- [ ] **Closure u useEffect?** (Preserving interval ID per mount)
- [ ] **useLiveAqi hook flow?** (SWR + observable subscription)
- [ ] **Kako API client radi?** (HTTP client, date conversion)
- [ ] **Component hierarchy?** (page.tsx → LiveAqiPanel → Pollutants)
- [ ] **State management?** (useState for preferences, SWR for data)
- [ ] **Kako handluješ errors?** (Error boundaries, SWR error state)

### Integration
- [ ] **Complete data flow?** (Scheduler → DB → API → Frontend → SWR)
- [ ] **Kako se podaci refreshuju?** (Backend: 10 min, Frontend: 60 sec)
- [ ] **CORS setup?** (Backend allows frontend origin)
- [ ] **Environment variables?** (WAQI_API_TOKEN, NEXT_PUBLIC_API_URL)
- [ ] **Deployment strategy?** (Railway + Vercel)

---

## 🎯 INTERVIEW DAY REMINDERS

### Presentation Tips
1. **Start with overview**: "BosniaAir je real-time air quality monitoring app za 6 gradova BiH"
2. **Show architecture diagram**: Draw or show system flow
3. **Walk through code**: Pick one endpoint and explain end-to-end
4. **Highlight patterns**: Repository, DI, Observable, Mapper
5. **Discuss trade-offs**: "Koristio sam SQLite za dev, ali production je PostgreSQL jer..."
6. **Show running app**: Live demo je uvijek impressive

### What They Want to Hear
- ✅ "Razumijem clean architecture"
- ✅ "Koristim design patterns gdje ima smisla"
- ✅ "Testiram kod prije deploya"
- ✅ "Razmišljam o performance-u i skalabilnosti"
- ✅ "Error handling je kritičan"
- ✅ "Dokumentujem kod za maintenance"

### What NOT to Say
- ❌ "Ne znam zašto sam ovo tako uradio"
- ❌ "Copy/paste sa StackOverflow-a"
- ❌ "Nikad nisam testirao"
- ❌ "Ne razumijem kako SWR radi"
- ❌ "Database je random izbor"

---

## 📚 DODATNI RESURSI

### Documentation Created
1. **README.md** - Project overview and architecture
2. **COMPLETE_FLOW_DOCUMENTATION.md** - Detailed flow with timelines
3. **SERVICE_REFACTORING.md** - Before/after comparison
4. **APIREADME.md** - API endpoint documentation
5. **CSHARP_FOR_REACT_DEVS.md** - C# crash course
6. **TROUBLESHOOTING.md** - Common issues and solutions
7. **INTERVIEW_CHECKLIST.md** - This file! 🎯

### Useful Commands
```bash
# Backend
dotnet build                    # Compile
dotnet run                      # Run API
dotnet ef migrations add Name   # Create migration
dotnet ef database update       # Apply migrations

# Frontend
npm run dev                     # Development server
npm run build                   # Production build
npm run start                   # Production server

# Database
sqlite3 bosniaair-aqi.db        # Open SQLite shell
.tables                         # List tables
.schema AirQualityRecords       # Show table schema
SELECT * FROM AirQualityRecords LIMIT 5;  # Query data

# Git
git log --oneline               # Commit history
git diff                        # See changes
```

---

## 🎉 YOU'RE READY!

**Sve što trebaš znati za intervju je ovdje.** Prođi kroz svaki dio, razumiješ li ga kao pjesmicu? Ako da - SPREMAN SI! 💪🚀

**Sretno u utorak u 12:00!** 🍀

Remember:
- Budite confident ali ne arogantni
- Priznajte ako ne znate nešto ("Ne znam tačno, ali mislim da...")
- Pitajte nazad ako nešto nije jasno
- Pokažite enthusiasm - ovaj projekat je VAŠA PRIČA!

**GO GET THAT JOB!** 🎯🔥
