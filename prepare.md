

# 📘 POTPUNA PRIPREMA ZA RAZGOVOR - BOSNIA AIR PROJEKAT

## PART 1: HIGH-LEVEL OVERVIEW 🎯

### 1.1 ŠTA JE OVAJ PROJEKAT?

**BosniaAir** je **real-time aplikacija za praćenje kvaliteta vazduha** u gradovima Bosne i Hercegovine. Aplikacija:
- Prikazuje **trenutni AQI (Air Quality Index)** za odabrani grad
- Daje **prognoze** kvaliteta vazduha za naredne dane
- Prikazuje koncentracije **zagađivača** (PM2.5, PM10, O3, NO2, CO, SO2)
- Daje **zdravstvene savjete** zasnovane na trenutnoj kvaliteti vazduha
- Omogućava **poređenje gradova**

---

### 1.2 ARHITEKTURA NA VISOKOM NIVOU

```
┌─────────────────┐
│   USER/BROWSER  │ (korisnik otvara web app)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   FRONTEND      │ Next.js 14 + React + TypeScript
│   (Vercel)      │ • Prikazuje UI
└────────┬────────┘ • Poziva backend API
         │          • Koristi SWR za caching
         │ HTTP/REST
         ▼
┌─────────────────┐
│   BACKEND API   │ .NET 8 + ASP.NET Core
│   (Railway)     │ • REST API endpoints
└────────┬────────┘ • Business logika
         │          • Cache menadžment
         │ SQL
         ▼
┌─────────────────┐
│   DATABASE      │ PostgreSQL (production) / SQLite (dev)
│   (Supabase)    │ • Čuva cache podataka
└─────────────────┘ • Live snapshots i forecasts
         ▲
         │
┌────────┴────────┐
│ BACKGROUND      │ AirQualityScheduler (Hosted Service)
│ WORKER          │ • Periodic refresh (svakih 10 min)
└────────┬────────┘ • Automatski poziva WAQI API
         │
         ▼
┌─────────────────┐
│  EXTERNAL API   │ WAQI API (World Air Quality Index)
│  (api.waqi.info)│ • Izvor podataka
└─────────────────┘ • Real-time measurements
```

---

### 1.3 TECH STACK OVERVIEW

#### **BACKEND** 🔧
| Tehnologija | Svrha |
|------------|-------|
| **.NET 8** | Runtime i framework (najnovija LTS verzija) |
| **ASP.NET Core** | Web framework za kreiranje REST API-ja |
| **Entity Framework Core** | ORM (Object-Relational Mapper) za rad sa bazom |
| **SQLite** | Development database (file-based, jednostavna) |
| **PostgreSQL** | Production database (Supabase hosted) |
| **Serilog** | Logging framework (structured logging) |
| **HttpClient + Resilience** | HTTP client sa retry policy za external API |
| **Swagger/OpenAPI** | API dokumentacija (auto-generated) |

#### **FRONTEND** 💻
| Tehnologija | Svrha |
|------------|-------|
| **Next.js 14** | React framework (SSR, routing, optimizacije) |
| **React 18** | UI library za komponente |
| **TypeScript** | Type safety i bolji developer experience |
| **Tailwind CSS** | Utility-first CSS framework |
| **SWR** | Data fetching library sa automatskim caching-om |

---

### 1.4 KAKO SISTEM RADI? (DATA FLOW)

#### **Korak 1: Background Worker (automatski)**
```
Svakih 10 minuta:
1. AirQualityScheduler se "budi"
2. Za svaki grad (Sarajevo, Tuzla, Zenica, Mostar, Travnik, Bihać):
   - Poziva AirQualityService.RefreshCityAsync()
   - Service poziva WAQI API (npr. api.waqi.info/feed/@10557/?token=XXX)
   - Dobije JSON sa live data i forecast
   - Parsira podatke
   - Sprema u database kao AirQualityRecord
3. Podaci su sada cached u bazi
```

#### **Korak 2: User Request (kada korisnik otvori app)**
```
1. Frontend (Next.js) renderuje stranicu
2. User bira grad (npr. Sarajevo)
3. Frontend poziva: GET /api/v1/air-quality/Sarajevo/complete
4. Backend (Controller) prima request
5. Controller poziva Service
6. Service poziva Repository
7. Repository izvlači podatke iz database (ne poziva WAQI!)
8. Podaci se vraćaju kroz lanac: Repository → Service → Controller → Frontend
9. Frontend prikazuje podatke u komponentama
```

**KLJUČNO:** Backend **NE POZIVA** WAQI API na svaki user request! Koristi **cached podatke** iz baze koje background worker refreshuje.

---

### 1.5 STRUKTURA PROJEKTA

```
sarajevoairvibe/
│
├── backend/                    # .NET API
│   └── src/
│       └── BosniaAir.Api/
│           ├── Program.cs      # Entry point, konfiguracija servisa
│           ├── Controllers/    # API endpoints
│           ├── Services/       # Business logika
│           ├── Repositories/   # Data access layer
│           ├── Data/           # EF Core DbContext
│           ├── Entities/       # Database modeli
│           ├── Dtos/           # Data transfer objects (API responses)
│           ├── Enums/          # Enumeracije (City, RecordType)
│           ├── Middleware/     # Global error handling
│           └── Utilities/      # Helper functions (timezone)
│
└── frontend/                   # Next.js App
    ├── app/
    │   ├── page.tsx           # Main page
    │   └── layout.tsx         # Root layout
    ├── components/            # React komponente
    ├── lib/
    │   ├── api-client.ts     # HTTP client za backend
    │   ├── hooks.ts          # Custom React hooks
    │   └── utils.ts          # Helper functions
    └── public/                # Static assets
```

---

## PART 2: C# I .NET OSNOVE ZA OVAJ PROJEKAT 📚

Pošto si početnik u .NET-u, evo šta **MORAŠ** znati za ovaj projekat:

### 2.1 KLJUČNI C# KONCEPTI

#### **1. Dependency Injection (DI)**
Najvažniji koncept u modernom .NET-u!

```csharp
// U Program.cs registruješ servise:
builder.Services.AddScoped<IAirQualityService, AirQualityService>();
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();

// U Controlleru ili Serviceu ih dobijaš kroz constructor:
public class AirQualityController : ControllerBase
{
    private readonly IAirQualityService _airQualityService;
    
    // ASP.NET automatski inject-uje instancu!
    public AirQualityController(IAirQualityService airQualityService)
    {
        _airQualityService = airQualityService;
    }
}
```

**Zašto je ovo važno?**
- Lakše testiranje (možeš inject-ovati mock objekte)
- Loose coupling (kod nije zavisan od konkretnih implementacija)
- ASP.NET upravlja lifecycle-om objekata

**Životni ciklus servisa:**
- **Scoped**: Novi objekat za svaki HTTP request (koristi se za DB context)
- **Singleton**: Jedan objekat za cijelu aplikaciju
- **Transient**: Novi objekat svaki put kada se traži

#### **2. Async/Await**
Sve operacije su asynchronous!

```csharp
public async Task<LiveAqiResponse> GetLiveAqi(City city, CancellationToken cancellationToken = default)
{
    var cached = await _repository.GetLatestAqi(city, cancellationToken);
    return MapToLiveResponse(cached);
}
```

**Ključno:**
- `async` - metoda je asynchronous
- `await` - čeka rezultat bez blokiranja thread-a
- `Task<T>` - return type za async metode
- `CancellationToken` - omogućava otkazivanje dugih operacija

