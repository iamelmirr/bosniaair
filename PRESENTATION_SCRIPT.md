# 🎤 BOSNIAAIR PROJECT PRESENTATION - 15-20 MINUTES
## Detaljni Scenario za Intervju Prezentaciju

---

## 📋 STRUKTURA PREZENTACIJE (20 minuta)

1. **Uvod & Inspiracija** (2 min)
2. **Pregled Projekta** (2 min)
3. **Tehnički Stack & Arhitektura** (4 min)
4. **Data Flow & Kako Sve Funkcioniše** (5 min)
5. **Backend Deep Dive** (3 min)
6. **Frontend Deep Dive** (2 min)
7. **Buduće Funkcionalnosti & Skalabilnost** (2 min)

---

## 🎬 1. UVOD & INSPIRACIJA (2 minuta)

### **Opening Statement** (30 sekundi)
*"Dobar dan! Danas ću vam predstaviti BosniaAir - moju aplikaciju za real-time praćenje kvaliteta zraka u Bosni i Hercegovini. Ovo je full-stack projekat koji sam razvio koristeći Next.js za frontend i ASP.NET Core za backend."*

### **Kako je Nastao Projekat** (90 sekundi)

**Priča o početku:**
*"Sve je počelo kada sam istraživao javne API-je i naišao na WAQI - World Air Quality Index API. To je besplatan servis koji agregira podatke sa hiljada stanica širom svijeta. Shvatio sam da postoje AQI stanice u Bosni i Hercegovini, pa sam pomislio - zašto ne napraviti aplikaciju koja će prikazivati te podatke na user-friendly način?"*

**Evolucija ideje:**
*"Na početku sam htio napraviti jednostavnu web app samo za Sarajevo. Započeo sam sa prvim endpointom - controller metodom koja prima grad kao parametar. I tu mi je sinulo - ako već moram hardkodirati ID stanice za Sarajevo, zašto ne dodati i druge gradove? Tako sam odlučio da napravim aplikaciju koja pokriva cijelu Bosnu i Hercegovinu."*

**Skalabilnost od starta:**
*"Trenutno imam 6 gradova - Sarajevo, Tuzla, Zenica, Mostar, Travnik i Bihać. Ali arhitektura je dizajnirana tako da mogu lako dodati bilo koji grad u svijetu, pa čak i proširiti na cijelu Evropu ili globalno. To je bila ključna odluka - dizajnirati za skalabilnost od prvog dana."*

---

## 📊 2. PREGLED PROJEKTA (2 minuta)

### **Što Aplikacija Radi** (60 sekundi)

*"BosniaAir omogućava korisnicima da prate kvalitet zraka u realnom vremenu. Evo glavnih funkcionalnosti:"*

1. **Live AQI Data**
   - *"Prikazuje trenutni Air Quality Index - broj od 0 do 500 koji pokazuje koliko je zrak čist ili zagađen"*
   - *"Svaki grad ima trenutne izmjerene vrijednosti za sve polutante: PM2.5, PM10, Ozon, Azot dioksid, Ugljen monoksid i Sumporni dioksid"*

2. **7-Day Forecast**
   - *"Aplikacija pokazuje prognozu kvaliteta zraka za narednih 7 dana"*
   - *"Korisnici mogu planirati aktivnosti na otvorenom - trčanje, biciklizam - na osnovu prognoze"*

3. **Multi-City Comparison**
   - *"Postoji tabela koja poredi sve gradove istovremeno - možete vidjeti koji grad ima najbolji/najgori zrak"*

4. **Health Advice**
   - *"Na osnovu EPA standarda, aplikacija daje zdravstvene savjete - npr. 'Osjetljive grupe treba da ograniče boravak napolju'"*

5. **PWA Features**
   - *"Aplikacija je Progressive Web App - može se instalirati na telefon, radi offline, brza je kao native app"*

### **Tech Stack** (60 sekundi)

*"Projekat koristi moderne tehnologije:"*

**Frontend:**
- *"Next.js 15 sa React 19 - latest versions"*
- *"TypeScript za type safety"*
- *"TailwindCSS za styling - modern utility-first approach"*
- *"SWR library za data fetching i caching - to je React hook library od Vercel-a"*

**Backend:**
- *"ASP.NET Core 8 sa C# 12 - Microsoft's latest framework"*
- *"Entity Framework Core za database access - to je ORM"*
- *"SQLite za development, PostgreSQL za production"*
- *"Serilog za structured logging"*

**External API:**
- *"WAQI API - World Air Quality Index - besplatni tier sa 1000 requesta dnevno"*

**Deployment:**
- *"Frontend je deployovan na Vercel - automatski build i deploy sa git push"*
- *"Backend je na Railway - container-based hosting sa PostgreSQL database"*

---

## 🏗️ 3. TEHNIČKI STACK & ARHITEKTURA (4 minuta)

### **High-Level Architecture** (90 sekundi)

*"Hajde da pogledam arhitekturu sistema. Imam tri glavne komponente:"*

