# 📘 BosniaAir - Complete Application Flow Documentation

> Kompletna tehnička dokumentacija svih flow-ova u aplikaciji - frontend, backend i njihova integracija

**Datum:** October 3, 2025  
**Projekat:** BosniaAir - Air Quality Monitoring  
**Stack:** Frontend (Next.js 14, React, TypeScript) + Backend (C# .NET 8, ASP.NET Core)

---

## 📑 Sadržaj

1. [Arhitektura Overview](#arhitektura-overview)
2. [Backend Flow](#backend-flow)
3. [Frontend Flow](#frontend-flow)
4. [Integration Flow](#integration-flow)
5. [Konkretni Scenariji](#konkretni-scenariji)
6. [Sve Funkcije i Njihova Uloga](#sve-funkcije-i-njihova-uloga)

---

## 🏗️ Arhitektura Overview

### **System Architecture Diagram**

```
┌─────────────────────────────────────────────────────────────────┐
│                         BROWSER                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              Next.js Frontend (Port 3000)                │   │
│  │                                                          │   │
│  │  Components → Hooks → API Client → Observable          │   │
│  │     ↓           ↓         ↓             ↓               │   │
│  │   React     useLiveAqi  fetch()    EventTarget         │   │
│  │   State     useComplete             Timer               │   │
│  │                  ↓                                       │   │
│  │             SWR Cache                                   │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                            ↕ HTTP Requests (CORS)
┌─────────────────────────────────────────────────────────────────┐
│                      .NET Backend (Port 5000)                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │         ASP.NET Core API                                 │   │
│  │                                                          │   │
│  │  Controllers → Services → Repositories → Database       │   │
│  │      ↓           ↓            ↓             ↓            │   │
│  │  HTTP Routes  Business   Data Access   SQLite/Postgres  │   │
│  │              Logic                                       │   │
│  │                  ↓                                       │   │
│  │         Background Service (Scheduler)                  │   │
│  │              Every 10 minutes                           │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                            ↕ HTTP Requests
┌─────────────────────────────────────────────────────────────────┐
│                    External WAQI API                            │
│         https://api.waqi.info/feed/{station}                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Backend Flow

### **Backend Architecture Layers**

```
HTTP Request → Controller → Service → Repository → Database
                                ↓
                        External API (WAQI)
                                ↓
                        Background Scheduler
```

---

### **1. Background Service - Automatski Refresh (Svake 10 Minuta)**

#### **AirQualityScheduler - Hosted Service**

**File:** `backend/src/BosniaAir.Api/Services/AirQualityScheduler.cs`

**Lifecycle:**
```
Application Start
    ↓
ExecuteAsync() metoda se pokreće
    ↓
Immediate first refresh
    ↓
Čeka 10 minuta
    ↓
Refresh ponovo
    ↓
Loop nastavlja dok aplikacija radi
```

**Kod Flow:**

```csharp
// 1️⃣ Background Service Starts
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Air quality scheduler starting...");
    
    // 2️⃣ Prvi refresh odmah
    await RunRefreshCycle(stoppingToken);

    // 3️⃣ Infinite loop
    while (!stoppingToken.IsCancellationRequested)
    {
        await Task.Delay(_interval, stoppingToken);  // Čeka 10 minuta
        await RunRefreshCycle(stoppingToken);        // Refresh ponovo
    }
}

// 4️⃣ Refresh svih gradova paralelno
private async Task RunRefreshCycle(CancellationToken cancellationToken)
{
    _logger.LogInformation("Data refresh cycle started");

    // Kreira Task za svaki grad
    var tasks = _cities.Select(city => RefreshCityAsync(city, cancellationToken)).ToList();
    
    // Čeka da SVI završe
    await Task.WhenAll(tasks);

    _logger.LogInformation("Data refresh cycle completed");
}

// 5️⃣ Refresh pojedinačnog grada
private async Task RefreshCityAsync(City city, CancellationToken cancellationToken)
{
    try
    {
        // Kreira novi scope (jer je Scheduler Singleton, a Service je Scoped)
        using var scope = _scopeFactory.CreateScope();
        var airQualityService = scope.ServiceProvider.GetRequiredService<IAirQualityService>();
        
        // Poziva service da refreshuje grad
        await airQualityService.RefreshCityAsync(city, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to refresh data for {City}", city);
        // Ne baca error - nastavlja sa drugim gradovima
    }
}
```

**Funkcije:**

| Funkcija | Šta Radi | Kada Se Poziva |
|----------|----------|----------------|
| `ExecuteAsync()` | Main loop - kontroliše timing | Aplikacija startuje |
| `RunRefreshCycle()` | Refreshuje SVE gradove paralelno | Svake 10 minuta + na startu |
| `RefreshCityAsync()` | Refreshuje jedan grad | Za svaki grad u listi |

---

### **2. API Controller - HTTP Endpoints**

**File:** `backend/src/BosniaAir.Api/Controllers/AirQualityController.cs`

**Endpoints:**

```csharp
// 1️⃣ GET /api/v1/air-quality/{city}/live
[HttpGet("{city}/live")]
public async Task<ActionResult<LiveAqiResponse>> GetLive(
    [FromRoute] City city,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Poziva service da vrati cached live data
        var response = await _airQualityService.GetLive(city, cancellationToken);
        return Ok(response);
    }
    catch (DataUnavailableException ex)
    {
        _logger.LogWarning(ex, "Live data unavailable for {City}", city);
        return StatusCode(503, new { error = "Data unavailable" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve live data for {City}", city);
        return StatusCode(500, new { error = "Internal server error" });
    }
}

// 2️⃣ GET /api/v1/air-quality/{city}/forecast
[HttpGet("{city}/forecast")]
public async Task<ActionResult<ForecastResponse>> GetForecast(
    [FromRoute] City city,
    CancellationToken cancellationToken = default)
{
    try
    {
        var response = await _airQualityService.GetForecast(city, cancellationToken);
        return Ok(response);
    }
    catch (DataUnavailableException ex)
    {
        return StatusCode(503, new { error = "Data unavailable" });
    }
}

// 3️⃣ GET /api/v1/air-quality/{city}/complete
[HttpGet("{city}/complete")]
public async Task<ActionResult<CompleteAqiResponse>> GetComplete(
    [FromRoute] City city,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Vraća live + forecast zajedno
        var response = await _airQualityService.GetComplete(city, cancellationToken);
        return Ok(response);
    }
    catch (DataUnavailableException ex)
    {
        return StatusCode(503, new { error = "Data unavailable" });
    }
}
```

**Request Flow:**

```
HTTP GET /api/v1/air-quality/Sarajevo/live
    ↓
ASP.NET Core Middleware Pipeline:
    1. CORS Middleware (AllowAnyOrigin)
    2. Routing Middleware
    3. Controller Middleware
    ↓
AirQualityController.GetLive(City.Sarajevo)
    ↓
_airQualityService.GetLive(City.Sarajevo)
    ↓
Response JSON
```

---

### **3. Service Layer - Business Logic**

**File:** `backend/src/BosniaAir.Api/Services/AirQualityService.cs`

#### **3.1 GetLive() - Vraća Cached Live Data**

```csharp
public async Task<LiveAqiResponse> GetLive(City city, CancellationToken cancellationToken = default)
{
    // 1️⃣ Pokuša čitati iz cache (database)
    var cached = await _repository.GetLive(city, cancellationToken);
    
    // 2️⃣ Ako postoji, vrati mapped response
    if (cached is not null)
    {
        return MapToLiveResponse(cached);
    }

    // 3️⃣ Ako ne postoji, baci error
    _logger.LogWarning("Live data requested for {City} but cache is empty", city);
    throw new DataUnavailableException(city, "live");
}
```

**Flow:**
```
GetLive(Sarajevo)
    ↓
Repository.GetLive(Sarajevo)
    ↓
SELECT * FROM AirQualityRecords 
WHERE City = 'Sarajevo' AND RecordType = 'LiveSnapshot'
ORDER BY Timestamp DESC
LIMIT 1
    ↓
AirQualityRecord entity
    ↓
MapToLiveResponse(record)
    ↓
LiveAqiResponse DTO
```

---

#### **3.2 RefreshCityAsync() - Fetch Fresh Data iz WAQI API**

```csharp
public Task RefreshCityAsync(City city, CancellationToken cancellationToken = default)
{
    return RefreshInternalAsync(city, cancellationToken);
}

private async Task<RefreshResult> RefreshInternalAsync(City city, CancellationToken cancellationToken)
{
    // 1️⃣ Fetchuje data sa WAQI API
    var waqiData = await FetchWaqiDataAsync(city, cancellationToken);
    var timestamp = ParseTimestamp(waqiData.Time);

    // 2️⃣ Kreira entity za database
    var record = new AirQualityRecord
    {
        City = city,
        StationId = city.ToStationId(),
        RecordType = AirQualityRecordType.LiveSnapshot,
        Timestamp = timestamp,
        AqiValue = waqiData.Aqi,
        DominantPollutant = MapDominantPollutant(waqiData.Dominentpol),
        Pm25 = waqiData.Iaqi?.Pm25?.V,
        Pm10 = waqiData.Iaqi?.Pm10?.V,
        O3 = waqiData.Iaqi?.O3?.V,
        // ... other pollutants
        CreatedAt = TimeZoneHelper.GetSarajevoTime()
    };

    // 3️⃣ Sprema u database
    await _repository.AddLive(record, cancellationToken);
    
    var liveResponse = MapToLiveResponse(record);

    // 4️⃣ Ako ima forecast data, procesuje i to
    ForecastResponse? forecastResponse = null;
    if (waqiData.Forecast?.Daily is not null)
    {
        var forecastDays = BuildForecastDays(waqiData.Forecast.Daily);
        if (forecastDays.Count > 0)
        {
            var cachePayload = new ForecastCache(timestamp, forecastDays);
            var serialized = JsonSerializer.Serialize(cachePayload, CacheSerializerOptions);
            await _repository.UpdateForecast(city, serialized, timestamp, cancellationToken);

            forecastResponse = new ForecastResponse(city.ToDisplayName(), forecastDays, timestamp);
        }
    }

    return new RefreshResult(liveResponse, forecastResponse);
}
```

**FetchWaqiDataAsync() - HTTP Call ka External API:**

```csharp
private async Task<WaqiData> FetchWaqiDataAsync(City city, CancellationToken cancellationToken)
{
    var stationId = city.ToStationId();
    var requestUri = $"feed/{stationId}/?token={_apiToken}";
    
    // HTTP GET: https://api.waqi.info/feed/@{stationId}/?token=xxx
    var response = await _httpClient.GetAsync(requestUri, cancellationToken);
    
    if (!response.IsSuccessStatusCode)
    {
        throw new HttpRequestException($"WAQI API returned {response.StatusCode}");
    }

    var content = await response.Content.ReadAsStringAsync(cancellationToken);
    var apiResponse = JsonSerializer.Deserialize<WaqiRootResponse>(content, CacheSerializerOptions);

    if (apiResponse?.Status != "ok" || apiResponse.Data is null)
    {
        throw new InvalidOperationException("WAQI API returned invalid data");
    }

    return apiResponse.Data;
}
```

**Complete Refresh Flow:**

```
RefreshCityAsync(Sarajevo)
    ↓
RefreshInternalAsync(Sarajevo)
    ↓
FetchWaqiDataAsync(Sarajevo)
    ↓
HTTP GET https://api.waqi.info/feed/@9605/?token=xxx
    ↓
JSON Response:
{
  "status": "ok",
  "data": {
    "aqi": 85,
    "time": { "s": "2024-10-03 12:00:00" },
    "iaqi": { "pm25": { "v": 35 }, "pm10": { "v": 45 } },
    "forecast": { "daily": { "pm25": [...] } }
  }
}
    ↓
Parse response → WaqiData object
    ↓
Create AirQualityRecord entity
    ↓
Repository.AddLive(record) → INSERT INTO database
    ↓
Repository.UpdateForecast(forecast) → UPDATE/INSERT forecast
    ↓
Return RefreshResult
```

---

### **4. Repository Layer - Data Access**

**File:** `backend/src/BosniaAir.Api/Repositories/AqiRepository.cs`

```csharp
// 1️⃣ GetLive - Čita najnoviji live record iz baze
public async Task<AirQualityRecord?> GetLive(City city, CancellationToken cancellationToken = default)
{
    return await _context.AirQualityRecords
        .AsNoTracking()  // Read-only
        .Where(r => r.City == city && r.RecordType == AirQualityRecordType.LiveSnapshot)
        .OrderByDescending(r => r.Timestamp)
        .FirstOrDefaultAsync(cancellationToken);
}

// 2️⃣ AddLive - Dodaje novi live record u bazu
public async Task AddLive(AirQualityRecord record, CancellationToken cancellationToken = default)
{
    record.RecordType = AirQualityRecordType.LiveSnapshot;
    record.CreatedAt = TimeZoneHelper.GetSarajevoTime();
    
    _context.AirQualityRecords.Add(record);
    await _context.SaveChangesAsync(cancellationToken);
}

// 3️⃣ UpdateForecast - Update ili insert forecast data
public async Task UpdateForecast(City city, string forecastJson, DateTime timestamp, CancellationToken cancellationToken = default)
{
    // Traži postojeći forecast za grad
    var existing = await _context.AirQualityRecords
        .FirstOrDefaultAsync(r => r.City == city && r.RecordType == AirQualityRecordType.Forecast, cancellationToken);

    if (existing is null)
    {
        // Insert new forecast
        _context.AirQualityRecords.Add(new AirQualityRecord
        {
            City = city,
            StationId = city.ToStationId(),
            RecordType = AirQualityRecordType.Forecast,
            Timestamp = timestamp,
            ForecastJson = forecastJson,
            CreatedAt = TimeZoneHelper.GetSarajevoTime()
        });
    }
    else
    {
        // Update existing forecast
        existing.Timestamp = timestamp;
        existing.ForecastJson = forecastJson;
        existing.UpdatedAt = TimeZoneHelper.GetSarajevoTime();
    }

    await _context.SaveChangesAsync(cancellationToken);
}
```

**Database Schema:**

```sql
CREATE TABLE AirQualityRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    City TEXT NOT NULL,
    StationId INTEGER NOT NULL,
    RecordType TEXT NOT NULL,  -- 'LiveSnapshot' or 'Forecast'
    Timestamp DATETIME NOT NULL,
    AqiValue INTEGER NULL,
    DominantPollutant TEXT NULL,
    Pm25 REAL NULL,
    Pm10 REAL NULL,
    O3 REAL NULL,
    No2 REAL NULL,
    Co REAL NULL,
    So2 REAL NULL,
    ForecastJson TEXT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL
);
```

---

## 🎨 Frontend Flow

### **Frontend Architecture Layers**

```
Components → Custom Hooks → API Client → Backend API
                ↓
         Observable Pattern
                ↓
            SWR Cache
```

---

### **1. Main Page Component - Entry Point**

**File:** `frontend/app/page.tsx`

**Component Lifecycle:**

```tsx
export default function HomePage() {
  // 1️⃣ State initialization
  const [primaryCity, setPrimaryCity] = useState<CityId | null>(null)
  const [preferencesLoaded, setPreferencesLoaded] = useState(false)
  const [isPreferencesModalOpen, setPreferencesModalOpen] = useState(false)

  // 2️⃣ Data fetching hooks
  const { data: aqiData, error, isLoading } = useLiveAqi(primaryCity)
  usePeriodicRefresh(60 * 1000)  // Auto-refresh every minute

  // 3️⃣ Effects
  useEffect(() => { /* Load city from localStorage */ }, [])
  useEffect(() => { /* Save city to localStorage */ }, [primaryCity])
  useEffect(() => { /* Trigger refresh on city change */ }, [primaryCity])

  // 4️⃣ Render logic
  if (!primaryCity) {
    return <PreferredCitySelectorModal />
  }

  return (
    <div>
      <Header />
      <LiveAqiPanel city={primaryCity} />
      <Pollutants measurements={aqiData?.measurements} />
      <ForecastTimeline city={primaryCity} />
      <SensitiveGroupsAdvice city={primaryCity} />
      <CitiesComparison primaryCity={primaryCity} />
    </div>
  )
}
```

---

### **2. Custom Hooks - Data Fetching Layer**

**File:** `frontend/lib/hooks.ts`

#### **2.1 useLiveAqi() - Fetch Live AQI Data**

```tsx
export function useLiveAqi(cityId: string | null, config?: SWRConfiguration) {
  // 1️⃣ SWR Hook - Cache + fetching
  const { data, error, isLoading, mutate } = useSWR<AqiResponse>(
    cityId ? `aqi-live-${cityId}` : null,  // Cache key
    () => apiClient.getLive(cityId!),      // Fetcher function
    {
      ...defaultConfig,
      refreshInterval: 0,           // Ne koristi SWR auto-refresh
      revalidateOnFocus: true,      // Refresh kad user vrati tab
      revalidateOnMount: true,      // Fetch odmah na mount
    }
  )

  const intervalMs = resolveInterval(config)

  // 2️⃣ Subscribe na Observable za coordinated refresh
  useEffect(() => {
    if (!cityId) {
      return  // Ne subscribe ako nema grada
    }

    const unsubscribe = airQualityObservable.subscribe(() => {
      void mutate()  // Kad Observable triggeruje, pozovi mutate()
    }, { intervalMs })

    return () => {
      unsubscribe()  // Cleanup na unmount
    }
  }, [cityId, mutate, intervalMs])

  const refresh = useCallback(() => mutate(), [mutate])

  return {
    data,
    error,
    isLoading,
    refresh,
  }
}
```

**Key Points:**

- **SWR Cache Key:** `'aqi-live-Sarajevo'` - Jedinstveni key za svaki grad
- **Fetcher Function:** `() => apiClient.getLive('Sarajevo')` - Poziva se kad je cache prazan ili stale
- **Observable Subscription:** Registruje callback koji poziva `mutate()` kad Observable triggeruje
- **mutate():** SWR funkcija koja revalidira cache i triggera re-fetch

---

#### **2.2 useComplete() - Fetch Complete Data (Live + Forecast)**

```tsx
export function useComplete(cityId: string | null, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<CompleteAqiResponse>(
    cityId ? `aqi-complete-${cityId}` : null,
    () => apiClient.getComplete(cityId!),
    {
      ...defaultConfig,
      refreshInterval: 0,
      revalidateOnFocus: true,
      revalidateOnMount: true,
    }
  )

  const intervalMs = resolveInterval(config)

  useEffect(() => {
    if (!cityId) {
      return
    }

    const unsubscribe = airQualityObservable.subscribe(() => {
      void mutate()
    }, { intervalMs })

    return () => {
      unsubscribe()
    }
  }, [cityId, mutate, intervalMs])

  const refresh = useCallback(() => mutate(), [mutate])

  return {
    data,
    error,
    isLoading,
    refresh,
  }
}
```

**Razlika od useLiveAqi:**
- **Cache Key:** `'aqi-complete-Sarajevo'`
- **Endpoint:** `/api/v1/air-quality/Sarajevo/complete` (umjesto `/live`)
- **Response:** Sadrži i live i forecast data

---

#### **2.3 usePeriodicRefresh() - Setup Auto-Refresh**

```tsx
export function usePeriodicRefresh(intervalMs: number = 10 * 60 * 1000) {
  const refreshAll = useRefreshAll()

  useEffect(() => {
    const previousInterval = airQualityObservable.getIntervalMs()
    airQualityObservable.setIntervalMs(intervalMs)  // Set interval to 60s
    airQualityObservable.notify()  // Initial trigger (nema efekta ako nema subscribera)

    return () => {
      airQualityObservable.setIntervalMs(previousInterval)  // Restore
    }
  }, [intervalMs])

  return refreshAll
}
```

**Šta radi:**
- Postavlja Observable refresh interval na 60 sekundi
- Poziva `notify()` odmah (ali nema subscribera u početku)
- Vraća `refreshAll` funkciju za manual triggering

---

### **3. API Client - HTTP Layer**

**File:** `frontend/lib/api-client.ts`

```tsx
class ApiClient {
  private baseUrl: string

  constructor() {
    const baseApiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
    this.baseUrl = `${baseApiUrl}/api/v1`
  }

  // 1️⃣ Get Live Data
  async getLive(cityId: string): Promise<AqiResponse> {
    const endpoint = `/air-quality/${encodeURIComponent(cityId)}/live`
    return this.request<AqiResponse>(endpoint)
  }

  // 2️⃣ Get Complete Data
  async getComplete(cityId: string): Promise<CompleteAqiResponse> {
    const endpoint = `/air-quality/${encodeURIComponent(cityId)}/complete`
    return this.request<CompleteAqiResponse>(endpoint)
  }

  // 3️⃣ Generic Request Method
  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`

    const response = await fetch(url, {
      credentials: 'include',
      cache: 'no-cache',
      headers: {
        'Content-Type': 'application/json',
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        ...options.headers,
      },
      ...options,
    })

    if (!response.ok) {
      throw new Error(`API Error: ${response.status} ${response.statusText}`)
    }

    const data = await response.json()
    return this.convertDates(data)  // Convert ISO strings to Date objects
  }

  // 4️⃣ Date Conversion
  private convertDates(obj: any): any {
    if (obj === null || obj === undefined) return obj
    if (typeof obj === 'string' && this.isDateString(obj)) {
      return new Date(obj)  // "2024-10-03T12:00:00Z" → Date object
    }
    if (Array.isArray(obj)) {
      return obj.map(item => this.convertDates(item))
    }
    if (typeof obj === 'object') {
      const converted: any = {}
      for (const [key, value] of Object.entries(obj)) {
        converted[key] = this.convertDates(value)
      }
      return converted
    }
    return obj
  }

  private isDateString(str: string): boolean {
    return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.test(str)
  }
}

export const apiClient = new ApiClient()
```

**Request Flow:**

```
apiClient.getLive('Sarajevo')
    ↓
request('/air-quality/Sarajevo/live')
    ↓
fetch('http://localhost:5000/api/v1/air-quality/Sarajevo/live')
    ↓
await response.json()
    ↓
convertDates(data)  // Recursively convert date strings
    ↓
Return typed AqiResponse
```

---

### **4. Observable Pattern - Event Coordination**

**File:** `frontend/lib/observable.ts`

```tsx
class AirQualityObservable {
  private readonly eventTarget = new EventTarget()
  private intervalId: number | null = null
  private subscriberCount = 0
  private intervalMs: number

  constructor(defaultIntervalMs: number) {
    this.intervalMs = defaultIntervalMs
  }

  // 1️⃣ Subscribe - Register listener
  subscribe(handler: RefreshHandler, options?: SubscribeOptions): () => void {
    const { intervalMs } = options ?? {}

    if (intervalMs && intervalMs !== this.intervalMs) {
      this.intervalMs = intervalMs
      this.restartTimer()
    }

    const listener = () => handler()
    this.eventTarget.addEventListener(REFRESH_EVENT, listener)
    this.subscriberCount += 1
    this.ensureTimer()  // Start timer if not running

    return () => {
      this.eventTarget.removeEventListener(REFRESH_EVENT, listener)
      this.subscriberCount = Math.max(0, this.subscriberCount - 1)
      if (this.subscriberCount === 0) {
        this.clearTimer()  // Stop timer if no subscribers
      }
    }
  }

  // 2️⃣ Notify - Trigger all subscribers
  notify(): void {
    this.eventTarget.dispatchEvent(new Event(REFRESH_EVENT))
  }

  // 3️⃣ Ensure Timer - Start if needed
  private ensureTimer(): void {
    if (this.intervalId != null) {
      return
    }

    if (typeof window === 'undefined') {
      return
    }

    this.intervalId = window.setInterval(() => {
      this.notify()
    }, this.intervalMs)
  }

  // 4️⃣ Clear Timer - Stop interval
  private clearTimer(): void {
    if (this.intervalId != null && typeof window !== 'undefined') {
      window.clearInterval(this.intervalId)
    }
    this.intervalId = null
  }
}

export const airQualityObservable = new AirQualityObservable(60 * 1000)
```

**Observable States:**

```javascript
// Initial State (No Subscribers)
{
  intervalMs: 60000,
  subscriberCount: 0,
  intervalId: null,  // Timer NOT running
  eventTarget: { listeners: [] }
}

// After First Subscribe
{
  intervalMs: 60000,
  subscriberCount: 1,
  intervalId: 123456,  // Timer RUNNING
  eventTarget: { 
    listeners: [
      { type: 'aqi-refresh', callback: () => mutate() }
    ] 
  }
}

// After Multiple Subscribes
{
  intervalMs: 60000,
  subscriberCount: 3,
  intervalId: 123456,
  eventTarget: { 
    listeners: [
      { type: 'aqi-refresh', callback: () => mutateSarajevo() },
      { type: 'aqi-refresh', callback: () => mutateTuzla() },
      { type: 'aqi-refresh', callback: () => mutateComplete() }
    ] 
  }
}
```

---

## 🔄 Integration Flow - Frontend ↔ Backend

### **Complete Request-Response Cycle**

```
┌─────────────────────────────────────────────────────────────────┐
│                         FRONTEND                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  User Action / Timer Tick                                      │
│         ↓                                                       │
│  Observable.notify()                                           │
│         ↓                                                       │
│  All Subscribers Triggered                                     │
│         ↓                                                       │
│  mutate() called (SWR)                                         │
│         ↓                                                       │
│  Fetcher Function Executes                                     │
│         ↓                                                       │
│  apiClient.getLive('Sarajevo')                                 │
│         ↓                                                       │
│  fetch('http://localhost:5000/api/v1/air-quality/Sarajevo/live')│
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            ↓
                    HTTP GET Request
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│                         BACKEND                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ASP.NET Core receives request                                 │
│         ↓                                                       │
│  CORS Middleware (AllowAnyOrigin)                              │
│         ↓                                                       │
│  Routing Middleware                                            │
│         ↓                                                       │
│  AirQualityController.GetLive(City.Sarajevo)                   │
│         ↓                                                       │
│  _airQualityService.GetLive(City.Sarajevo)                     │
│         ↓                                                       │
│  _repository.GetLive(City.Sarajevo)                            │
│         ↓                                                       │
│  Entity Framework Query:                                       │
│  SELECT * FROM AirQualityRecords                               │
│  WHERE City = 'Sarajevo' AND RecordType = 'LiveSnapshot'       │
│  ORDER BY Timestamp DESC LIMIT 1                               │
│         ↓                                                       │
│  AirQualityRecord entity                                       │
│         ↓                                                       │
│  MapToLiveResponse(record)                                     │
│         ↓                                                       │
│  LiveAqiResponse DTO                                           │
│         ↓                                                       │
│  JSON Serialization                                            │
│         ↓                                                       │
│  HTTP 200 OK Response                                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            ↓
                    HTTP Response (JSON)
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│                         FRONTEND                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  response.json() → Parse JSON                                  │
│         ↓                                                       │
│  apiClient.convertDates(data)                                  │
│         ↓                                                       │
│  {                                                             │
│    city: "Sarajevo",                                           │
│    overallAqi: 85,                                             │
│    timestamp: Date object,  // Converted from ISO string       │
│    measurements: [...]                                         │
│  }                                                             │
│         ↓                                                       │
│  SWR Updates Cache:                                            │
│  cache['aqi-live-Sarajevo'] = data                             │
│         ↓                                                       │
│  React State Update                                            │
│         ↓                                                       │
│  Component Re-render with Fresh Data                           │
│         ↓                                                       │
│  UI Updated ✅                                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📋 Konkretni Scenariji

### **Scenario 1: Prvi Put User Otvori Stranicu (Empty localStorage)**

#### **Timeline:**

```
T+0ms: Browser učitava stranicu
T+10ms: React komponenta HomePage se renderuje
T+20ms: State inicijalizacija
    primaryCity = null
    preferencesLoaded = false
    isPreferencesModalOpen = false

T+30ms: Hooks execute
    useLiveAqi(null) → NO FETCH (key = null)
    usePeriodicRefresh(60000) → Observable.notify() (no subscribers yet)

T+40ms: useEffect #1 executes (localStorage read)
    localStorage.getItem('bosniaair.primaryCity') → null
    setPrimaryCity(null) → No change
    setPreferencesModalOpen(true) → State change
    setPreferencesLoaded(true) → State change

T+50ms: React re-render #1
    Modal se prikazuje
    UI: City selection modal visible

-- User thinks and selects 'Sarajevo' --

T+5000ms: User clicks "Sarajevo" button
    handleModalSave('Sarajevo')
    setPrimaryCity('Sarajevo') → State change
    setPreferencesModalOpen(false) → State change

T+5010ms: React re-render #2
    useLiveAqi('Sarajevo') executes:
        - SWR key = 'aqi-live-Sarajevo'
        - Fetcher: () => apiClient.getLive('Sarajevo')
        - revalidateOnMount = true → FETCH TRIGGERED
        - isLoading = true

T+5020ms: Observable subscription
    airQualityObservable.subscribe(() => mutate())
    subscriberCount: 0 → 1
    ensureTimer() → setInterval starts (60000ms)

T+5030ms: useEffect #2 (save to localStorage)
    localStorage.setItem('bosniaair.primaryCity', 'Sarajevo')

T+5040ms: useEffect #3 (manual refresh)
    airQualityObservable.notify()
    EventTarget dispatches 'aqi-refresh'
    Listener callback: mutate() executes

T+5050ms: HTTP Request #1 (from revalidateOnMount)
    GET http://localhost:5000/api/v1/air-quality/Sarajevo/live

T+5060ms: HTTP Request #2 (from manual notify)
    SWR sees duplicate request → DEDUPLICATES → Samo jedan HTTP poziv!

T+5200ms: Backend response received
    JSON parsed
    Dates converted
    SWR cache updated: cache['aqi-live-Sarajevo'] = data

T+5210ms: React re-render #3
    useLiveAqi returns:
        data = { city: "Sarajevo", overallAqi: 85, ... }
        isLoading = false
    
T+5220ms: UI updates with data
    ✅ LiveAqiPanel shows AQI: 85
    ✅ Pollutants display
    ✅ Forecast loads (separate request)
    ✅ Everything visible

T+65000ms: Timer tick (60 seconds later)
    Observable timer callback executes
    notify() called
    All subscribers triggered
    mutate() executes
    Fresh API call → New data
```

**Total Time to First Data:** ~200ms (5000ms user thinking + 200ms network)

---

### **Scenario 2: Returning User (localStorage Has 'Sarajevo')**

#### **Timeline:**

```
T+0ms: Browser učitava stranicu
T+10ms: React komponenta HomePage se renderuje
T+20ms: State inicijalizacija
    primaryCity = null
    preferencesLoaded = false

T+30ms: Hooks execute
    useLiveAqi(null) → NO FETCH (key = null)
    usePeriodicRefresh(60000) → notify() (no effect)

T+40ms: useEffect #1 executes (localStorage read)
    localStorage.getItem('bosniaair.primaryCity') → 'Sarajevo'
    setPrimaryCity('Sarajevo') → State change ✅
    setPreferencesModalOpen(false) → Stays false
    setPreferencesLoaded(true) → State change

T+50ms: React re-render #1 (with city immediately!)
    useLiveAqi('Sarajevo') executes:
        - SWR key = 'aqi-live-Sarajevo'
        - Fetcher: () => apiClient.getLive('Sarajevo')
        - revalidateOnMount = true → FETCH TRIGGERED
        - isLoading = true

T+60ms: Observable subscription
    airQualityObservable.subscribe(() => mutate())
    subscriberCount: 0 → 1
    Timer starts

T+70ms: useEffect #2 (save to localStorage)
    localStorage.setItem('bosniaair.primaryCity', 'Sarajevo')
    // No change - already has 'Sarajevo'

T+80ms: useEffect #3 (manual refresh)
    airQualityObservable.notify()
    mutate() executes

T+90ms: HTTP Request
    GET http://localhost:5000/api/v1/air-quality/Sarajevo/live

T+230ms: Backend response received
    Data cached
    React re-render #2
    UI shows data ✅

T+60000ms: Timer tick
    Auto-refresh cycle continues...
```

**Total Time to First Data:** ~240ms (immediate, no user interaction needed!)

**Key Difference:** NO MODAL! User vidi loading skeleton odmah, a zatim data.

---

### **Scenario 3: User Mijenja Grad (Sarajevo → Tuzla)**

#### **Timeline:**

```
T+0ms: Current state
    primaryCity = 'Sarajevo'
    SWR cache = { 'aqi-live-Sarajevo': { aqi: 85, ... } }
    Observable subscriberCount = 1
    Timer running

T+100ms: User clicks city selector → Selects 'Tuzla'
    handleCityChange('Tuzla')
    setPrimaryCity('Tuzla') → State change

T+110ms: React re-render
    useLiveAqi cleanup for Sarajevo:
        - unsubscribe() called
        - EventTarget removes listener for Sarajevo
        - subscriberCount: 1 → 0
        - Timer stopped (clearTimer())

T+120ms: useLiveAqi('Tuzla') mount
    - SWR key = 'aqi-live-Tuzla'
    - Cache check → MISS (no data for Tuzla)
    - Fetcher triggered
    - isLoading = true

T+130ms: Observable re-subscription for Tuzla
    airQualityObservable.subscribe(() => mutateTuzla())
    subscriberCount: 0 → 1
    Timer restarted

T+140ms: useEffect #2 (save to localStorage)
    localStorage.setItem('bosniaair.primaryCity', 'Tuzla')

T+150ms: useEffect #3 (manual refresh)
    airQualityObservable.notify()
    mutateTuzla() executes

T+160ms: HTTP Request
    GET http://localhost:5000/api/v1/air-quality/Tuzla/live

T+300ms: Backend response
    Data for Tuzla received
    SWR cache = { 
      'aqi-live-Sarajevo': { aqi: 85, ... },  // Old, still cached
      'aqi-live-Tuzla': { aqi: 42, ... }      // New
    }

T+310ms: React re-render
    UI shows Tuzla data ✅

T+60000ms: Timer tick
    All subscribers for Tuzla refresh
```

**Important:** Svaki grad ima svoj cache entry u SWR! Kad se vratimo na Sarajevo, data će biti instant (iz cache).

---

### **Scenario 4: Background Service Auto-Refresh (Backend)**

#### **Timeline:**

```
T+0s: Application starts
    AirQualityScheduler.ExecuteAsync() begins
    _logger.LogInformation("Scheduler starting...")

T+1s: First refresh cycle (immediate)
    RunRefreshCycle()
        ↓
    Parallel tasks created for all cities:
        - RefreshCityAsync(Sarajevo)
        - RefreshCityAsync(Tuzla)
        - RefreshCityAsync(Mostar)
        - RefreshCityAsync(Travnik)
        - RefreshCityAsync(Zenica)
        - RefreshCityAsync(Bihac)

T+2s: Sarajevo refresh starts
    Scope created
    AirQualityService.RefreshCityAsync(Sarajevo)
        ↓
    FetchWaqiDataAsync(Sarajevo)
        ↓
    HTTP GET https://api.waqi.info/feed/@9605/?token=xxx

T+2s: Tuzla refresh starts (parallel!)
    HTTP GET https://api.waqi.info/feed/@9609/?token=xxx

T+2s: Other cities start (all parallel)

T+3s: Sarajevo WAQI response received
    Parse JSON
    Create AirQualityRecord entity
    Repository.AddLive(record)
    INSERT INTO AirQualityRecords (City, AqiValue, Pm25, ...)
    Process forecast if available
    Repository.UpdateForecast(...)

T+3.5s: Tuzla response received
    Same process as Sarajevo

T+4s: All cities finished
    Task.WhenAll() completes
    _logger.LogInformation("Refresh cycle completed")

T+600s: Wait for next cycle (10 minutes)
    await Task.Delay(TimeSpan.FromMinutes(10))

T+610s: Next refresh cycle starts
    Repeat from T+1s
```

**Key Points:**
- **Parallel Execution:** Svi gradovi refreshuju istovremeno
- **Error Isolation:** Ako jedan grad faila, drugi nastavljaju
- **Database Updates:** Svaki grad dobije novi record u bazi
- **Independent of Frontend:** Radi bez obzira da li je frontend otvoren

---

## 📚 Sve Funkcije i Njihova Uloga

### **Backend Functions**

#### **AirQualityScheduler.cs**

| Funkcija | Tip | Svrha | Kada Se Poziva |
|----------|-----|-------|----------------|
| `ExecuteAsync()` | protected override | Main loop - kontroliše timing | Kada aplikacija startuje |
| `RunRefreshCycle()` | private | Refreshuje SVE gradove paralelno | Svake 10 minuta + na startu |
| `RefreshCityAsync()` | private | Refreshuje jedan grad | Za svaki grad u listi |
| `ParseCity()` | private | Parsuje string u City enum | Config validation |

#### **AirQualityController.cs**

| Funkcija | HTTP Method | Endpoint | Svrha |
|----------|------------|----------|-------|
| `GetLive()` | GET | `/api/v1/air-quality/{city}/live` | Vraća cached live AQI data |
| `GetForecast()` | GET | `/api/v1/air-quality/{city}/forecast` | Vraća cached forecast data |
| `GetComplete()` | GET | `/api/v1/air-quality/{city}/complete` | Vraća live + forecast zajedno |

#### **AirQualityService.cs**

| Funkcija | Tip | Svrha | Poziva |
|----------|-----|-------|--------|
| `GetLive()` | public | Čita live data iz cache | `Repository.GetLive()` |
| `GetForecast()` | public | Čita forecast iz cache | `Repository.GetForecast()` |
| `GetComplete()` | public | Kombinuje live + forecast | `GetLive()` + `GetForecast()` |
| `RefreshCityAsync()` | public | Entry point za refresh | `RefreshInternalAsync()` |
| `RefreshInternalAsync()` | private | Main refresh logic | `FetchWaqiDataAsync()`, `Repository.AddLive()` |
| `FetchWaqiDataAsync()` | private | HTTP call ka WAQI API | External API |
| `MapToLiveResponse()` | private | Entity → DTO mapping | - |
| `BuildForecastDays()` | private | Parse forecast data | - |

#### **AqiRepository.cs**

| Funkcija | Tip | Svrha | SQL Operation |
|----------|-----|-------|---------------|
| `GetLive()` | public | Čita najnoviji live record | SELECT ... WHERE RecordType = 'LiveSnapshot' |
| `AddLive()` | public | Dodaje novi live record | INSERT INTO AirQualityRecords |
| `GetForecast()` | public | Čita forecast record | SELECT ... WHERE RecordType = 'Forecast' |
| `UpdateForecast()` | public | Update/Insert forecast | UPDATE or INSERT |

---

### **Frontend Functions**

#### **page.tsx**

| Funkcija | Tip | Svrha | Trigger |
|----------|-----|-------|---------|
| `HomePage()` | Component | Main page logic | React render |
| `handleModalSave()` | Event handler | Sprema selected city | User clicks save |
| `cityIdToLabel()` | Utility | Converts ID to display name | Rendering |

#### **hooks.ts**

| Funkcija | Tip | Svrha | Poziva |
|----------|-----|-------|--------|
| `useLiveAqi()` | Custom Hook | Fetches live AQI data | `apiClient.getLive()`, `airQualityObservable.subscribe()` |
| `useComplete()` | Custom Hook | Fetches complete data | `apiClient.getComplete()`, `airQualityObservable.subscribe()` |
| `useRefreshAll()` | Custom Hook | Manual refresh trigger | `airQualityObservable.notify()` |
| `usePeriodicRefresh()` | Custom Hook | Setup auto-refresh | `airQualityObservable.setIntervalMs()` |
| `resolveInterval()` | Utility | Resolve refresh interval | - |

#### **api-client.ts**

| Funkcija | Tip | Svrha | HTTP |
|----------|-----|-------|------|
| `getLive()` | public | Fetch live data | GET `/air-quality/{city}/live` |
| `getComplete()` | public | Fetch complete data | GET `/air-quality/{city}/complete` |
| `request()` | private | Generic HTTP request | `fetch()` |
| `convertDates()` | private | ISO string → Date objects | - |
| `isDateString()` | private | Validate date format | - |

#### **observable.ts**

| Funkcija | Tip | Svrha | Internal |
|----------|-----|-------|----------|
| `subscribe()` | public | Register listener | `addEventListener()`, `ensureTimer()` |
| `notify()` | public | Trigger all listeners | `dispatchEvent()` |
| `getIntervalMs()` | public | Get current interval | - |
| `setIntervalMs()` | public | Set new interval | `restartTimer()` |
| `ensureTimer()` | private | Start timer if needed | `setInterval()` |
| `restartTimer()` | private | Restart timer | `clearTimer()`, `ensureTimer()` |
| `clearTimer()` | private | Stop timer | `clearInterval()` |

---

## 🎯 Key Takeaways

### **Data Flow Patterns**

1. **Backend Auto-Refresh:** Background service refreshuje data svake 10 minuta, independent od frontend-a
2. **Frontend Cache-First:** SWR prvo provjerava cache, zatim fetchuje ako je potrebno
3. **Observable Coordination:** Jedan centralni Observable koordinira refresh za sve komponente
4. **Parallel Execution:** Backend refreshuje sve gradove paralelno, Frontend deduplikuje duplicate requests

### **Performance Optimizations**

1. **SWR Caching:** Sprječava duplicate API calls
2. **Request Deduplication:** Multiple komponente dijele isti API request
3. **Parallel Backend Refresh:** Svi gradovi refreshuju istovremeno
4. **Lazy Loading:** Data se fetchuje samo kada je komponenta mountovana

### **Error Handling**

1. **Backend:** Try-catch na svakom nivou, logiranje errora
2. **Frontend:** SWR automatski retry logic, error state u UI
3. **Isolation:** Error u jednom gradu ne blokira druge gradove

---

## 📝 Summary

Aplikacija koristi **three-tier architecture**:
- **Frontend:** React/Next.js sa custom hooks i Observable pattern
- **Backend:** ASP.NET Core sa service layer i repository pattern
- **Integration:** HTTP REST API sa JSON responses

**Data freshness** se održava na dva načina:
1. **Backend:** Background service refreshuje data svake 10 minuta
2. **Frontend:** Observable triggera refresh svake minute za sve komponente

**Key technologies:**
- **Frontend:** SWR (caching), EventTarget (Observable), TypeScript
- **Backend:** Entity Framework (ORM), Dependency Injection, Background Services
- **Communication:** REST API, JSON, CORS

---

*End of Documentation*