#### **3. Records**
Moderan način kreiranja immutable data klasa (C# 9+):

```csharp
public record CompleteAqiResponse(
    LiveAqiResponse LiveData,
    ForecastResponse ForecastData,
    DateTime RetrievedAt
);
```

Ekvivalentno sa:
```csharp
public class CompleteAqiResponse
{
    public LiveAqiResponse LiveData { get; init; }
    public ForecastResponse ForecastData { get; init; }
    public DateTime RetrievedAt { get; init; }
    
    public CompleteAqiResponse(LiveAqiResponse liveData, ForecastResponse forecastData, DateTime retrievedAt)
    {
        LiveData = liveData;
        ForecastData = forecastData;
        RetrievedAt = retrievedAt;
    }
}
```

#### **4. Nullable Reference Types**
```csharp
public string? DominantPollutant { get; set; }  // Može biti null
public int? AqiValue { get; set; }              // Nullable int
```

`?` znači da vrijednost može biti `null`.

#### **5. LINQ (Language Integrated Query)**
Za rad sa kolekcijama:

```csharp
// Dohvati najnoviji record za grad
return await _context.AirQualityRecords
    .AsNoTracking()  // Ne trackuje promjene (performance)
    .Where(r => r.City == city && r.RecordType == AirQualityRecordType.LiveSnapshot)
    .OrderByDescending(r => r.Timestamp)
    .FirstOrDefaultAsync(cancellationToken);
```

---

### 2.2 ASP.NET CORE OSNOVE

#### **Middleware Pipeline**
Request prolazi kroz pipeline prije nego dođe do Controllera:

```csharp
// U Program.cs:
app.UseSerilogRequestLogging();              // 1. Logira request
app.UseMiddleware<ExceptionHandlingMiddleware>(); // 2. Hvata errore
app.UseCors("FrontendOnly");                 // 3. CORS headers
app.UseRouting();                            // 4. Routing
app.MapControllers();                        // 5. Route do Controllera
```

#### **Controllers i Routing**
```csharp
[ApiController]                              // Omogućava API convencinse
[Route("api/v1/air-quality")]               // Base route
public class AirQualityController : ControllerBase
{
    [HttpGet("{city}/live")]                // GET /api/v1/air-quality/Sarajevo/live
    public async Task<ActionResult<LiveAqiResponse>> GetLiveAqi(
        [FromRoute] City city,              // Parametar iz URL-a
        CancellationToken cancellationToken = default)
    {
        var response = await _airQualityService.GetLiveAqi(city, cancellationToken);
        return Ok(response);                 // 200 OK
    }
}
```

#### **Configuration**
Čitanje iz appsettings.json:

```csharp
// appsettings.json
{
  "Aqicn": {
    "ApiUrl": "https://api.waqi.info"
  }
}

// Program.cs
builder.Services.Configure<AqicnConfiguration>(options =>
{
    builder.Configuration.GetSection("Aqicn").Bind(options);
});

// U Serviceu
public AirQualityService(IOptions<AqicnConfiguration> aqicnOptions)
{
    _apiToken = aqicnOptions.Value.ApiToken;
}
```

---

### 2.3 ENTITY FRAMEWORK CORE

#### **DbContext**
Glavni ulaz u bazu podataka:

```csharp
public class AppDbContext : DbContext
{
    public DbSet<AirQualityRecord> AirQualityRecords => Set<AirQualityRecord>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Konfiguracija tabela, indexa, constrainta
    }
}
```

#### **Migracije**
EF Core kreira SQL iz C# modela:

```bash
# Kreiraj novu migraciju
dotnet ef migrations add InitialCreate

# Primijeni migracije na bazu
dotnet ef database update
```

#### **Query i Save**
```csharp
// Query
var record = await _context.AirQualityRecords
    .FirstOrDefaultAsync(r => r.Id == id);

// Insert
_context.AirQualityRecords.Add(newRecord);
await _context.SaveChangesAsync();

// Update
existing.Timestamp = DateTime.Now;
await _context.SaveChangesAsync();  // EF trackuje promjene automatski
```

---

### 2.4 BACKGROUND SERVICES

```csharp
public class AirQualityScheduler : BackgroundService
{
    // ExecuteAsync se automatski poziva kada app startuje
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunRefreshCycle(stoppingToken);
            await Task.Delay(_interval, stoppingToken);  // Čeka 10 minuta
        }
    }
}
```

Registracija:
```csharp
builder.Services.AddHostedService<AirQualityScheduler>();
```

---

## PART 3: LOW-LEVEL TECHNICAL DEEP DIVE 🔬 

Starting (6/8) *Kreiraj low-level technical deep dive*



### 3.1 BACKEND KOMPONENTE - DETALJNO

#### **A) Program.cs - Aplikacioni Entry Point**

Ovo je **main fajl** aplikacije. Sve kreće ovdje.

**Šta radi:**

1. **Učitava Environment Variables**
```csharp
Env.Load("../../../.env");  // Učitava WAQI_API_TOKEN i druge sekrete
```

2. **Konfigurira Logging (Serilog)**
```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()  // Logovi idu u terminal/console
    .CreateLogger();
```

3. **CORS Policy** (dozvoljava frontend da poziva API)
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", policy =>
    {
        policy.WithOrigins("http://localhost:3000", frontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

4. **JSON Serialization Config**
```csharp
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
})
```
- `UtcDateTimeConverter` - Konvertuje DateTime u UTC za konzistentnost
- `JsonStringEnumConverter` - Enums u JSON kao stringovi (npr. "Sarajevo" umjesto brojeva)
- `CamelCase` - JSON property names su camelCase (aqiValue, a ne AqiValue)

5. **Database Setup**
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                       ?? "Data Source=bosniaair-aqi.db";

if (connectionString.StartsWith("postgresql://"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));  // Production: PostgreSQL
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));  // Development: SQLite
}
```

6. **Dependency Injection Registration**
```csharp
// HTTP Client sa retry policy
builder.Services.AddHttpClient<AirQualityService>()
    .ConfigureHttpClient((sp, client) =>
    {
        client.BaseAddress = new Uri("https://api.waqi.info/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddStandardResilienceHandler();  // Automatski retry na network errors

// Servisi
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();
builder.Services.AddScoped<IAirQualityService>(sp => sp.GetRequiredService<AirQualityService>());
builder.Services.AddHostedService<AirQualityScheduler>();  // Background worker
```

7. **Middleware Pipeline**
```csharp
app.UseSerilogRequestLogging();                      // 1. Log request
app.UseMiddleware<ExceptionHandlingMiddleware>();   // 2. Global error handling
app.UseCors("FrontendOnly");                         // 3. CORS
app.UseRouting();                                    // 4. Routing
app.MapControllers();                                // 5. Map to controllers
```

---

#### **B) AirQualityController - API Endpoints**

**3 glavna endpointa:**

1. **GET /api/v1/air-quality/{city}/live**
```csharp
[HttpGet("{city}/live")]
public async Task<ActionResult<LiveAqiResponse>> GetLiveAqi(
    [FromRoute] City city,
    CancellationToken cancellationToken = default)
{
    try
    {
        var response = await _airQualityService.GetLiveAqi(city, cancellationToken);
        return Ok(response);  // 200 OK
    }
    catch (DataUnavailableException ex)
    {
        return StatusCode(503, new { error = "Data unavailable", message = ex.Message });
    }
}
```

**Šta radi:**
- Prima `city` iz URL-a (npr. `/Sarajevo/live`)
- `[FromRoute]` - ASP.NET automatski parsira string "Sarajevo" u `City.Sarajevo` enum
- Poziva Service metodu
- Hvata greške i vraća odgovarajući HTTP status

2. **GET /api/v1/air-quality/{city}/forecast**
- Isti pattern kao `/live`, samo vraća forecast podatke

3. **GET /api/v1/air-quality/{city}/complete** ⭐ **Najvažniji!**
- Kombinuje live i forecast u jedan response
- Frontend koristi ovaj endpoint

---

#### **C) AirQualityService - Business Logic**

Ovo je **srce aplikacije**. Ovdje se dešava sva logika.

**Ključne metode:**

**1. GetLiveAqi()**
```csharp
public async Task<LiveAqiResponse> GetLiveAqi(City city, CancellationToken cancellationToken = default)
{
    // Dohvati iz cache-a (database)
    var cached = await _repository.GetLatestAqi(city, cancellationToken);
    
    if (cached is not null)
    {
        return MapToLiveResponse(cached);  // Transform DB model u API response
    }

    throw new DataUnavailableException(city, "live");
}
```

**2. GetLiveAqiAndForecast()** - Kombinuje oba
```csharp
public async Task<CompleteAqiResponse> GetLiveAqiAndForecast(City city, ...)
{
    var live = await GetLiveAqi(city, cancellationToken);
    
    ForecastResponse forecast;
    try
    {
        forecast = await GetAqiForecast(city, cancellationToken);
    }
    catch (DataUnavailableException)
    {
        // Ako nema forecasta, vrati prazan niz umjesto da pukne
        forecast = new ForecastResponse(city.ToDisplayName(), Array.Empty<ForecastDayDto>(), ...);
    }

    return new CompleteAqiResponse(live, forecast, TimeZoneHelper.GetSarajevoTime());
}
```

**3. RefreshInternalAsync()** ⭐ **Najvažnija metoda!**

Ovo je metoda koja ZAPRAVO dohvata podatke sa WAQI API-ja:

```csharp
private async Task<RefreshResult> RefreshInternalAsync(City city, CancellationToken cancellationToken)
{
    // 1. Fetch sa WAQI API
    var waqiData = await FetchWaqiDataAsync(city, cancellationToken);
    var timestamp = ParseTimestamp(waqiData.Time);

    // 2. Kreiraj DB entity
    var record = new AirQualityRecord
    {
        City = city,
        StationId = city.ToStationId(),  // 10557 za Sarajevo
        RecordType = AirQualityRecordType.LiveSnapshot,
        Timestamp = timestamp,
        AqiValue = waqiData.Aqi,
        DominantPollutant = MapDominantPollutant(waqiData.Dominentpol),
        Pm25 = waqiData.Iaqi?.Pm25?.V,
        Pm10 = waqiData.Iaqi?.Pm10?.V,
        // ... ostali pollutanti
    };

    // 3. Spremi u bazu
    await _repository.AddLatestAqi(record, cancellationToken);
    
    // 4. Ako ima forecast, spremi i njega
    if (waqiData.Forecast?.Daily is not null)
    {
        var forecastDays = BuildForecastDays(waqiData.Forecast.Daily);
        var serialized = JsonSerializer.Serialize(forecastDays);
        await _repository.UpdateAqiForecast(city, serialized, timestamp, cancellationToken);
    }

    return new RefreshResult(liveResponse, forecastResponse);
}
```

**4. FetchWaqiDataAsync()** - HTTP poziv ka WAQI API
```csharp
private async Task<WaqiData> FetchWaqiDataAsync(City city, CancellationToken cancellationToken)
{
    var stationId = city.ToStationId();  // 10557 za Sarajevo
    var requestUri = $"feed/{stationId}/?token={_apiToken}";
    
    // HTTP GET request
    using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
    response.EnsureSuccessStatusCode();  // Throw ako nije 2xx
    
    // Deserialize JSON
    var waqiResponse = await JsonSerializer.DeserializeAsync<WaqiApiResponse>(...);
    
    if (waqiResponse.Status != "ok" || waqiResponse.Data is null)
    {
        throw new InvalidOperationException($"WAQI API returned invalid status");
    }

    return waqiResponse.Data;
}
```

**WAQI API Response format:**
```json
{
  "status": "ok",
  "data": {
    "aqi": 85,
    "time": {
      "iso": "2025-10-03T10:00:00+02:00",
      "v": 1727946000
    },
    "dominentpol": "pm25",
    "iaqi": {
      "pm25": { "v": 85 },
      "pm10": { "v": 120 },
      "o3": { "v": 15 }
    },
    "forecast": {
      "daily": {
        "pm25": [
          { "avg": 97, "day": "2025-10-03", "min": 55, "max": 152 }
        ]
      }
    }
  }
}
```

---

#### **D) AirQualityScheduler - Background Worker**

Ovo je **samostalan proces** koji radi u pozadini:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Scheduler starting...");
    
    // Odmah refresuj na startup
    await RunRefreshCycle(stoppingToken);

    // Beskonačna petlja
    while (!stoppingToken.IsCancellationRequested)
    {
        await Task.Delay(_interval, stoppingToken);  // Čeka 10 minuta
        await RunRefreshCycle(stoppingToken);
    }
}

private async Task RunRefreshCycle(CancellationToken cancellationToken)
{
    // Refresuj sve gradove paralelno
    var tasks = _cities.Select(city => RefreshCityAsync(city, cancellationToken));
    await Task.WhenAll(tasks);
}

private async Task RefreshCityAsync(City city, CancellationToken cancellationToken)
{
    using var scope = _scopeFactory.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IAirQualityService>();
    
    await service.RefreshCityAsync(city, cancellationToken);
}
```

**Zašto Scope?**
- `AirQualityService` je Scoped (jedan po request-u)
- Background worker je Singleton (živi cijelo vrijeme)
- Mora se kreirati novi scope za svaki refresh cycle

---

#### **E) AirQualityRepository - Data Access**

**Interface:**
```csharp
public interface IAirQualityRepository
{
    Task AddLatestAqi(AirQualityRecord record, CancellationToken cancellationToken = default);
    Task<AirQualityRecord?> GetLatestAqi(City city, CancellationToken cancellationToken = default);
    Task UpdateAqiForecast(City city, string forecastJson, DateTime timestamp, ...);
    Task<AirQualityRecord?> GetForecastAsync(City city, CancellationToken cancellationToken = default);
}
```

**Implementacija:**

**1. GetLatestAqi** - Dohvati najnoviji live snapshot
```csharp
public async Task<AirQualityRecord?> GetLatestAqi(City city, CancellationToken cancellationToken = default)
{
    return await _context.AirQualityRecords
        .AsNoTracking()  // Read-only, brže
        .Where(r => r.City == city && r.RecordType == AirQualityRecordType.LiveSnapshot)
        .OrderByDescending(r => r.Timestamp)  // Najnoviji prvi
        .FirstOrDefaultAsync(cancellationToken);
}
```

**SQL koji EF Core generiše:**
```sql
SELECT TOP 1 * FROM AirQualityRecords
WHERE City = 'Sarajevo' AND RecordType = 'LiveSnapshot'
ORDER BY Timestamp DESC;
```

**2. UpdateAqiForecast** - Upsert forecast (insert ili update)
```csharp
public async Task UpdateAqiForecast(City city, string forecastJson, DateTime timestamp, ...)
{
    var existing = await _context.AirQualityRecords
        .FirstOrDefaultAsync(r => r.City == city && r.RecordType == AirQualityRecordType.Forecast, ...);

    if (existing is null)
    {
        // INSERT
        _context.AirQualityRecords.Add(new AirQualityRecord { ... });
    }
    else
    {
        // UPDATE
        existing.ForecastJson = forecastJson;
        existing.Timestamp = timestamp;
        existing.UpdatedAt = TimeZoneHelper.GetSarajevoTime();
    }

    await _context.SaveChangesAsync(cancellationToken);
}
```

---

#### **F) Entities & Database Schema**

**AirQualityRecord Entity:**
```csharp
public class AirQualityRecord
{
    public int Id { get; set; }                    // Primary key
    public City City { get; set; }                 // Enum: Sarajevo, Tuzla, ...
    public AirQualityRecordType RecordType { get; set; }  // LiveSnapshot ili Forecast
    public string StationId { get; set; }          // WAQI station ID (npr. "@10557")
    public DateTime Timestamp { get; set; }        // Kada je measurement napravljen
    
    // Pollutant values
    public int? AqiValue { get; set; }
    public string? DominantPollutant { get; set; }
    public double? Pm25 { get; set; }
    public double? Pm10 { get; set; }
    public double? O3 { get; set; }
    public double? No2 { get; set; }
    public double? Co { get; set; }
    public double? So2 { get; set; }
    
    // Forecast data (samo za RecordType.Forecast)
    public string? ForecastJson { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**Database Schema (SQLite/PostgreSQL):**
```sql
CREATE TABLE AirQualityRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    City TEXT NOT NULL,
    RecordType TEXT NOT NULL,
    StationId TEXT,
    Timestamp DATETIME NOT NULL,
    AqiValue INTEGER,
    DominantPollutant TEXT,
    Pm25 REAL,
    Pm10 REAL,
    O3 REAL,
    No2 REAL,
    Co REAL,
    So2 REAL,
    ForecastJson TEXT,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME
);

-- Index za brze query-je
CREATE INDEX IX_AirQuality_CityTypeTimestamp 
ON AirQualityRecords(City, RecordType, Timestamp);

-- Unique constraint: samo jedan forecast po gradu
CREATE UNIQUE INDEX UX_AirQuality_ForecastPerCity 
ON AirQualityRecords(City, RecordType) 
WHERE RecordType = 'Forecast';
```

**Kako se čuvaju podaci:**
- **Live snapshots**: Svaki refresh kreira novi record. Stari ostaju (historical data).
- **Forecasts**: Uvijek se ažurira isti record za grad (upsert). Nema historije forecasta.

---

#### **G) DTOs (Data Transfer Objects)**

**Zašto DTOs?**
- Database entities ≠ API responses
- DTOs daju kontrolu nad JSON strukturom
- Mogu kombinovati podatke iz više entiteta

**LiveAqiResponse:**
```csharp
public record LiveAqiResponse(
    string City,
    int OverallAqi,
    string AqiCategory,        // "Good", "Moderate", "Unhealthy", ...
    string Color,              // "#00E400", "#FFFF00", ...
    string HealthMessage,      // Zdravstveni savjet
    DateTime Timestamp,
    IReadOnlyList<MeasurementDto> Measurements,
    string DominantPollutant
);
```

**Mapping iz Entity u DTO:**
```csharp
private static LiveAqiResponse MapToLiveResponse(AirQualityRecord record)
{
    var aqi = record.AqiValue ?? 0;
    var (category, color, message) = GetAqiInfo(aqi);  // Helper metoda
    var measurements = BuildMeasurements(record);

    return new LiveAqiResponse(
        City: record.City.ToDisplayName(),
        OverallAqi: aqi,
        AqiCategory: category,
        Color: color,
        HealthMessage: message,
        Timestamp: record.Timestamp,
        Measurements: measurements,
        DominantPollutant: record.DominantPollutant ?? "Unknown"
    );
}
```

**GetAqiInfo() - AQI kategorije:**
```csharp
private static (string Category, string Color, string Message) GetAqiInfo(int aqi)
{
    return aqi switch
    {
        <= 50 => ("Good", "#00E400", "Air quality is satisfactory..."),
        <= 100 => ("Moderate", "#FFFF00", "Air quality is acceptable..."),
        <= 150 => ("Unhealthy for Sensitive Groups", "#FF7E00", "Members of sensitive groups may experience health effects..."),
        <= 200 => ("Unhealthy", "#FF0000", "Everyone may begin to experience health effects..."),
        <= 300 => ("Very Unhealthy", "#8F3F97", "Health alert: everyone may experience more serious health effects..."),
        _ => ("Hazardous", "#7E0023", "Health warnings of emergency conditions...")
    };
}
```

---

#### **H) Middleware - ExceptionHandlingMiddleware**

**Šta radi:**
- Hvata sve neobrađene exceptione
- Konvertuje ih u standardizovane HTTP responses
- Logira errore

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);  // Pozovi next middleware
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception occurred");
        await HandleExceptionAsync(context, ex);
    }
}

private async Task HandleExceptionAsync(HttpContext context, Exception exception)
{
    var (statusCode, title) = exception switch
    {
        ArgumentException => (400, "Bad Request"),
        KeyNotFoundException => (404, "Not Found"),
        UnauthorizedAccessException => (401, "Unauthorized"),
        TimeoutException => (408, "Request Timeout"),
        _ => (500, "Internal Server Error")
    };

    context.Response.StatusCode = (int)statusCode;
    context.Response.ContentType = "application/problem+json";

    var problemDetails = new
    {
        title,
        status = (int)statusCode,
        detail = exception.Message,
        traceId = context.TraceIdentifier,
        timestamp = TimeZoneHelper.GetSarajevoTime()
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
}
```

---

### 3.2 FRONTEND KOMPONENTE - DETALJNO

#### **A) API Client (api-client.ts)**

**ApiClient klasa:**
```typescript
class ApiClient {
  private baseUrl: string

  constructor() {
    const baseApiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
    this.baseUrl = `${baseApiUrl}/api/v1`
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`

    const response = await fetch(url, {
      credentials: 'include',
      cache: 'no-cache',
      headers: {
        'Content-Type': 'application/json',
        'Cache-Control': 'no-cache',
        ...options.headers,
      },
      ...options,
    })

    if (!response.ok) {
      throw new Error(`API Error: ${response.status}`)
    }

    const data = await response.json()
    return this.convertDates(data)  // Konvertuje ISO strings u Date objekte
  }

  async getComplete(cityId: string): Promise<CompleteAqiResponse> {
    return this.request<CompleteAqiResponse>(`/air-quality/${cityId}/complete`)
  }
}