**1. WAQI API (External Service)**
```
https://api.waqi.info/feed/{stationId}/?token={apiToken}
```
- *"Ovo je eksterni servis koji vraća podatke o kvalitetu zraka"*
- *"Svaki grad ima svoj station ID - npr. Sarajevo je 10557, Tuzla 8739"*
- *"API vraća JSON sa trenutnim AQI vrijednostima i forecastom"*

**2. Backend (ASP.NET Core)**
- *"Moj backend je orchestrator - povlači podatke sa WAQI-a, obrađuje ih, cacheira u bazu"*
- *"Ima scheduled background worker koji automatski refreshuje podatke svake 10 minuta"*
- *"Eksponuje REST API endpointe koje frontend konzumira"*

**3. Frontend (Next.js)**
- *"Single Page Application koja prikazuje podatke kroz React komponente"*
- *"Koristi SWR za client-side caching - to znači da podaci ostaju u memoriji browsera"*
- *"Automatski refreshuje podatke svake minute dok je stranica otvorena"*

**Data Flow Diagram:**
```
┌──────────────┐    HTTP/REST    ┌──────────────┐    HTTP     ┌──────────────┐
│   Frontend   │ ←──────────────→ │   Backend    │ ←──────────→ │   WAQI API   │
│  (Next.js)   │   JSON/HTTPS    │  (ASP.NET)   │   JSON      │  (External)  │
└──────────────┘                 └──────────────┘             └──────────────┘
      ↓                                 ↓
 SWR Cache                        SQLite/PG DB
(In-Memory)                       (Persistent Cache)
```

### **Backend Architecture - Clean Architecture Pattern** (90 sekundi)

*"Backend je organizovan po Clean Architecture principima - separation of concerns. Hajde da prođem kroz slojeve:"*

**1. Controllers Layer - HTTP Handlers**
```
AirQualityController.cs
```
- *"Ovo je entry point - prima HTTP reqeste od frontenda"*
- *"Ima samo 3 endpointa: `/live`, `/forecast`, `/complete`"*
- *"Validira parametre, poziva servis, vraća odgovor"*
- *"Minimalna logika - samo orchestration"*

**2. Services Layer - Business Logic**
```
AirQualityService.cs - 226 linija
AirQualityScheduler.cs - Background worker
```
- *"Service je srce aplikacije - ovdje je business logic"*
- *"AirQualityService ima metode za fetching live i forecast podataka"*
- *"AirQualityScheduler je background worker koji radi 24/7 - refreshuje podatke svake 10 minuta"*
- *"Service NE zna kako se podaci čuvaju - to je posao Repository-ja"*

**3. Repositories Layer - Data Access**
```
AirQualityRepository.cs
```
- *"Repository pattern - abstrakcija iznad baze podataka"*
- *"Ima interfejs IAirQualityRepository i implementaciju"*
- *"Metode: GetLatestAqi, AddLatestAqi, UpdateAqiForecast, GetForecastAsync"*
- *"Koristi Entity Framework Core - ne pišem SQL ručno"*

**4. Utilities Layer - Helper Classes**
```
AirQualityMapper.cs - 298 linija
TimeZoneHelper.cs
```
- *"AirQualityMapper je NAJVAŽNIJI utility - ovdje se dešavaju sve transformacije"*
- *"Konvertuje WAQI JSON → Database Entity"*
- *"Konvertuje Database Entity → API Response DTO"*
- *"Parsira forecast podatke - grupira po danima, računa prosijeke"*
- *"Mapira AQI kategorije po EPA standardima (Good, Moderate, Unhealthy...)"*
- *"TimeZoneHelper konvertuje sve u sarajevsko vrijeme - WAQI vraća UTC"*

**5. Data Layer - Database Context**
```
AppDbContext.cs
AirQualityRecord.cs - Entity model
```
- *"EF Core DbContext - connection sa bazom"*
- *"AirQualityRecord je database entity - ima properties za sve polutante"*
- *"Indeksi za performance: composite index na (City, RecordType, Timestamp)"*

**6. DTOs - Data Transfer Objects**
```
LiveAqiResponse.cs
ForecastResponse.cs
CompleteAqiResponse.cs
WaqiApiDtos.cs
```
- *"DTOs su C# records - immutable data structures"*
- *"LiveAqiResponse je ono što frontend dobije - JSON sa svim podacima"*
- *"WaqiApiDtos su modeli za WAQI API response - deserijalizujem njihov JSON"*

**7. Enums - Type Safety**
```
City.cs - Sarajevo = 10557, Tuzla = 8739...
CityExtensions.cs - ToStationId(), ToDisplayName()
AirQualityRecordType.cs - LiveSnapshot | Forecast
```
- *"Enumi daju type safety - ne mogu poslati pogrešan grad"*
- *"Extension metode: City.Sarajevo.ToStationId() vraća '10557'"*
- *"RecordType enum - dva tipa: LiveSnapshot (trenutno stanje) i Forecast (prognoza)"*

---

## 🔄 4. DATA FLOW & KAKO SVE FUNKCIONIŠE (5 minuta)

### **Scenario 1: Background Worker - Automatic Refresh** (90 sekundi)

*"Hajde da pratimo podatke od početka. Backend ima background worker koji radi 24/7:"*

**Korak po korak:**

1. **Timer Tick - Svake 10 Minuta**
```csharp
AirQualityScheduler.ExecuteAsync()
    await Task.Delay(TimeSpan.FromMinutes(10));
```
- *"Scheduler se budi svake 10 minuta"*
- *"To je configurable - mogu promijeniti u appsettings.json"*

2. **Parallel City Refresh**
```csharp
var tasks = _cities.Select(city => RefreshCityAsync(city));
await Task.WhenAll(tasks);
```
- *"Refreshuje SVE gradove istovremeno - paralelno"*
- *"Ne čeka da Sarajevo završi prije nego što ode na Tuzlu - sve odjednom"*
- *"Ovo znači refresh traje ~2-3 sekunde umjesto 10-15 sekundi"*

3. **API Call to WAQI**
```csharp
GET https://api.waqi.info/feed/10557/?token=xxx
```
- *"Za svaki grad šalje HTTP request na WAQI API"*
- *"WAQI vraća JSON sa trenutnim AQI, polutantima, forecastom"*

4. **Data Transformation - Mapper Magic**
```csharp
var timestamp = AirQualityMapper.ParseTimestamp(waqiData.Time);
```
- *"WAQI vraća timestamp u UTC - konvertujem u Sarajevo vrijeme"*

```csharp
var record = AirQualityMapper.MapToEntity(city, waqiData, timestamp);
```
- *"Mapper uzima WAQI JSON i kreira AirQualityRecord entity"*
- *"To je database model sa svim poljima: PM2.5, PM10, O3..."*

5. **Database Save**
```csharp
await _repository.AddLatestAqi(record, cancellationToken);
```
- *"Snima novi record u bazu - live snapshot"*
- *"Svaki refresh kreira novi red u tabeli"*

6. **Forecast Processing**
```csharp
var forecastResponse = AirQualityMapper.BuildForecastResponse(...);
await _repository.UpdateAqiForecast(city, serialized, timestamp);
```
- *"Posebno procesira forecast - grupira po danima"*
- *"Snima kao JSON string u ForecastJson kolonu"*
- *"Svaki grad ima JEDAN forecast record - update, ne insert"*

**Rezultat:**
- *"Nakon 10 minuta svi gradovi imaju fresh podatke u bazi"*
- *"Baza je cache layer - frontend ne čeka na WAQI API"*

### **Scenario 2: User Opens App - Frontend Request** (120 sekundi)

*"Sada kada baza ima podatke, hajde da vidimo šta se dešava kad korisnik otvori aplikaciju:"*

**Korak po korak:**

1. **User Opens Page**
```typescript
// page.tsx
const { data, error, isLoading } = useLiveAqi('sarajevo');
```
- *"Komponenta poziva custom hook useLiveAqi"*
- *"Hook koristi SWR library - to je data fetching sa cachingom"*

2. **SWR Cache Check**
```typescript
cache['aqi-live-sarajevo'] = { data: ..., timestamp: Date.now() }
```
- *"SWR prvo gleda u svoj cache - JavaScript objekat u memoriji"*
- *"Ako nema podataka (cache miss) - ide na API"*
- *"Ako ima podataka - gleda koliko su stari"*

3. **Freshness Decision**
- **Fresh (< 2 sekunde):**
  - *"Vraća cached podatke ODMAH - nema API call"*
  - *"To je deduping interval - sprečava spam requesta"*
  
- **Stale (> 2 sekunde):**
  - *"Vraća stare podatke ODMAH (instant response)"*
  - *"ALI u background-u povlači nove podatke"*
  - *"Kad stignu novi - re-render sa fresh podacima"*
  - *"Ovo se zove 'Stale-While-Revalidate' pattern - korisnik vidi nešto odmah"*

4. **API Call to Backend**
```typescript
apiClient.getLive('sarajevo')
    → fetch('http://localhost:5000/api/v1/air-quality/sarajevo/live')
```
- *"Frontend šalje GET request na backend"*

5. **Backend Controller**
```csharp
[HttpGet("{city}/live")]
public async Task<ActionResult<LiveAqiResponse>> GetLiveAqi(City city) {
    var response = await _airQualityService.GetLiveAqi(city);
    return Ok(response);
}
```
- *"Controller prima request, poziva servis"*

6. **Service Fetch from Database**
```csharp
var cached = await _repository.GetLatestAqi(city, cancellationToken);
```
- *"Service traži najnoviji live snapshot iz baze"*
- *"SQL query: WHERE City='Sarajevo' AND RecordType='LiveSnapshot' ORDER BY Timestamp DESC LIMIT 1"*