export const apiClient = new ApiClient()
```

---

#### **B) Custom Hooks (`lib/hooks.ts`)**

**useLiveAqi Hook - SWR wrapper:**
```typescript
export function useLiveAqi(city: CityId | null) {
  const { data, error, isLoading, mutate } = useSWR<CompleteAqiResponse>(
    city ? `/air-quality/${city}/complete` : null,
    async () => {
      if (!city) return null
      return apiClient.getComplete(city)
    },
    {
      revalidateOnFocus: false,    // Ne refetchuj kad tab dobije focus
      revalidateOnReconnect: true, // Refetchuj kad se internet vrati
      dedupingInterval: 30000,     // Cache 30 sekundi
      shouldRetryOnError: true,
      errorRetryCount: 3,
    }
  )

  return { data: data?.liveData, error, isLoading, mutate }
}
```

**usePeriodicRefresh Hook - Auto refresh:**
```typescript
export function usePeriodicRefresh(intervalMs: number = 60000) {
  useEffect(() => {
    const subscription = airQualityObservable.subscribe(() => {
      mutate()  // Triggeruj re-fetch svih SWR hookova
    })

    const timer = setInterval(() => {
      airQualityObservable.notify()  // Notifikuj sve subscribere
    }, intervalMs)

    return () => {
      subscription.unsubscribe()
      clearInterval(timer)
    }
  }, [intervalMs])
}
```

---

#### **C) Main Page (page.tsx)**

**Flow:**

1. **Load user preferences iz localStorage**
```typescript
useEffect(() => {
  const storedPrimary = localStorage.getItem('bosniaair.primaryCity')
  if (isValidCityId(storedPrimary)) {
    setPrimaryCity(storedPrimary)
  } else {
    setPreferencesModalOpen(true)  // Prikaži modal za izbor grada
  }
}, [])
```

2. **Fetch data sa SWR**
```typescript
const { data: aqiData, error, isLoading } = useLiveAqi(primaryCity)
usePeriodicRefresh(60 * 1000)  // Refresuj svaki minut
```

3. **Render komponente**
```tsx
<LiveAqiPanel city={primaryCity} />
<Pollutants measurements={aqiData?.measurements} />
<ForecastTimeline city={primaryCity} />
<CitiesComparison />
```

---

#### **D) LiveAqiPanel Component**

Prikazuje trenutni AQI sa dinamičkom pozadinom:

```tsx
export default function LiveAqiPanel({ city }: { city: CityId }) {
  const { data, error, isLoading } = useLiveAqi(city)

  if (isLoading) return <Skeleton />
  if (error) return <ErrorCard />

  const backgroundColor = data.color  // Boja zavisi od AQI kategorije

  return (
    <div style={{ backgroundColor }} className="rounded-xl p-8">
      <h2 className="text-5xl font-bold">{data.overallAqi}</h2>
      <p className="text-2xl">{data.aqiCategory}</p>
      <p className="text-sm mt-4">{data.healthMessage}</p>
      <time className="text-xs opacity-80">
        {formatDistanceToNow(data.timestamp)} ago
      </time>
    </div>
  )
}
```

---

#### **E) Pollutants Component**

Grid sa karticama za svaki pollutant:

```tsx
export default function Pollutants({ measurements }: { measurements: Measurement[] }) {
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
      {measurements.map((m) => (
        <PollutantCard key={m.parameter} measurement={m} />
      ))}
    </div>
  )
}

function PollutantCard({ measurement }: { measurement: Measurement }) {
  const { parameter, value, unit } = measurement
  const info = getPollutantInfo(parameter)  // Metadata (name, icon, limits)

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg p-4">
      <div className="flex items-center gap-2">
        <span className="text-2xl">{info.icon}</span>
        <span className="font-semibold">{info.name}</span>
      </div>
      <p className="text-3xl font-bold mt-2">{value.toFixed(1)}</p>
      <p className="text-sm text-gray-600">{unit}</p>
      <HealthIndicator value={value} limits={info.limits} />
    </div>
  )
}
```

---

### 3.3 DATA FLOW - KOMPLETAN DIJAGRAM

```
┌─────────────────────────────────────────────────────────────────┐
│                    STARTUP (App Start)                          │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  Program.cs:                                                    │
│  1. Configure services (DI)                                     │
│  2. Setup database connection                                   │
│  3. Start AirQualityScheduler (Background Service)              │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  AirQualityScheduler.ExecuteAsync():                            │
│  ┌─────────────────────────────────────────────┐                │
│  │ LOOP every 10 minutes:                      │                │
│  │   For each city (Sarajevo, Tuzla, ...):    │                │
│  │     1. Call AirQualityService.RefreshCityAsync() │          │
│  └─────────────────┬───────────────────────────┘                │
└────────────────────┼────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│  AirQualityService.RefreshInternalAsync():                      │
│  1. FetchWaqiDataAsync(city)                                    │
│     └─> HTTP GET to api.waqi.info/feed/{stationId}/?token=XXX  │
│  2. Parse response (JSON → WaqiData DTO)                        │
│  3. Create AirQualityRecord entity                              │
│  4. repository.AddLatestAqi(record)                             │
│  5. IF forecast exists:                                         │
│       repository.UpdateAqiForecast(city, forecastJson)          │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  AirQualityRepository.AddLatestAqi():                           │
│  1. _context.AirQualityRecords.Add(record)                      │
│  2. _context.SaveChangesAsync()                                 │
│     └─> EF Core executes: INSERT INTO AirQualityRecords ...    │
└─────────────────────────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                     DATABASE                                    │
│  SQLite/PostgreSQL now has fresh data cached                   │
└─────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════

┌─────────────────────────────────────────────────────────────────┐
│                    USER REQUEST FLOW                            │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  User opens browser → https://bosniaair.vercel.app              │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  Frontend (Next.js page.tsx):                                   │
│  1. Load preferences from localStorage                          │
│  2. const { data } = useLiveAqi("Sarajevo")                     │
│  3. SWR calls: apiClient.getComplete("Sarajevo")                │
│     └─> fetch("http://api/air-quality/Sarajevo/complete")      │
└──────────────────┬──────────────────────────────────────────────┘
                   │ HTTP GET
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  Backend API:                                                   │
│  Request → Middleware Pipeline → Controller                    │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  AirQualityController.GetLiveAqiAndForecast():                  │
│  1. await _airQualityService.GetLiveAqiAndForecast(City.Sarajevo) │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  AirQualityService.GetLiveAqiAndForecast():                     │
│  1. var live = await GetLiveAqi(city)                           │
│     └─> repository.GetLatestAqi(city)                           │
│         └─> EF Core: SELECT * FROM AirQualityRecords WHERE ...  │
│  2. var forecast = await GetAqiForecast(city)                   │
│     └─> repository.GetForecastAsync(city)                       │
│  3. return new CompleteAqiResponse(live, forecast, timestamp)   │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  Controller → JSON Serialization → HTTP Response                │
│  Status: 200 OK                                                 │
│  Content-Type: application/json                                 │
│  Body: { liveData: {...}, forecastData: {...} }                 │
└──────────────────┬──────────────────────────────────────────────┘
                   │ HTTP Response
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  Frontend receives response:                                    │
│  1. apiClient.convertDates() - ISO strings → Date objects       │
│  2. SWR caches response                                         │
│  3. Component re-renders with new data                          │
└──────────────────┬──────────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│  UI Updates:                                                    │
│  • LiveAqiPanel shows current AQI                               │
│  • Pollutants grid displays measurements                        │
│  • ForecastTimeline shows predictions                           │
└─────────────────────────────────────────────────────────────────┘
```

--- 

Starting (7/8) *Pripremi C# i .NET learning guide*



## PART 4: KEY CONCEPTS & PATTERNS 🎓

### 4.1 DESIGN PATTERNS KORIŠTENI U PROJEKTU

#### **1. Repository Pattern**
```
Controller → Service → Repository → Database
```

**Zašto?**
- Odvaja business logiku od data access logike
- Lakše testiranje (možeš mock-ovati repository)
- Može se zamijeniti baza bez mijenjanja servisa

**Primjer:**
```csharp
// Interface - contract
public interface IAirQualityRepository
{
    Task<AirQualityRecord?> GetLatestAqi(City city, ...);
}

// Implementation - konkretna implementacija
public class AirQualityRepository : IAirQualityRepository
{
    public async Task<AirQualityRecord?> GetLatestAqi(City city, ...)
    {
        return await _context.AirQualityRecords.FirstOrDefaultAsync(...);
    }
}

// Service koristi interface, ne zna za EF Core
public class AirQualityService
{
    private readonly IAirQualityRepository _repository;  // Dependency injection
}
```

---

#### **2. Dependency Injection (DI) Pattern**

**Problem bez DI:**
```csharp
public class AirQualityService
{
    private readonly AirQualityRepository _repo = new AirQualityRepository();  // ❌ Tight coupling
}
```

**Rješenje sa DI:**
```csharp
// Registration (Program.cs)
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();

// Usage (AirQualityService)
public AirQualityService(IAirQualityRepository repository)  // ✅ ASP.NET injektuje
{
    _repository = repository;
}
```

**Benefiti:**
- Testability: možeš inject-ovati mock repository
- Flexibility: lako zamijeniš implementaciju
- Lifecycle management: ASP.NET upravlja objektima

---

#### **3. Service Layer Pattern**

```
Controller (HTTP layer)
    ↓
Service (Business logic)
    ↓
Repository (Data access)
```

**Separacija odgovornosti:**
- **Controller**: Prima HTTP request, validira input, vraća HTTP response
- **Service**: Business logika, transformacije, orchestracija
- **Repository**: Query database, CRUD operacije

---

#### **4. DTO (Data Transfer Object) Pattern**

```
Database Entity ≠ API Response
```

**Primjer:**
```csharp
// Entity (database model)
public class AirQualityRecord
{
    public int Id { get; set; }
    public City City { get; set; }  // Enum
    public int? AqiValue { get; set; }
    public double? Pm25 { get; set; }
    // ... 15+ properties
}

// DTO (API response)
public record LiveAqiResponse(
    string City,           // String, ne enum
    int OverallAqi,        // Uvijek postoji, ne nullable
    string AqiCategory,    // Calculated field
    string Color,          // Calculated field
    string HealthMessage,  // Calculated field
    IReadOnlyList<MeasurementDto> Measurements  // Nested DTOs
);
```

**Zašto DTOs?**
- Kontrola nad JSON strukturom
- Ne eksponuješ internal database strukture
- Možeš kombinovati podatke iz više entiteta
- API evolution bez breaking database

---

#### **5. Options Pattern (Configuration)**

```csharp
// appsettings.json
{
  "Aqicn": {
    "ApiUrl": "https://api.waqi.info",
    "ApiToken": "from-env-variable"
  }
}

// Configuration class
public class AqicnConfiguration
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
}

// Registration (Program.cs)
builder.Services.Configure<AqicnConfiguration>(
    builder.Configuration.GetSection("Aqicn"));