7. **Entity to DTO Mapping**
```csharp
return AirQualityMapper.MapToLiveResponse(cached);
```
- *"Mapper konvertuje database entity u LiveAqiResponse DTO"*
- *"Dodaje AQI category (Good/Moderate/Unhealthy) na osnovu AQI vrijednosti"*
- *"Kreira measurements array - svaki polutant je measurement objekat"*

8. **JSON Response**
```json
{
  "city": "Sarajevo",
  "overallAqi": 45,
  "aqiCategory": "Good",
  "color": "#00E400",
  "healthMessage": "Air quality is satisfactory...",
  "measurements": [...],
  "dominantPollutant": "PM2.5"
}
```
- *"Backend vraća JSON"*

9. **Frontend Renders**
```tsx
<LiveAqiPanel city="sarajevo">
  <div className="text-6xl">{data.overallAqi}</div>
  <div style={{ color: data.color }}>{data.aqiCategory}</div>
</LiveAqiPanel>
```
- *"React komponente renderuju podatke"*
- *"Conditional styling - crvena boja za unhealthy, zelena za good"*

**Total Time:**
- *"First load (cache miss): ~100-200ms (database je brz)"*
- *"Subsequent loads (cache hit): ~5-10ms (instant from memory)"*

### **Scenario 3: Periodic Refresh - Observable Pattern** (90 sekundi)

*"Aplikacija automatski refreshuje podatke svake minute dok je stranica otvorena. Ovo je implementirano sa Observable pattern-om:"*

**Kako radi:**

1. **Observable Setup**
```typescript
// page.tsx
usePeriodicRefresh(60 * 1000); // 60 seconds
```
- *"Hook postavlja interval timer na 60 sekundi"*

2. **Timer Creates Observable**
```typescript
setInterval(() => {
    airQualityObservable.notify();
}, 60000);
```
- *"Svake minute observable šalje notifikaciju svim subscriberima"*

3. **All Hooks Subscribe**
```typescript
useEffect(() => {
    const unsubscribe = airQualityObservable.subscribe(() => {
        mutate(); // SWR revalidate
    });
    return unsubscribe;
}, []);
```
- *"Svaki useLiveAqi hook se subscribuje na observable"*
- *"Kad dođe notifikacija, poziva mutate() - SWR refresh"*

4. **Coordinated Refresh**
- *"JEDAN timer kontroliše SVE komponente"*
- *"Bez observable: svaka komponenta ima svoj timer - haos"*
- *"Sa observable: sinhronizovano - sve refreshuju istovremeno"*

**Prednost:**
- *"Resource efficient - jedan timer umjesto N timera"*
- *"Koordinisan - ne šalje 5 requesta odjednom"*
- *"Clean - lako kontrolisati interval (promijenim na jednom mjestu)"*

---

## 🔧 5. BACKEND DEEP DIVE (3 minuta)

### **Key Design Decisions** (60 sekundi)

**1. Repository Pattern - Zašto?**

*"Odlučio sam se za Repository pattern jer:"*
- *"Separation of concerns - Service ne zna za bazu, Repository ne zna za business logic"*
- *"Testability - Lako mock-ujem interface za unit testove"*
- *"Flexibility - Mogu promijeniti bazu (MongoDB, Cosmos) bez promjene servisa"*

```csharp
// Service code - clean, ne zna za EF Core
var data = await _repository.GetLatestAqi(city);

// Repository hides complexity
public async Task<AirQualityRecord?> GetLatestAqi(City city) {
    return await _context.AirQualityRecords
        .AsNoTracking() // Performance optimization
        .Where(r => r.City == city && r.RecordType == AirQualityRecordType.LiveSnapshot)
        .OrderByDescending(r => r.Timestamp)
        .FirstOrDefaultAsync();
}
```

**2. Mapper Pattern - Centralized Transformations**

*"Ranije sam imao mapping logiku raspoređenu kroz Service - 420 linija koda! Refaktorisao sam:"*
- *"Sve transformacije u jedan utility class - AirQualityMapper"*
- *"Service smanjio sa 420 na 226 linija (-46%!)"*
- *"Sada je Service clean - samo orchestration"*
- *"Mapper ima 298 linija - ali to je SAMO transformations"*

**3. Background Worker - Scheduled Tasks**