// Usage (Service)
public AirQualityService(IOptions<AqicnConfiguration> aqicnOptions)
{
    _apiToken = aqicnOptions.Value.ApiToken;
}
```

---

#### **6. Background Service Pattern**

```csharp
public class AirQualityScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoWork();
            await Task.Delay(interval, stoppingToken);
        }
    }
}

// Registration
builder.Services.AddHostedService<AirQualityScheduler>();
```

**ASP.NET automatski:**
- Startuje service kada aplikacija startuje
- Stopira service kada aplikacija gasi
- Provjerava `stoppingToken` za graceful shutdown

---

### 4.2 KLJUČNI .NET KONCEPTI ZA RAZGOVOR

#### **1. Async/Await & Task**

**Šta je Task?**
- Predstavlja asynchronous operaciju
- `Task` - operacija bez return value
- `Task<T>` - operacija sa return value T

**Zašto async?**
- Tidak blokira thread dok čeka I/O (database, HTTP)
- Server može procesirati druge requestove
- Bolji performance i scalability

**Primjer:**
```csharp
// ❌ Synchronous (blokira thread)
public LiveAqiResponse GetLiveAqi(City city)
{
    var data = _httpClient.GetAsync(url).Result;  // Blokira!
    return MapToResponse(data);
}

// ✅ Asynchronous (ne blokira)
public async Task<LiveAqiResponse> GetLiveAqi(City city)
{
    var data = await _httpClient.GetAsync(url);  // Async, thread slobodan
    return MapToResponse(data);
}
```

---

#### **2. LINQ (Language Integrated Query)**

**Extension methods na kolekcijama:**

```csharp
var latestRecord = await _context.AirQualityRecords
    .Where(r => r.City == city)              // Filter
    .OrderByDescending(r => r.Timestamp)     // Sort
    .FirstOrDefaultAsync();                  // Get first or null

// Ekvivalentan SQL:
// SELECT TOP 1 * FROM AirQualityRecords
// WHERE City = @city
// ORDER BY Timestamp DESC
```

**Projection:**
```csharp
var aqiValues = records
    .Select(r => r.AqiValue)                 // Project to AqiValue only
    .Where(aqi => aqi.HasValue)
    .Select(aqi => aqi.Value)
    .ToList();
```

---

#### **3. Entity Framework Core**

**DbContext lifecycle:**
```csharp
// ❌ Ne radi - Scoped service u Singleton
public class MyBackgroundService : BackgroundService
{
    private readonly AppDbContext _context;  // ❌ DbContext je Scoped!
}

// ✅ Radi - Kreira scope za svaki query
public class MyBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public async Task DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // ... koristi context
    }  // Scope se automatski dispose-uje
}
```

**Tracking vs No-Tracking:**
```csharp
// Tracking (default) - EF prati promjene
var record = await _context.Records.FirstOrDefaultAsync(...);
record.AqiValue = 100;
await _context.SaveChangesAsync();  // UPDATE automatski

// No-Tracking - Read-only, brže
var record = await _context.Records
    .AsNoTracking()  // EF ne prati promjene
    .FirstOrDefaultAsync(...);
```

---

#### **4. Middleware Pipeline**

**Request flow:**
```
HTTP Request
    ↓
1. Logging Middleware (Serilog)
    ↓
2. Exception Handling Middleware
    ↓
3. CORS Middleware
    ↓
4. Routing Middleware
    ↓
5. Controller
    ↓
4. Routing Middleware
    ↓
3. CORS Middleware
    ↓
2. Exception Handling Middleware
    ↓
1. Logging Middleware
    ↓
HTTP Response
```

**Custom middleware:**
```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);  // Pozovi next middleware
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }
}

// Registration
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

---

#### **5. Dependency Injection Scopes**

```csharp
// Transient - novi objekat svaki put
builder.Services.AddTransient<IMyService, MyService>();

// Scoped - jedan objekat po HTTP request-u
builder.Services.AddScoped<AppDbContext>();  // DbContext je uvijek Scoped!

// Singleton - jedan objekat za cijelu aplikaciju
builder.Services.AddSingleton<ICacheService, CacheService>();
```

**Pravila:**
- **Singleton NE MOŽE** imati Scoped dependency
- **Scoped NE MOŽE** imati Transient sa state-om koji treba da živi kroz request
- **DbContext UVIJEK Scoped** (jedan context po request-u)

---

### 4.3 FRONTEND - REACT KONCEPTI

#### **1. Custom Hooks**

```typescript
// Custom hook za data fetching
export function useLiveAqi(city: CityId | null) {
  const { data, error, isLoading, mutate } = useSWR(
    city ? `/air-quality/${city}/complete` : null,
    () => apiClient.getComplete(city!)
  )

  return { data: data?.liveData, error, isLoading, mutate }
}

// Usage u componentu
function MyComponent() {
  const { data, error, isLoading } = useLiveAqi("Sarajevo")
  
  if (isLoading) return <Spinner />
  if (error) return <Error />
  return <Display data={data} />
}
```

---

#### **2. SWR (stale-while-revalidate)**

**Šta radi SWR?**
1. Odmah vrati cached data (stale)
2. U pozadini fetch fresh data (revalidate)
3. Update UI kada fresh data stigne

**Config:**
```typescript
useSWR(key, fetcher, {
  revalidateOnFocus: false,    // Ne refetch kad tab dobije focus
  revalidateOnReconnect: true, // Refetch kad se internet vrati
  dedupingInterval: 30000,     // Ignoriši duplicate requestove 30s
  shouldRetryOnError: true,    // Retry na error
  errorRetryCount: 3,          // Max 3 retry-a
})
```

---

#### **3. useEffect & Lifecycle**

```typescript
useEffect(() => {
  // Mount: izvršava se kad component mount-uje
  console.log("Component mounted")
  
  const timer = setInterval(() => {
    console.log("Tick")
  }, 1000)

  // Cleanup: izvršava se kad component unmount-uje
  return () => {
    console.log("Component unmounting")
    clearInterval(timer)
  }
}, [])  // Dependency array: [] = izvršava se samo na mount
```

--- 

Starting (8/8) *Pripremi moguća pitanja i odgovore*



## PART 5: MOGUĆA PITANJA NA RAZGOVORU & ODGOVORI 🎤

### 5.1 ARHITEKTURA & DESIGN DECISIONS

#### **Q: Zašto si odabrao Clean Architecture / Repository pattern?**

**A:** Odabrao sam layered architecture sa Repository pattern-om iz nekoliko razloga:

1. **Separation of Concerns**: Controller se bavi samo HTTP requestima, Service ima business logiku, Repository data access. Svaki layer ima svoju odgovornost.

2. **Testability**: Mogu lako mock-ovati Repository i testirati Service bez database-a.

3. **Maintainability**: Ako treba promijeniti bazu (npr. iz SQLite u MongoDB), mijenjam samo Repository layer. Service ostaje isti.

4. **Industry Standard**: Ovo je standard u enterprise .NET aplikacijama, lako je za druge developere da razumiju.

**Follow-up pitanje: Šta bi radio drugačije?**
- Za veći projekat, dodao bih **CQRS** (Command Query Responsibility Segregation) - odvojeni modeli za read i write operacije
- Dodao bih **MediatR** library za handling commands i queries
- Implementirao **Unit of Work** pattern za transactional operations

---

#### **Q: Zašto koristiš caching u bazi umjesto in-memory cache (Redis)?**

**A:** Decision sam donio iz nekoliko razloga:

**Prednosti database caching:**
1. **Persistence**: Podaci ostaju nakon restarta aplikacije
2. **Simplicity**: Ne treba dodatna infrastruktura (Redis server)
3. **Historical Data**: Mogu vidjeti istoriju AQI vrijednosti (za live snapshots)
4. **Cost**: Besplatno, Redis košta (hosting)

**Kada bih koristio Redis:**
- Ako imam **visok traffic** (1000+ requests/sec)
- Ako trebam **distributed caching** (multiple API instances)
- Ako trebam **low-latency** (sub-millisecond responses)

**Current setup:**
- Background worker refreshuje svakih 10 minuta
- Database je brza dovoljno za current load
- Mogu dodati Redis kasnije ako treba (Repository pattern makes it easy)

---

#### **Q: Kako se nosi sa greškama i exception handling-om?**

**A:** Implementirao sam **multi-layer error handling**:

**1. Global Exception Middleware:**
```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```
- Hvata sve unhandled exceptions
- Vraća standardizovan JSON response (Problem Details RFC 7807)
- Logira error sa full stack trace

**2. Service-Level Exceptions:**
```csharp
if (cached is null)
    throw new DataUnavailableException(city, "live");
```
- Custom exceptions za business logic errors
- Meaningful error messages

**3. Controller-Level Try-Catch:**
```csharp
try {
    var response = await _service.GetLiveAqi(city);
    return Ok(response);
}
catch (DataUnavailableException ex) {
    return StatusCode(503, new { error = "Data unavailable" });
}
```
- Explicit handling za expected errors
- Appropriate HTTP status codes

**4. Frontend Error Handling:**
```typescript
const { data, error } = useLiveAqi(city)
if (error) return <ErrorCard message="Data unavailable" />
```

---

### 5.2 PERFORMANCE & SCALABILITY

#### **Q: Kako bi skalirao ovu aplikaciju za 100,000 usera?**

**A:** Trenutni setup može hendlati ~1,000 concurrent users. Za 100,000, uradio bih:

**1. Backend Scaling:**
```
Load Balancer
    ↓
API Instance 1 ──┐
API Instance 2 ──┼──→ PostgreSQL (Primary)
API Instance 3 ──┘        ↓
                    Read Replicas
```
- **Horizontal scaling**: Multiple API instances
- **Database read replicas**: Separate read/write
- **Connection pooling**: Reuse DB connections

**2. Caching Layer:**
```
Client → CDN → Load Balancer → Redis Cache → API → Database
```
- **Redis** za frequently accessed data
- **CDN** (Cloudflare) za static assets
- **Browser caching** sa proper Cache-Control headers

**3. Database Optimization:**
```csharp
// Add indexes
modelBuilder.Entity<AirQualityRecord>()
    .HasIndex(e => new { e.City, e.RecordType, e.Timestamp });

// Query optimization
.AsNoTracking()  // Read-only queries
.Take(100)       // Limit results
```

**4. Rate Limiting:**
```csharp
builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("fixed", opt => {
        opt.PermitLimit = 100;  // 100 requests
        opt.Window = TimeSpan.FromMinutes(1);  // per minute
    });
});
```

**5. Monitoring:**
- **Application Insights** za performance metrics
- **Serilog + ELK Stack** za centralized logging
- **Health checks** za uptime monitoring

---

#### **Q: Šta ako WAQI API padne ili je slow?**

**A:** Implementirao sam **resilience strategy**:

**1. HTTP Client Resilience:**
```csharp
builder.Services.AddHttpClient<AirQualityService>()
    .AddStandardResilienceHandler();  // Automatski retry, timeout, circuit breaker
```

**2. Circuit Breaker Pattern:**
- Ako WAQI API fails 5 puta, circuit se "otvara"
- API više ne pokušava pozivati WAQI (fast fail)
- Nakon 30 sekundi, pokušava opet (half-open state)

**3. Graceful Degradation:**
```csharp
try {
    forecast = await GetAqiForecast(city);
}
catch (DataUnavailableException) {
    forecast = new ForecastResponse(city, Array.Empty<ForecastDayDto>(), ...);
    // Return empty forecast instead of failing completely
}
```

**4. Stale Data Strategy:**
- Frontend prikazuje cached data sa timestamp-om
- User zna da su podaci možda outdated
- "Last updated: 2 hours ago" indicator

**5. Fallback to Alternative API:**
```csharp
try {
    return await FetchWaqiDataAsync(city);
}
catch {
    return await FetchOpenAQDataAsync(city);  // Alternative data source
}
```

---

### 5.3 SECURITY

#### **Q: Kako osiguravaš API od zloupotrebe?**

**A:** Implementirao sam nekoliko security measures:

**1. CORS Policy:**
```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("FrontendOnly", policy => {
        policy.WithOrigins("https://bosniaair.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```
- Samo frontend može pozivati API
- Blokirani cross-origin requestovi

**2. HTTPS Enforcement:**
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

**3. API Key Protection:**
```csharp
// WAQI API token u environment variables, ne u kodu
var token = Environment.GetEnvironmentVariable("WAQI_API_TOKEN");
```

**4. Input Validation:**
```csharp
[HttpGet("{city}/live")]
public async Task<ActionResult> GetLiveAqi([FromRoute] City city)  // Enum validation
{
    // ASP.NET automatski vraća 400 Bad Request ako city nije validan enum
}
```

**5. Rate Limiting** (za production):
```csharp
builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("api", opt => {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

**Šta bih dodao:**
- **Authentication** (JWT tokens) ako ima protected endpoints
- **API Versioning** (`/api/v1`, `/api/v2`)
- **Request signing** za kritične operacije
- **SQL Injection protection** (već pokriveno sa EF Core parametrizovanim queries)

---

### 5.4 DATABASE & DATA MANAGEMENT

#### **Q: Kako menedžiraš database schema promjene?**

**A:** Koristim **Entity Framework Core Migrations**:

**Development workflow:**
```bash
# 1. Promijenim C# entity
public class AirQualityRecord
{
    public string? NewProperty { get; set; }  // Nova kolona
}

# 2. Kreiram migraciju
dotnet ef migrations add AddNewProperty

# 3. EF generiše migration file
public partial class AddNewProperty : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NewProperty",
            table: "AirQualityRecords",
            nullable: true);
    }
}

# 4. Primijenim na bazu
dotnet ef database update
```

**Production deployment:**
1. Build migration u development-u
2. Commit migration files u Git
3. Na production startu, automatski:
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();  // Automatski apply pending migrations
}
```

**Rollback strategy:**
```bash
dotnet ef database update PreviousMigrationName  # Rollback
dotnet ef migrations remove  # Obriši zadnju migraciju
```

---

#### **Q: Zašto čuvaš sve live snapshots? Zašto ne overwrite-uješ?**

**A:** Decision sam donio da **čuvam historical data**:

**Benefiti:**
1. **Trend Analysis**: Mogu vidjeti kako se AQI mijenja tokom dana/sedmice
2. **Reporting**: Mogu praviti charts (average AQI per day, etc.)
3. **Debugging**: Mogu vidjeti kada je data bila zadnji put refreshovana
4. **Future Features**: Mogu dodati "Historical view" u UI

**Trade-off:**
- Database size raste (~1 KB po record × 6 cities × 144 snapshots/day = ~850 KB/day)
- Za godinu dana: ~310 MB (still manageable)

**Optimizacija:**
```csharp
// Cleanup job - briše snapshots starije od 30 dana
public async Task CleanupOldSnapshots()
{
    var cutoffDate = DateTime.UtcNow.AddDays(-30);
    var oldRecords = _context.AirQualityRecords
        .Where(r => r.RecordType == AirQualityRecordType.LiveSnapshot 
                    && r.Timestamp < cutoffDate);
    _context.RemoveRange(oldRecords);
    await _context.SaveChangesAsync();
}
```

**Za forecasts:**
- **Overwrite-ujem** jer samo trebam latest forecast
- Forecasts se mijenjaju, ne trebam history

---

### 5.5 FRONTEND & USER EXPERIENCE

#### **Q: Zašto Next.js umjesto Create React App?**

**A:** Next.js daje mnogo prednosti:

**1. Performance:**
- **Server-Side Rendering (SSR)**: Brži initial load
- **Automatic Code Splitting**: Samo potreban JS se učitava
- **Image Optimization**: `next/image` automatski optimizuje slike
- **Built-in Font Optimization**: Google Fonts se učitavaju optimalno

**2. Developer Experience:**
- **File-based Routing**: page.tsx = `/` route
- **API Routes**: Mogu dodati backend endpoint direktno u Next.js
- **TypeScript Support**: Out-of-the-box
- **Fast Refresh**: Instant feedback na promjene

**3. SEO:**
- **Metadata API**: Easy SEO optimization
- **Static Generation**: Better for search engines

**4. Production-Ready:**
- **Vercel Deployment**: One-click deploy
- **Edge Runtime**: Fast globally
- **Analytics**: Built-in

**Comparison:**
| Feature | Next.js | CRA |
|---------|---------|-----|
| SSR | ✅ | ❌ |
| Routing | ✅ Built-in | ❌ Need react-router |
| API Routes | ✅ | ❌ |
| Image Optimization | ✅ | ❌ |
| Production Build | ✅ Optimized | ⚠️ Basic |

---

#### **Q: Kako osiguravaš da UI ostane responsive i brz?**

**A:** Implementirao sam nekoliko optimizacija:

**1. Data Fetching sa SWR:**
```typescript
const { data } = useLiveAqi(city)  // SWR automatski cache-uje
```
- Instant display od cached data
- Background refresh
- Deduplication (multiple components, jedan request)

**2. Lazy Loading:**
```tsx
const HeavyComponent = dynamic(() => import('./HeavyComponent'), {
  loading: () => <Spinner />,
  ssr: false  // Ne renderuj na serveru
})
```

**3. Memoization:**
```typescript
const expensiveCalculation = useMemo(() => {
  return measurements.reduce((sum, m) => sum + m.value, 0)
}, [measurements])  // Recompute samo kada se measurements mijenjaju
```

**4. Virtual Scrolling** (za velike liste):
```tsx
import { useVirtualizer } from '@tanstack/react-virtual'
```

**5. Image Optimization:**
```tsx
<Image 
  src="/icon.png" 
  width={100} 
  height={100}
  loading="lazy"  // Lazy load
  quality={75}    // Compressed
/>
```

**6. Code Splitting:**
```tsx
// Automatski splituje na page level
app/
  page.tsx        → /_app.js (main bundle)
  about/page.tsx  → /about.js (separate bundle)
```

---

### 5.6 TESTING & QUALITY ASSURANCE

#### **Q: Kako testiram ovu aplikaciju?**

**A:** Iako trenutno nemam napisane testove (jer je bio brzi prototip), ovako bih ih implementirao:

**1. Unit Tests (xUnit + Moq):**
```csharp
public class AirQualityServiceTests
{
    [Fact]
    public async Task GetLiveAqi_ReturnsData_WhenCacheExists()
    {
        // Arrange
        var mockRepo = new Mock<IAirQualityRepository>();
        mockRepo.Setup(r => r.GetLatestAqi(City.Sarajevo, default))
                .ReturnsAsync(new AirQualityRecord { AqiValue = 85 });
        
        var service = new AirQualityService(mockRepo.Object, ...);
        
        // Act
        var result = await service.GetLiveAqi(City.Sarajevo);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(85, result.OverallAqi);
    }
}
```

**2. Integration Tests:**
```csharp
public class AirQualityControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetLiveAqi_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/api/v1/air-quality/Sarajevo/live");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sarajevo", content);
    }
}
```

**3. Frontend Tests (Jest + React Testing Library):**
```typescript
test('displays AQI value', async () => {
  render(<LiveAqiPanel city="Sarajevo" />)
  
  await waitFor(() => {
    expect(screen.getByText(/85/)).toBeInTheDocument()
  })
})
```

**4. E2E Tests (Playwright):**
```typescript
test('user can select city and see data', async ({ page }) => {
  await page.goto('http://localhost:3000')
  await page.click('text=Sarajevo')
  await expect(page.locator('.aqi-value')).toBeVisible()
})
```

**Test Coverage Target:**
- Unit Tests: 80%+ coverage za business logika
- Integration Tests: All API endpoints
- E2E Tests: Critical user flows

---

### 5.7 DEPLOYMENT & DEVOPS