*"AirQualityScheduler inherits BackgroundService - ASP.NET Core ga automatski startuje:"*
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    while (!stoppingToken.IsCancellationRequested) {
        await RunRefreshCycle(stoppingToken);
        await Task.Delay(_interval, stoppingToken);
    }
}
```
- *"Infinite loop sa Task.Delay"*
- *"CancellationToken za graceful shutdown"*
- *"Parallel refresh sa Task.WhenAll"*

### **Database Schema** (60 sekundi)

*"Imam jednu glavnu tabelu - AirQualityRecords. Dual-purpose design:"*

**Schema:**
```sql
CREATE TABLE AirQualityRecords (
    Id INTEGER PRIMARY KEY,
    City TEXT NOT NULL,              -- "Sarajevo", "Tuzla"...
    RecordType TEXT NOT NULL,        -- "LiveSnapshot" ili "Forecast"
    StationId TEXT,                  -- "10557"
    Timestamp DATETIME NOT NULL,     -- Vrijeme mjerenja
    AqiValue INTEGER,                -- Overall AQI
    DominantPollutant TEXT,          -- "PM2.5", "O3"...
    Pm25 REAL,                       -- PM2.5 μg/m³
    Pm10 REAL,                       -- PM10 μg/m³
    O3 REAL,                         -- Ozone μg/m³
    No2 REAL,                        -- NO2 μg/m³
    Co REAL,                         -- CO mg/m³
    So2 REAL,                        -- SO2 μg/m³
    ForecastJson TEXT,               -- Forecast data (JSON)
    CreatedAt DATETIME,
    UpdatedAt DATETIME
);
```

**Indexes:**
```sql
-- Fast lookups by city and timestamp
CREATE INDEX IX_AirQuality_CityTypeTimestamp 
    ON AirQualityRecords (City, RecordType, Timestamp);

-- Only ONE forecast per city
CREATE UNIQUE INDEX UX_AirQuality_ForecastPerCity 
    ON AirQualityRecords (City, RecordType) 
    WHERE RecordType = 'Forecast';
```

**Design Decisions:**
- *"RecordType enum razlikuje live snapshots od forecast-a"*
- *"Live snapshots: novi record svake 10 minuta - history se čuva"*
- *"Forecast: UPDATE, ne INSERT - samo jedan po gradu"*
- *"ForecastJson kao TEXT kolona - JSON string sa forecast danima"*
- *"Nullable columns - ponekad WAQI nema sve polutante"*

**Why This Design?**
- *"Mogu raditi statistiku - imam historical data"*
- *"Lako pravim charts - SELECT za period vremena"*
- *"Indexes osiguravaju brze queries (5-20ms)"*

### **Error Handling Strategy** (60 sekundi)

*"Tri nivoa error handling-a:"*

**1. Controller Level - HTTP Status Codes**
```csharp
catch (DataUnavailableException ex) {
    _logger.LogWarning(ex, "Live data unavailable for {City}", city);
    return StatusCode(503, new { error = "Data unavailable" });
}
catch (Exception ex) {
    _logger.LogError(ex, "Failed to retrieve live data");
    return StatusCode(500, new { error = "Internal server error" });
}
```
- *"Specific exceptions → specific status codes"*
- *"503 Service Unavailable - temporary issue"*
- *"500 Internal Server Error - unexpected failure"*

**2. Middleware Level - Global Handler**
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
- *"Catches sve što prođe kroz Controller"*
- *"Safety net - ensure proper response format"*

**3. Logging - Serilog**
```csharp
_logger.LogInformation("Data refresh cycle started");
_logger.LogWarning("Forecast data unavailable for {City}", city);
_logger.LogError(ex, "WAQI API call failed");
```
- *"Structured logging - mogu tražiti po properties"*
- *"Production: logs idu u file ili cloud (Application Insights)"*

---

## 🎨 6. FRONTEND DEEP DIVE (2 minuta)

### **SWR Cache Strategy** (60 sekundi)

*"SWR je ključan za performance. Stale-While-Revalidate pattern:"*

**Kako radi:**
```typescript
const { data, error, isLoading } = useSWR(
    'aqi-live-sarajevo',           // Cache key
    () => apiClient.getLive('sarajevo'),  // Fetcher function
    {
        refreshInterval: 0,          // No automatic polling (koristim observable)
        revalidateOnFocus: true,     // Refresh kad user se vrati na tab
        revalidateOnMount: true,     // Fetch on component mount
        dedupingInterval: 2000,      // 2s - ignore duplicate requests
    }
);
```

**Timeline Example:**
```
T+0s:    User opens page
         → SWR cache MISS
         → Fetch API → 100ms → Response
         → Store in cache → Render

T+1s:    Component re-renders
         → SWR cache HIT + FRESH (< 2s)
         → Return cached → NO API CALL
         → Instant render (5ms)

T+3s:    Another component mounts
         → SWR cache HIT + STALE (> 2s)
         → Return stale → Instant render
         → Background fetch → Update cache → Re-render

T+60s:   Observable timer fires
         → mutate() called
         → Fetch fresh data → Update cache
```

**Benefits:**
- *"User UVIJEK vidi nešto odmah - no loading spinners"*
- *"Background revalidation - fresh data bez čekanja"*
- *"Deduping - sprečava spam requesta"*

### **Component Architecture** (60 sekundi)

*"React komponente su organizovane po funkcionalnosti:"*

**Main Components:**

1. **page.tsx** - Root Page
   - *"State management - preferredCity, modal visibility"*
   - *"Orchestracija - poziva sve child komponente"*
   - *"localStorage integration - čuva user preferences"*

2. **LiveAqiPanel.tsx** - Current AQI Display
   - *"Prikazuje veliki AQI broj sa bojom"*
   - *"Loading skeleton dok se učitava"*
   - *"Error state sa retry button"*
   - *"Health advice na osnovu kategorije"*

3. **ForecastTimeline.tsx** - 7-Day Forecast
   - *"Horizontal timeline sa forecast danima"*
   - *"Color-coded po AQI kategoriji"*
   - *"Shows PM2.5, PM10, O3 ranges"*

4. **Pollutants.tsx** - Pollutant Breakdown
   - *"Grid of cards - svaki polutant ima karticu"*
   - *"Tooltip sa objašnjenjem (npr. 'PM2.5 - Fine particulate matter')"*
   - *"Mobile-friendly - stacks vertically"*

5. **CitiesComparison.tsx** - Multi-City Table
   - *"Tabela sa svim gradovima"*
   - *"Sortable columns"*
   - *"Highlighting - worst/best air quality"*

**Custom Hooks:**
```typescript
useLiveAqi(cityId)      // Fetch live data with SWR + Observable
useComplete(cityId)     // Fetch live + forecast combined
usePeriodicRefresh(ms)  // Setup automatic refresh timer
useRefreshAll()         // Manual refresh trigger
```
- *"Reusable logic - ne ponavljam kod"*
- *"Separation of concerns - components ne znaju za SWR detalje"*

---

## 🚀 7. BUDUĆE FUNKCIONALNOSTI & SKALABILNOST (2 minuta)

### **Short-Term Features (1-3 Mjeseca)** (60 sekundi)

**1. Interactive Map View**
```
Map sa pinovima za svaku AQI stanicu
├── Leaflet.js ili Google Maps integration
├── Color-coded markers (zelena/žuta/crvena)
├── Click na marker → Popup sa live AQI
└── Zoom levels - prikazuj sve stanice ili samo major cities
```
- *"WAQI API ima geo coordinates za svaku stanicu"*
- *"Mogu prikazati SVE stanice u BiH, ne samo 6 gradova"*
- *"User experience: vizualno vidi gdje je zrak najgori"*

**2. Historical Data & Charts**
```
Analytics Dashboard
├── Line chart - AQI trend zadnjih 30 dana
├── Bar chart - Average AQI po gradu
├── Comparison chart - PM2.5 Sarajevo vs Tuzla
└── Export to CSV - download historical data
```
- *"Već imam podatke u bazi - svaki refresh snapshot"*
- *"SQL query: SELECT AVG(AqiValue) FROM Records WHERE City='Sarajevo' GROUP BY DATE(Timestamp)"*
- *"Charting library: Chart.js ili Recharts"*

**3. User Accounts & Notifications**
```
User Features
├── Register/Login (Auth0 ili Firebase)
├── Favorite cities - custom dashboard
├── Alert thresholds - "Notify me when Sarajevo AQI > 100"
├── Email/Push notifications
└── User preferences - dark mode, language
```
- *"Backend: Add Users table, JWT authentication"*
- *"Notifications: BackgroundService provjerava thresholds"*

**4. Health Recommendations Engine**
```
Advanced Health Advice
├── User profile - age, health conditions (asthma, etc.)
├── Activity planner - "Best time to run today: 6-8 AM (AQI 40)"
├── Sensitive groups - elderly, children, pregnant women
└── Integration with calendar - suggest outdoor activities
```

### **Long-Term Vision (6-12 Mjeseci)** (60 sekundi)

**1. Geographic Expansion**

*"Trenutno BiH, ali arhitektura podržava globalno:"*

**Europe Expansion:**
```
Dodaj gradove:
├── Balkan: Zagreb, Beograd, Skopje, Priština, Tirana
├── EU: Vienna, Budapest, Ljubljana, Podgorica
└── Total: 50+ gradova u regionu
```
- *"WAQI ima stanice širom Europe"*
- *"Samo dodati City enum values - sve ostalo radi"*

**Global Expansion:**
```
Worldwide Coverage
├── API route: /api/v1/air-quality/search?country=Bosnia
├── Database: Add Country table, City-Country relationship
├── Frontend: Country selector dropdown
└── Pagination: Top 100 cities worldwide
```

**2. Advanced Analytics & AI**

```
Machine Learning Features
├── AQI Prediction Model - ML model za prediction 48h unaprijed
├── Anomaly Detection - alert ako AQI nenormalno skoči
├── Seasonal Patterns - "Zimi u Sarajevu AQI prosječno 30% veći"
└── Recommendation System - "Based on your location & health, avoid outdoor activity today"
```
- *"Train model na historical data"*
- *"Python microservice sa TensorFlow/PyTorch"*
- *"Backend poziva ML service za predictions"*

**3. Mobile Native Apps**

```
Native Apps
├── React Native - reuse frontend components
├── iOS & Android
├── Push notifications native support
├── Offline mode - cache last known data
└── Background refresh - update data even when app closed
```

**4. Community Features**

```
Social Platform
├── User-submitted data - "I see smog in Sarajevo right now"
├── Comments & discussions - "Why is Zenica AQI so high?"
├── Photo uploads - visual evidence of air quality
├── Crowdsourced alerts - "Factory smoke reported near Tuzla"
└── Gamification - badges for reporting, streaks for checking daily
```

**5. Government & Enterprise Integration**

```
B2B Features
├── API for governments - integrate sa zvaničnim web-ovima
├── White-label solution - rebrand za druge zemlje
├── Enterprise dashboards - municipalities tracking
├── Compliance reporting - EU air quality directives
└── Premium tier - advanced analytics, custom alerts
```

### **Technical Scalability** (30 sekundi)

**Current vs Future Architecture:**

**Current:**
```
6 cities × 144 refreshes/day = 864 API calls/day
SQLite (single file) - dev only
Railway (single instance) - ~$5/month
```

**Future (100 cities, global):**
```
100 cities × 144 refreshes/day = 14,400 API calls/day
PostgreSQL (managed) + Redis cache layer
Multiple backend instances - load balancer
CDN for frontend - Cloudflare or AWS CloudFront
```

**Scaling Strategy:**
- *"Redis za caching - reduce database load"*
- *"Horizontal scaling - deploy multiple backend instances"*
- *"Database replication - read replicas za analytics"*
- *"Microservices - separate scheduler from API"*
- *"Queue system - RabbitMQ za async processing"*

---

## 🎯 CLOSING STATEMENT (30 sekundi)

*"Dakle, BosniaAir je full-stack aplikacija koja demonstrira moderne development practices:"*

- ✅ *"Clean architecture - separation of concerns, SOLID principles"*
- ✅ *"Scalable design - lako dodati nove gradove ili features"*
- ✅ *"Performance optimization - caching na svim nivoima"*
- ✅ *"User-centric - instant response, smooth experience"*
- ✅ *"Production-ready - error handling, logging, monitoring"*

*"Ovo je projekat koji može rasti - od 6 gradova do cijelog svijeta, od live data do advanced analytics i AI predictions. Arhitektura to podržava."*

*"Imam još puno ideja za razvoj, ali sam fokusiran na to da trenutna implementacija bude best practice primer - clean code, proper patterns, maintainability."*

*"Da li imate pitanja o bilo kojem aspektu projekta?"*

---

## 📝 PRIPREMA ZA PITANJA

### **Očekivana Pitanja & Odgovori**

**Q: "Zašto si izabrao ASP.NET Core umjesto Node.js/Express?"**

*A: "Odlučio sam se za ASP.NET Core iz nekoliko razloga:*
1. *Type safety - C# je strongly typed language, što sprečava mnoge errore u compile time*
2. *Performance - ASP.NET Core je jedan od najbržih web frameworka (TechEmpower benchmarks)*
3. *Ecosystem - Entity Framework Core, Dependency Injection built-in, odličan tooling*
4. *Career growth - želio sam naučiti enterprise-grade framework koji se koristi u velikim kompanijama*
5. *Cloud-ready - izvrsna integracija sa Azure, ali radi i na AWS, Google Cloud, Railway"*

**Q: "Kako handluješ WAQI API rate limits?"**

*A: "WAQI free tier ima limit od 1000 requesta dnevno. Trenutno imam:*
- *6 gradova × 144 requesta/dan (10 min interval) = 864 requesta*
- *To je ispod limita, ali blizu*

*Strategija za scaling:*
1. *Increase refresh interval na 15-20 minuta - manje requesta*
2. *Implementirati exponential backoff ako dobijem 429 error*
3. *Upgrade na paid tier ($9/month) - 100,000 requesta*
4. *Hybrid approach - WAQI za live data, vlastiti senzori za specific locations"*

**Q: "Šta bi uradio drugačije da počinješ ponovo?"**

*A: "Odličko pitanje! Nekoliko stvari:*
1. *Started sa TypeScript i na backendu - možda probao C# + TypeScript full-stack approach*
2. *Implementirao bi unit testove od starta - sada bih morao retroaktivno*
3. *Redis caching layer - umjesto samo database cache*
4. *Docker od početka - lakši development setup*
5. *CI/CD pipeline - automated testing i deployment sa GitHub Actions*

*Ali generalno, zadovoljan sam arhitekturom - clean, maintainable, scalable."*

**Q: "Kako testirate aplikaciju?"**

*A: "Trenutno testiram manualno - integration testing kroz Postman i browser testing. Planiram dodati:*

*Backend:*
- *xUnit test framework*
- *Mock repositories - testiram service logic izolovano*
- *Integration tests - testiram cijeli pipeline*

*Frontend:*
- *Jest + React Testing Library*
- *Component tests - user interactions*
- *E2E tests - Playwright ili Cypress*

*To je prioritet za production deployment."*

**Q: "Objasni zakaj si koristio C# records za DTOs?"**

*A: "C# records su perfect fit za DTOs:*
1. *Immutability by default - jednom kreiran, ne može se promijeniti*
2. *Concise syntax - 1 line umjesto 20 lines boilerplate*
3. *Value equality - dva objekta sa istim podacima su jednaki*
4. *Perfect for JSON serialization - minimalan ceremony*

*Primjer:*
```csharp
// Old way - class (30+ lines)
public class LiveAqiResponse {
    public string City { get; set; }
    // ... getters, setters, equals, hashcode
}