#### **Q: Kako deployu-ješ aplikaciju?**

**A:** Imam **three-tier deployment**:

**1. Frontend (Vercel):**
```
Git Push → GitHub → Vercel Build → Deploy
```
- Automatski deploy na svaki push
- Preview deployment za pull requestove
- Edge CDN globally distributed
- Environment variables u Vercel dashboard

**2. Backend (Railway):**
```
Git Push → GitHub → Railway Build → Docker Container → Deploy
```
- Dockerfile definše build process
- Automatski environment variables iz Railway
- Health checks za uptime monitoring
- Auto-restart on failure

**3. Database (Supabase):**
- PostgreSQL hosted database
- Connection string u environment variable
- Automatski backups
- Read replicas za scaling

**Environment Variables:**
```env
# Backend
WAQI_API_TOKEN=secret_token_here
DATABASE_URL=postgresql://...
FRONTEND_ORIGIN=https://bosniaair.vercel.app

# Frontend
NEXT_PUBLIC_API_URL=https://api.railway.app
```

**CI/CD Pipeline:**
```yaml
# .github/workflows/deploy.yml
name: Deploy
on:
  push:
    branches: [main]

jobs:
  deploy-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Run tests
        run: dotnet test
      - name: Deploy to Railway
        run: railway up

  deploy-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Deploy to Vercel
        run: vercel --prod
```

---

#### **Q: Kako monitoriš aplikaciju u production-u?**

**A:** Implementirao sam **multi-layer monitoring**:

**1. Application Logging (Serilog):**
```csharp
_logger.LogInformation("Data refresh cycle completed");
_logger.LogError(ex, "Failed to fetch WAQI data for {City}", city);
```
- Structured logging (JSON format)
- Different log levels (Info, Warning, Error)
- Console output (Railway logs)

**2. Health Checks:**
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                duration = x.Value.Duration
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});
```

**3. Uptime Monitoring (UptimeRobot):**
- Ping `/health` endpoint svakih 5 minuta
- Email alert ako API ne odgovori
- Status page za users

**4. Error Tracking (Sentry - za production):**
```csharp
builder.Services.AddSentry(options =>
{
    options.Dsn = "https://...@sentry.io/...";
    options.TracesSampleRate = 1.0;
});
```
- Automatski hvata exceptione
- Stack traces sa source maps
- User context (IP, browser, etc.)

**5. Analytics:**
- Vercel Analytics za frontend metrics
- Custom events tracking:
```typescript
trackEvent('city_selected', { city: 'Sarajevo' })
trackEvent('api_error', { endpoint: '/live', status: 503 })
```

---

### 5.8 CODE QUALITY & BEST PRACTICES

#### **Q: Kako osiguravaš konzistentnost koda?**

**A:** Implementirao sam nekoliko práctica:

**1. Code Style:**
```json
// .editorconfig
[*.cs]
indent_style = space
indent_size = 4
csharp_new_line_before_open_brace = all

[*.{ts,tsx}]
indent_style = space
indent_size = 2
```

**2. Linting:**
```bash
# Backend
dotnet format  # Automatski format C# koda

# Frontend
npm run lint   # ESLint za TypeScript
npm run format # Prettier za formatting
```

**3. Code Review:**
- Pull requests za sve promjene
- Required reviews prije merge-a
- Automated checks (tests, linting)

**4. Documentation:**
```csharp
/// <summary>
/// Retrieves the most recent live snapshot for a city.
/// </summary>
/// <param name="city">The city to get data for</param>
/// <returns>Live AQI data or null if unavailable</returns>
public async Task<AirQualityRecord?> GetLatestAqi(City city, ...)
{
    // Implementation
}
```

**5. Git Workflow:**
```
main (production)
  ↑
  merge via PR
  ↑
feature/add-new-city (development)
```
- Feature branches za novi features
- Semantic commit messages: `feat: Add Banja Luka city`
- Conventional Commits standard

---

## PART 6: KAKO PREZENTOVATI PROJEKAT 🎬

### 6.1 PREZENTACIONA STRUKTURA (10-15 min)

#### **1. Uvod (2 min)**
"Dobar dan! Hvala što ste mi dali priliku da prezentuje moje rješenje. Napravio sam **BosniaAir** - aplikaciju za praćenje kvalitete vazduha u realnom vremenu za gradove u Bosni i Hercegovini.

Aplikacija je **full-stack solution** sa .NET 8 backend-om i Next.js frontend-om, deployed na production i trenutno dostupna na bosniaair.vercel.app."

---

#### **2. Demo (3 min)**
**Otvori live app i prikaži:**
1. **Home page** - prikaži trenutni AQI za Sarajevo
2. **City selection** - promijeni grad (Tuzla)
3. **Pollutants** - prikaži detalje zagađivača
4. **Forecast** - prikaži prognozu
5. **Responsive** - otvori na mobitelu (Chrome DevTools)

**Dok prikazuješ, objasni:**
- "Ovdje korisnik vidi current AQI koji je označen bojom prema EPA standardima"
- "Podaci se automatski refreshuju svaki minut koristeći SWR"
- "Aplikacija je fully responsive, radi na svim uređajima"

---

#### **3. Arhitektura (4 min)**
**Prikaži dijagram (možeš nacrtati na papiru ili whiteboard):**

```
User → Frontend (Vercel) → Backend API (Railway) → Database (Supabase)
                                    ↑
                          Background Worker
                                    ↑
                              WAQI API
```

**Objasni flow:**
1. "Background worker refreshuje podatke svakih 10 minuta sa WAQI API-ja"
2. "Podaci se cache-uju u PostgreSQL bazi"
3. "Kada korisnik otvori app, frontend poziva moj backend API"
4. "Backend vraća cached podatke, ne poziva external API na svaki request"
5. "Ovo smanjuje latency i štiti od rate limiting-a"

---

#### **4. Tehnički Detalji (4 min)**

**Backend:**
- ".NET 8 sa ASP.NET Core za REST API"
- "Entity Framework Core za database access"
- "Repository pattern za separation of concerns"
- "Background service za automatic data refresh"
- "Global exception handling middleware"

**Frontend:**
- "Next.js 14 sa TypeScript za type safety"
- "SWR za data fetching i caching"
- "Tailwind CSS za styling"
- "Responsive design sa mobile-first approach"

**Database:**
- "PostgreSQL u production-u, SQLite u development-u"
- "EF Core migrations za schema management"
- "Indexes za optimizovane queries"

---

#### **5. Challenges & Solutions (2 min)**

**Challenge 1: Timezone handling**
- "WAQI API vraća podatke u UTC-u"
- "Solution: Kreirao TimeZoneHelper klasu koja konvertuje sve u sarajevsku zonu (CET/CEST)"

**Challenge 2: Data unavailability**
- "Ponekad WAQI API nema forecast za sve gradove"
- "Solution: Graceful degradation - prikazujem live data čak i ako forecasta nema"

**Challenge 3: API rate limiting**
- "WAQI ima rate limit od 1000 requestova dnevno"
- "Solution: Background worker + caching - reduciram pozive na 144/dan"

---

#### **6. Zaključak (1 min)**
"To bi bilo sve! Projekat je potpuno funkcionalan i deploy-ovan na production. Spreman sam odgovoriti na bilo kakva pitanja."

---

### 6.2 TIPS ZA PREZENTACIJU

#### **DO:**
✅ **Pripremi demo prije razgovora** - testiraj da sve radi
✅ **Imaj backup** - screenshots ako internet ne radi
✅ **Budi entuzijastan** - pokazi da te zanima projekat
✅ **Objasni decisions** - zašto si izabrao X umjesto Y
✅ **Priznaj što ne znaš** - bolje nego lagati
✅ **Pitaj feedforward** - "Šta biste vi uradili drugačije?"

#### **DON'T:**
❌ **Ne kritiziraj svoj kod** - "Ovo je užasno napisano..."
❌ **Ne pokazuj nervozu** - speak slowly and clearly
❌ **Ne preskači pitanja** - ako ne znaš, reci "Nisam siguran, ali mislim..."
❌ **Ne pravi izgovore** - "Nije bilo vremena za..."

---

### 6.3 MOGUĆA TEHNIČKA PITANJA (QUICK REFERENCE)

| Pitanje | Ključni Koncept |
|---------|-----------------|
| "Objasni kako radi DI?" | Constructor injection, scoped lifecycle |
| "Šta je async/await?" | Non-blocking I/O, Task<T> |
| "Zašto Repository pattern?" | Separation of concerns, testability |
| "Kako EF Core radi?" | ORM, LINQ to SQL, tracking vs no-tracking |
| "Šta je middleware?" | Request pipeline, chain of responsibility |
| "Kako testirate kod?" | Unit tests (xUnit), mocking (Moq) |
| "Kako deploy-ujete?" | CI/CD, Docker, environment variables |
| "Kako scaling?" | Horizontal scaling, caching, read replicas |

---

### 6.4 PSYCHOLOGICAL PREP

**Remember:**
1. **Oni znaju da si učio sa AI** - to je OK! Focus on razumijevanje, ne memorisanje.
2. **Ne moraš znati sve** - "Nisam radio sa tim, ali mogu brzo naučiti"
3. **Ask questions back** - "Kako vi riješavate X u vašem projektu?"
4. **Show enthusiasm** - "Jako mi se svidio ovaj zadatak, naučio sam puno!"
5. **Be honest** - Ako ne znaš, reci "Ne znam, ali ovako bih pristupio..."

---

## PART 7: BRZI REFERENCE SHEET 📝

### KLJUČNE STVARI ZA ZAPAMTITI

#### **BACKEND STACK:**
- .NET 8 (LTS verzija, najnovija)
- ASP.NET Core (web framework)
- Entity Framework Core (ORM)
- Serilog (logging)
- PostgreSQL/SQLite (database)

#### **FRONTEND STACK:**
- Next.js 14 (React framework)
- TypeScript (type safety)
- SWR (data fetching)
- Tailwind CSS (styling)

#### **ARHITEKTURA:**
```
Controller → Service → Repository → Database
                ↑
         Background Worker
                ↑
            WAQI API