// New way - record (1 line)
public record LiveAqiResponse(string City, int OverallAqi, ...);
```

*Records su novi C# 9+ feature - pokazuje da koristim moderne language features."*

---

## ⏰ TIMING BREAKDOWN

- **Uvod & Inspiracija**: 2 min ✓
- **Pregled Projekta**: 2 min ✓
- **Arhitektura**: 4 min ✓
- **Data Flow**: 5 min ✓
- **Backend Deep Dive**: 3 min ✓
- **Frontend Deep Dive**: 2 min ✓
- **Future Features**: 2 min ✓
- **TOTAL**: 20 minuta ✓

---

## 💡 PREZENTACIJSKI TIPOVI

### **Delivery Tips**

1. **Tempo**
   - Govori polako - ne žuri
   - Prave pauze nakon svake sekcije
   - Daj intervjuerima vremena da procesiraju

2. **Body Language**
   - Održavaj eye contact
   - Koristi ruke za emphasize (npr. kad objašnjavaš flow)
   - Smiješi se - pokaži enthusiasm

3. **Technical Terminology**
   - Koristi proper termine - "dependency injection", "repository pattern"
   - ALI objasni ih jednostavno ako vidiš da nisu sigurni
   - Balance između impresije i jasnoće

4. **Interactivity**
   - Pitaj "Da li mogu pokazati kod?" i otvori fajl
   - Crtaj dijagrame na papiru ili whiteboard
   - Involvuj ih - "Da li želite da vidimo kako ovo radi live?"

5. **Confidence**
   - Govori odlučno - "Odlučio sam se za X jer Y"
   - Ako ne znaš nešto - "Ne znam tačno, ali mislim da bi moglo biti..."
   - Pokaži da si siguran u svoje odluke

### **Visual Aids**

Pripremi:
1. **Architecture Diagram** - nacrtaj unaprijed ili crtaj live
2. **Code Snippets** - otvori key files u VS Code
3. **Running App** - live demo (ali kao backup, ne glavni dio)
4. **Database Query** - pokaži kako izgleda data u SQLite

### **Energy Management**

- **Start strong** - confident uvod
- **Build excitement** - kada objašnjavaš future features, budi enthusiastic
- **End strong** - confident closing statement

---

## 🎤 PRACTICE SCRIPT

*Ovdje imaš exact words koje možeš practice-ovati:*

**Opening (30 sec):**
> "Dobar dan! Zovem se [ime], i danas ću vam predstaviti BosniaAir - aplikaciju koju sam razvio za real-time praćenje kvaliteta zraka u Bosni i Hercegovini. Ovo je full-stack projekat koji kombinuje Next.js frontend sa ASP.NET Core backendom. Prezentacija će trajati oko 20 minuta, i na kraju ću rado odgovoriti na sva pitanja."

**Transition (Backend → Frontend):**
> "To je backend strana. Hajde sada da vidimo kako frontend koristi ove podatke..."

**Transition (Current → Future):**
> "Trenutna implementacija je solidna i production-ready. Ali imam viziju za dalji razvoj..."

**Closing (30 sec):**
> "Dakle, BosniaAir demonstrira clean architecture, moderne development practices, i scalable design. Projekat može rasti od 6 gradova do globalnog sistema sa advanced analytics. Fokusirao sam se na to da current implementation bude maintainable i profesionalna. Hvala vam na pažnji. Da li imate pitanja?"

---

## ✅ FINAL CHECKLIST - DAN PRIJE INTERVJUA

- [ ] Prođi kroz script 2-3 puta naglas
- [ ] Nacrtaj architecture diagram na papiru - practice-uj crtanje
- [ ] Otvori projekat - pokreni backend i frontend, provjeri da sve radi
- [ ] Pripremi 3-4 code snippets koje možeš pokazati (Controller, Mapper, Hook)
- [ ] Pregledaj INTERVIEW_CHECKLIST.md - refresh memory
- [ ] Napiši 5 bullet points koje NE SMIJEŠ zaboraviti:
  1. Clean architecture & separation of concerns
  2. SWR caching strategy (stale-while-revalidate)
  3. Background worker za automatic refresh
  4. Scalability - od 6 gradova do globalnog
  5. Future features - map, analytics, ML

---

## 🎯 YOU GOT THIS!

**Ovo je tvoja priča. Projekat je quality work. Prezentuj sa confidence!**

**Remember:**
- Polako govori - clarity > speed
- Show enthusiasm - ovo je tvoj rad!
- Be honest - ako ne znaš, priznaj i objasni kako bi našao odgovor
- Engage them - involve ih u prezentaciju

**SRETNO U UTORAK! 🚀🔥**