```

#### **KEY CONCEPTS:**
1. **Dependency Injection** - Services registrovani u Program.cs
2. **Async/Await** - Non-blocking operations
3. **Repository Pattern** - Separation of data access
4. **DTOs** - Database entities ≠ API responses
5. **Middleware** - Global error handling
6. **Background Service** - Automatic data refresh
7. **Entity Framework** - Migracije, LINQ, tracking

#### **DATA FLOW:**
1. Background worker poziva WAQI API (svakih 10 min)
2. Parsira response → sprema u database
3. User otvara app → poziva backend API
4. Backend čita iz database (ne poziva WAQI!)
5. Vraća JSON → frontend prikazuje

--- 

Completed (8/8) *Pripremi moguća pitanja i odgovore*



## PART 8: FINALNE NAPOMENE I SAVJETI 🎯

### 8.1 ŠTA UČITI PRIJE RAZGOVORA

#### **Priority 1 (MUST KNOW):**
1. **Dependency Injection** - kako radi, lifecycle (Scoped, Singleton, Transient)
2. **Async/Await** - zašto koristimo, kako radi Task
3. **Repository Pattern** - zašto, kako
4. **Entity Framework osnove** - DbContext, migrations, LINQ
5. **Middleware Pipeline** - redoslijed izvršavanja
6. **Data Flow** - od WAQI API-ja do browser-a

#### **Priority 2 (GOOD TO KNOW):**
1. **HTTP Status Codes** - 200, 400, 404, 500, 503
2. **REST API principles** - GET, POST, resource naming
3. **JSON Serialization** - camelCase, enums as strings
4. **CORS** - zašto ga trebamo
5. **Environment Variables** - gdje se čuvaju secreti

#### **Priority 3 (NICE TO HAVE):**
1. **Design Patterns** - Service Layer, DTO, Options Pattern
2. **Performance optimization** - caching, indexes, AsNoTracking
3. **Error handling** - middleware, try-catch, custom exceptions
4. **Testing** - unit tests, integration tests (theory only)

---

### 8.2 HOW TO LEARN FAST

#### **1. Official Microsoft Docs (2-3h)**
Pročitaj ove stranice:
- [ASP.NET Core Fundamentals](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/)
- [Dependency Injection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

#### **2. YouTube Tutorials (3-4h)**
Pogledaj:
- "ASP.NET Core Crash Course" - Nick Chapsas
- "Entity Framework Core Tutorial" - IAmTimCorey
- "Dependency Injection Explained" - CodeOpinion

#### **3. Practice (1-2h)**
Napravi mali projekat:
```csharp
// API koji vraća listu korisnika iz baze
// Implementiraj: Controller → Service → Repository → Database
// To će ti učvrstiti sve koncepte
```

#### **4. Review Your Code (1h)**
Pročitaj ponovno svoj kod sa razumijevanjem:
- Program.cs - line by line
- AirQualityService - svaku metodu
- AirQualityController - svaki endpoint

---

### 8.3 DAN PRIJE RAZGOVORA

#### **Morning (Jutro):**
✅ Testiraj da live app radi (bosniaair.vercel.app)
✅ Pripremi backup screenshots
✅ Pročitaj ovaj dokument ponovo (2h)

#### **Afternoon (Popodne):**
✅ Napravi mock prezentaciju (sam sebi objasni projekat)
✅ Zapiši na papir 5 ključnih architectural decisions
✅ Pripremi 3 pitanja za njih ("Kako vi riješavate X?")

#### **Evening (Veče):**
✅ Relaksuj! Ne uči nova ne više.
✅ Pregledaj samo QUICK REFERENCE (Part 7)
✅ Rano spavaj

---

### 8.4 TOKOM RAZGOVORA

#### **Opening (Početak):**
1. Predstavi se profesionalno
2. Zahvali što su ti dali zadatak
3. Reci da si spreman za demo

#### **During Demo (Tokom demonstracije):**
1. **Screen share** - testiraj audio/video prije
2. **Talk while showing** - ne puštaj silence-e
3. **Highlight key features** - "Ovo je posebno zanimljivo jer..."
4. **Show enthusiasm** - "Jako mi se svidjelo raditi na ovom dijelu"

#### **Answering Questions:**
1. **Pause before answering** - uzmi 2-3 sekunde da razmisliš
2. **Structure your answer**:
   - "Dobro pitanje! Uradio sam X iz Y razloga..."
   - "Konkretno, implementacija je..."
   - "Trade-off je bio..."

3. **If you don't know**:
   ❌ NE: "Nemam pojma"
   ✅ DA: "Nisam siguran, ali mislim da bi pristup bio..."
   ✅ DA: "Nisam radio sa tim, ali čuo sam da se koristi za..."

4. **Ask clarifying questions**:
   - "Možete li mi dati više konteksta?"
   - "Da li mislite na production scenario ili development?"

#### **Technical Discussion:**
- **Draw diagrams** - ako razgovarate uživo, nacrtaj na papir
- **Use concrete examples** - "Na primjer, kada user klikne na Sarajevo..."
- **Admit trade-offs** - "Odabrao sam X, ali Y bi bio bolji za Z scenario"

#### **Closing (Završetak):**
1. Pitaj feedforward: "Šta biste vi uradili drugačije?"
2. Pitaj o next steps: "Kakav je dalje proces?"
3. Zahvali opet
4. Follow-up email: "Hvala što ste odvojili vrijeme..."

---

### 8.5 RED FLAGS & HOW TO HANDLE

#### **Red Flag 1: "Zašto nemaš testove?"**
**Answer:** "Fokusirao sam se na funkcionalnost i deployment za demonstration. U production scenariju, definitivno bih implementirao:
- Unit tests za Service layer
- Integration tests za API endpoints
- E2E tests za kritične user flow-ove
Mogu li vam pokazati kako bih strukturirao test za AirQualityService?"

#### **Red Flag 2: "Zašto AI-generated comments?"**
**Answer:** "Comments sam generisao sa AI toolom jer je to industry best practice. Međutim, pročitao sam svaki comment i razumijem šta kod radi. Mogu li vam walk-through any component?"

#### **Red Flag 3: "Zašto nemaš authentication?"**
**Answer:** "Za ovaj use case, podaci su javni i ne trebaju authentication. Ali u production scenariju sa user accounts, implementirao bih JWT authentication sa:
- Login/Register endpoints
- Token-based auth
- Refresh tokens
Želite li da elaboriram arhitekturu za to?"

#### **Red Flag 4: "Zašto nije optimizovano za 100k usera?"**
**Answer:** "Pravio sam MVP za demonstration. Za 100k usera, uradio bih:
- Horizontal scaling (multiple instances)
- Redis caching layer
- Database read replicas
- CDN za static assets
Mogu li vam skicirati tu arhitekturu?"

---

### 8.6 CONFIDENCE BOOSTERS

**Remember these facts:**
1. ✅ Tvoj projekat **RADI** u production-u
2. ✅ Ima **real value** - ljudi mogu koristiti app
3. ✅ Arhitektura je **industry standard**
4. ✅ Kod je **čitljiv i maintainable**
5. ✅ Deploy-ovan sa **proper CI/CD**

**They gave you this interview because:**
- Your project impressed them
- They see potential
- They want to hire, not reject

**You know more than you think:**
- You built a full-stack application
- You deployed to production
- You integrated external API
- You handled async operations
- You implemented caching strategy

---

### 8.7 FINAL CHECKLIST ✅

**24h Before:**
- [ ] Live app radi (testiran)
- [ ] Backup screenshots spremni
- [ ] Review ovog dokumenta
- [ ] Mock prezentacija urađena
- [ ] 3 pitanja za njih pripremljena
- [ ] Rano spavanje

**1h Before:**
- [ ] Internet connection stabilan
- [ ] Mic/camera testirani
- [ ] Screen share testiran
- [ ] Browser tabs pripremnieni (live app, GitHub)
- [ ] Papir i olovka za crtanje
- [ ] Voda/kafa pri ruci

**During:**
- [ ] Smilej! 😊
- [ ] Speak clearly i slowly
- [ ] Pause before answering
- [ ] Ask questions when unsure
- [ ] Show enthusiasm
- [ ] Take notes

**After:**
- [ ] Thank you email (24h later)
- [ ] Reflect na šta si mogao bolje
- [ ] Prepare for potential follow-up

---

## 🎉 ZAKLJUČAK

Elmire, sada imaš **POTPUNU pripremu** za razgovor!

**Što si naučio:**
✅ High-level arhitekturu sistema
✅ Svaki layer aplikacije (Controller, Service, Repository)
✅ Data flow od API-ja do browser-a
✅ .NET i C# ključne koncepte
✅ Entity Framework i database management
✅ Next.js i React frontend
✅ Design patterns i best practices
✅ Moguća pitanja i kako odgovoriti
✅ Kako prezentovati projekat

**Tvoj projekat je SOLIDAN!**
- Full-stack aplikacija koja RADI
- Industry-standard arhitektura
- Deployed u production
- Real value za korisnike

**Mental Framework za razgovor:**
1. **Relax** - znaju da si junior, to je OK
2. **Be honest** - ako ne znaš, priznaj i reci šta misliš
3. **Show enthusiasm** - energija je zarazna
4. **Ask questions** - pokazuje da razmišljaš

---

### ZADNJI SAVJET

**Ne pokušavaj biti perfect.** Pokušaj biti:
- **Honest** - priznaj šta ne znaš
- **Curious** - pitaj kako bi oni riješili
- **Enthusiastic** - pokaži da te zanima
- **Collaborative** - razgovor, ne ispit

Zapamti: **Oni žele da uspješ!** Dali su ti interview jer vide potencijal.

---

**SRETNO NA RAZGOVORU! 🚀**

P.S. Ako ima još pitanja prije razgovora, slobodno pitaj! Mogu dodatno elaborirati bilo koji dio.