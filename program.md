# 🎓 **KOMPLETNI C# / ASP.NET CORE TUTORIAL ZA REACT DEVELOPERA**

Odlično! Hajde da ti objasnim **SVE** od početka - zašto koristimo ove tehnologije, kako rade, i **comparison sa React/Next.js** svijetom koji već poznaješ! 💪

---

## 📚 **PART 1: ZAŠTO OVE TEHNOLOGIJE?**

---

### **1️⃣ ASP.NET Core - "Express.js Backend-a, Ali Enterprise"**

#### **Šta Je ASP.NET Core?**
- **Web framework** za kreiranje API-ja i web aplikacija
- **Cross-platform** (Windows, Mac, Linux)
- **Open-source** i super brz

#### **React/Next.js Comparison:**

| Koncept | Next.js/Node.js | ASP.NET Core |
|---------|-----------------|--------------|
| **Runtime** | Node.js (JavaScript) | .NET Runtime (C#) |
| **Framework** | Next.js (React framework) | ASP.NET Core (Web framework) |
| **Use Case** | Frontend + API routes | Backend APIs, MVC apps |
| **Package Manager** | npm/yarn | NuGet |
| **Entry Point** | `app/page.tsx` | Program.cs |

#### **Zašto Je Dobar?**

```csharp
// 1. PERFORMANCE - Brži od Node.js
// Benchmark: ASP.NET Core radi 7M requests/sec vs Node.js 1M requests/sec

// 2. TYPE SAFETY - C# je strongly-typed (kao TypeScript, ali built-in)
public int Calculate(int a, int b)  // Mora biti int!
{
    return a + b;
}

// 3. ENTERPRISE FEATURES - Built-in dependency injection, logging, configuration
builder.Services.AddScoped<IAirQualityService, AirQualityService>();
// ↑ Next.js nema ovako nešto built-in!

// 4. ASYNC/AWAIT - Kao u JS, ali bolje integrisano
public async Task<string> FetchDataAsync()
{
    return await httpClient.GetStringAsync("...");
}
```

**Usporedba:**
```javascript
// Next.js API Route
export async function GET(request) {
  const data = await fetch('...')
  return Response.json(data)
}
```

```csharp
// ASP.NET Core Controller Action
[HttpGet]
public async Task<IActionResult> Get()
{
    var data = await _service.GetDataAsync();
    return Ok(data);  // Returns 200 OK with JSON
}
```

---

### **2️⃣ Entity Framework Core - "Prisma/Drizzle Backend-a"**

#### **Šta Je EF Core?**
- **ORM** (Object-Relational Mapper)
- Konvertuje C# objekte → SQL queries
- Kao Prisma ili TypeORM

#### **React/Next.js Comparison:**

```typescript
// Prisma (Next.js)
const user = await prisma.user.findUnique({
  where: { id: 1 }
})
```

```csharp
// Entity Framework Core (ASP.NET)
var user = await context.Users
    .FirstOrDefaultAsync(u => u.Id == 1);
```

**Oba rade isto - database query bez pisanja SQL-a!**

#### **Zašto Je Dobar?**

```csharp
// 1. TYPE-SAFE QUERIES
var records = await context.AirQualityRecords
    .Where(r => r.City == City.Sarajevo)  // ← IntelliSense ovdje!
    .OrderByDescending(r => r.Timestamp)
    .Take(10)
    .ToListAsync();

// 2. AUTOMATIC MIGRATIONS - kao Prisma migrate
// dotnet ef migrations add InitialCreate
// dotnet ef database update

// 3. RELATIONSHIPS - automatski rješava foreign keys
public class AirQualityRecord
{
    public int Id { get; set; }
    public City City { get; set; }
    public DateTime Timestamp { get; set; }
}

// 4. CHANGE TRACKING - zna šta je promijenjeno
record.Aqi = 100;
await context.SaveChangesAsync();  // ← Automatski UPDATE query!
```

---

### **3️⃣ Dependency Injection - "React Context, Ali Za Backend"**

#### **React Comparison:**

```tsx
// React Context Pattern
const ApiContext = createContext<ApiClient | null>(null)

export function ApiProvider({ children }) {
  const apiClient = new ApiClient()
  return (
    <ApiContext.Provider value={apiClient}>
      {children}
    </ApiContext.Provider>
  )
}

// Use u komponenti:
function MyComponent() {
  const apiClient = useContext(ApiContext)
  // ...
}
```

**ASP.NET Core verzija:**

```csharp
// Register u Program.cs (kao Provider)
builder.Services.AddScoped<IAirQualityService, AirQualityService>();
//                         ^^^^^^^^^^^^^^^^^^^  ^^^^^^^^^^^^^^^^^^^
//                         Interface (contract)  Implementation

// Use u Controller-u (kao useContext):
public class AirQualityController : ControllerBase
{
    private readonly IAirQualityService _service;
    
    // Constructor Injection - .NET automatski ubrizgava instance!
    public AirQualityController(IAirQualityService service)
    {
        _service = service;  // ← Dobio gotov objekat!
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var data = await _service.GetLiveAqiAsync();
        return Ok(data);
    }
}
```

**Magija:** .NET automatski kreira `AirQualityService` instance i injektuje ih gdje god treba! 🪄

---

### **4️⃣ Middleware - "Next.js Middleware, Ali Moćniji"**

#### **Next.js Middleware:**

```typescript
// middleware.ts
export function middleware(request: NextRequest) {
  console.log('Request:', request.url)
  
  if (!request.cookies.get('auth')) {
    return NextResponse.redirect('/login')
  }
  
  return NextResponse.next()
}
```

#### **ASP.NET Core Middleware:**

```csharp
// ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    
    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;  // Next middleware u chain-u
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);  // Pozovi next middleware (kao next())
        }
        catch (Exception ex)
        {
            // Handle error
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
    }
}

// Register u Program.cs:
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Pipeline Comparison:**

```
Next.js:
Request → middleware.ts → API Route → Response

ASP.NET Core:
Request → Middleware #1 → Middleware #2 → Controller → Response
```

---

### **5️⃣ Service Pattern - "Custom Hooks, Ali Za Backend"**

#### **React Custom Hook:**

```tsx
// hooks/useAirQuality.ts
export function useAirQuality(city: string) {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  
  useEffect(() => {
    fetch(`/api/aqi/${city}`)
      .then(res => res.json())
      .then(setData)
      .finally(() => setLoading(false))
  }, [city])
  
  return { data, loading }
}
```

#### **ASP.NET Core Service:**

```csharp
// Services/AirQualityService.cs
public class AirQualityService : IAirQualityService
{
    private readonly HttpClient _httpClient;
    private readonly IAirQualityRepository _repository;
    
    public AirQualityService(
        HttpClient httpClient,
        IAirQualityRepository repository)
    {
        _httpClient = httpClient;
        _repository = repository;
    }
    
    public async Task<LiveAqiResponse> GetLiveAqiAsync(City city)
    {
        // Business logic ovdje
        var data = await _repository.GetLatestAqi(city);
        return MapToResponse(data);
    }
}
```

**Služi istoj svrsi:** Enkapsulira business logic, reusable kod!

---

## 📄 **PART 2: LINE-BY-LINE ANALIZA Program.cs**

Sada hajde da pređemo **SVAKU LINIJU** ovog fajla! 💪

---

### **🔧 Section 1: Setup & Environment**

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
// ... ostali using statements
```

**Šta Je `using`?**
- Kao `import` u JavaScript-u!

```javascript
// JavaScript
import { useState } from 'react'
import axios from 'axios'
```

```csharp
// C#
using Microsoft.EntityFrameworkCore;
using BosniaAir.Api.Services;
```

---

```csharp
/// <summary>
/// Main entry point for the BosniaAir API. Sets up ASP.NET Core with services, middleware, and background workers.
/// </summary>
```

**XML Documentation Comments:**
- Kao JSDoc u JavaScript-u
- IntelliSense ih prikazuje u IDE-u

```javascript
// JavaScript JSDoc
/**
 * Main entry point for the app
 */
```

```csharp
// C# XML Comments
/// <summary>
/// Main entry point for the app
/// </summary>
```

---

```csharp
Env.Load("../../../.env");
```

**Environment Variables:**
- Kao `dotenv` u Node.js!

```javascript
// Node.js
require('dotenv').config()
process.env.DATABASE_URL
```

```csharp
// C#
Env.Load(".env");
Environment.GetEnvironmentVariable("DATABASE_URL")
```

---

```csharp
var builder = WebApplication.CreateBuilder(args);
```

**`WebApplication.CreateBuilder()`:**
- **Entry point** aplikacije
- Kao `Next.js` app initialization

**Next.js Equivalent:**

```typescript
// Next.js nema explicit builder, radi automatski:
// next.config.js → konfiguracija
// app/layout.tsx → root layout
```

**ASP.NET Core:**

```csharp
var builder = WebApplication.CreateBuilder(args);
// ↑ Kreira builder koji će postaviti sve servise

builder.Services.AddControllers();  // Dodaj controllere
builder.Services.AddDbContext<AppDbContext>();  // Dodaj database

var app = builder.Build();  // BUILD aplikaciju
app.Run();  // POKRENI server
```

**Flow:**

```
1. CreateBuilder() → Napravi builder
2. builder.Services.Add...() → Registruj servise
3. builder.Build() → Izgradi app
4. app.Use...() → Dodaj middleware
5. app.Run() → Pokreni server ✅
```

---

### **🪵 Section 2: Logging Setup (Serilog)**

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
```

**Serilog - Structured Logging:**

```javascript
// Next.js - obični console.log
console.log('User logged in:', userId)
// Output: "User logged in: 123"
```

```csharp
// ASP.NET Core - Serilog (structured)
Log.Information("User {UserId} logged in", userId);
// Output (JSON): { "message": "User logged in", "UserId": 123, "timestamp": "..." }
```

**Zašto Structured?**
- Lakše za search u production logs
- Tools kao Seq/Elasticsearch mogu parsirati
- Bolje za debugging

---

```csharp
builder.Host.UseSerilog();
```

**Integracija sa ASP.NET:**
- Serilog postaje default logger
- Automatski logira sve HTTP requeste

---

### **🌐 Section 3: CORS Configuration**

```csharp
var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", frontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

**CORS - Cross-Origin Resource Sharing:**

**Problem:**
```
Frontend: http://localhost:3000
Backend:  http://localhost:5000

Browser: "NOPE! Different origins - CORS error!" ❌
```

**Rješenje:**

```javascript
// Next.js API Route
export async function GET() {
  return new Response(JSON.stringify(data), {
    headers: {
      'Access-Control-Allow-Origin': 'http://localhost:3000',
      'Access-Control-Allow-Methods': 'GET, POST',
    }
  })
}
```

```csharp
// ASP.NET Core - CORS Policy (čistije!)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", policy =>
    {
        policy.WithOrigins("http://localhost:3000")  // Allowed origins
              .AllowAnyHeader()   // Allow all headers (Content-Type, Authorization, etc.)
              .AllowAnyMethod()   // Allow GET, POST, PUT, DELETE, etc.
              .AllowCredentials(); // Allow cookies/credentials
    });
});

// Kasnije aktiviraj:
app.UseCors("FrontendOnly");
```

**Line-by-Line:**

```csharp
var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
//                   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^    ^^^^^^^^^^^^^^^^^^^^^^^^^^
//                   Read from config/env variable                Fallback ako nema u config
```

**`??` Operator - Null-Coalescing:**

```javascript
// JavaScript
const origin = process.env.FRONTEND_ORIGIN || 'http://localhost:3000'
```

```csharp
// C#
var origin = builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";
```

---

```csharp
policy.WithOrigins("http://localhost:3000", "http://localhost:3001", frontendOrigin)
```

**Multiple Origins:**
- Dev: `localhost:3000`
- Storybook: `localhost:3001`
- Production: `frontendOrigin` (iz env variable)

---

### **🎮 Section 4: Controllers & JSON Setup**

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
```

**`AddControllers()` - Register MVC Controllers:**

```csharp
// Registruje sve @[ApiController] klase:
public class AirQualityController : ControllerBase
{
    [HttpGet("/api/aqi")]
    public IActionResult GetAqi() { ... }
}
```

**Next.js Equivalent:**

```
Next.js:
app/api/aqi/route.ts → Automatic API route

ASP.NET Core:
Controllers/AirQualityController.cs → Manual registration
```

---

**JSON Serialization Options:**

```csharp
options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
```

**C# Property → JSON Key Conversion:**

```csharp
// C# Class:
public class AirQualityRecord
{
    public int OverallAqi { get; set; }  // PascalCase (C# convention)
    public string CityName { get; set; }
}

// Without camelCase policy:
// {"OverallAqi": 85, "CityName": "Sarajevo"}

// With camelCase policy:
// {"overallAqi": 85, "cityName": "Sarajevo"}  ← Frontend-friendly!
```

---

```csharp
options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
```

**Custom Date Converter:**
- Konvertuje `DateTime` → ISO 8601 string
- Kao `date.toISOString()` u JS

```csharp
// C# DateTime:
var timestamp = new DateTime(2024, 10, 7, 15, 30, 0);

// JSON output (with converter):
"timestamp": "2024-10-07T15:30:00Z"
```

---

```csharp
options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
```

**Enum Serialization:**

```csharp
// C# Enum:
public enum City
{
    Sarajevo = 10557,
    Tuzla = 8739
}

// Without converter:
{ "city": 10557 }  // Number

// With converter:
{ "city": "Sarajevo" }  // String ← Frontend-friendly!
```

---

### **📖 Section 5: Swagger/OpenAPI**

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BosniaAir API",
        Version = "v1",
        Description = "Air quality monitoring API",
        Contact = new OpenApiContact { ... }
    });
});
```

**Swagger - API Documentation:**

**Next.js:**
```typescript
// No built-in API docs
// Need manual documentation or tools like Redoc
```

**ASP.NET Core:**
```csharp
// Swagger auto-generates interactive API docs!
// Visit: http://localhost:5000/swagger
```

**Screenshot Equivalent:**

```
┌─────────────────────────────────────────┐
│  Swagger UI                             │
├─────────────────────────────────────────┤
│  GET /api/v1/air-quality/{city}/live    │
│  ├─ Try it out                          │
│  ├─ Parameters: city = Sarajevo         │
│  ├─ Execute ▶                           │
│  └─ Response: { "city": "Sarajevo", ... }│
└─────────────────────────────────────────┘
```

---

### **🗄️ Section 6: Database Configuration**

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? builder.Configuration["CONNECTION_STRING"]
                       ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                       ?? "Data Source=bosniaair-aqi.db";
```

**Multiple Fallbacks - Priority Order:**

```
1. appsettings.json: "ConnectionStrings:DefaultConnection"
2. appsettings.json: "CONNECTION_STRING"
3. Environment variable: DATABASE_URL
4. Default: SQLite file "bosniaair-aqi.db"
```

**Next.js Equivalent:**

```typescript
const dbUrl = 
  process.env.DATABASE_URL ||
  process.env.CONNECTION_STRING ||
  'postgresql://localhost:5432/mydb'
```

---

```csharp
if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}
```

**Smart Database Detection:**

```
Connection String:
  "postgresql://..." → UseNpgsql() → PostgreSQL
  "Data Source=..."  → UseSqlite() → SQLite
```

**Why?**
- **Dev:** SQLite (no setup needed, just a file!)
- **Prod:** PostgreSQL (Scalable, production-ready)

**Prisma Equivalent:**

```javascript
// prisma/schema.prisma
datasource db {
  provider = "postgresql"  // or "sqlite"
  url      = env("DATABASE_URL")
}
```

---

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

**`AddDbContext<T>()` - Register EF Core:**

```csharp
// Registruje AppDbContext za dependency injection
// Sada možeš koristiti u controller-ima:

public class MyController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public MyController(AppDbContext context)  // ← Automatski injected!
    {
        _context = context;
    }
    
    public async Task<IActionResult> Get()
    {
        var records = await _context.AirQualityRecords.ToListAsync();
        return Ok(records);
    }
}
```

---

### **🔐 Section 7: WAQI API Configuration**

```csharp
builder.Services.Configure<AqicnConfiguration>(options =>
{
    builder.Configuration.GetSection("Aqicn").Bind(options);
    
    var envToken = builder.Configuration["WAQI_API_TOKEN"];
    if (!string.IsNullOrWhiteSpace(envToken))
    {
        options.ApiToken = envToken;
    }
});
```

**Configuration Binding:**

**appsettings.json:**
```json
{
  "Aqicn": {
    "ApiUrl": "https://api.waqi.info/",
    "ApiToken": "demo"
  }
}
```

**C# Class:**
```csharp
public class AqicnConfiguration
{
    public string ApiUrl { get; set; }
    public string ApiToken { get; set; }
}
```

**Bind JSON → C# Class:**
```csharp
builder.Configuration.GetSection("Aqicn").Bind(options);
// ↑ Automatski mapira JSON properties na C# properties!
```

**Next.js Equivalent:**

```typescript
// next.config.js
module.exports = {
  env: {
    WAQI_API_URL: 'https://api.waqi.info/',
    WAQI_API_TOKEN: process.env.WAQI_API_TOKEN
  }
}

// Use:
const apiUrl = process.env.WAQI_API_URL
```

---

**Override sa Environment Variable:**

```csharp
var envToken = builder.Configuration["WAQI_API_TOKEN"];
if (!string.IsNullOrWhiteSpace(envToken))
{
    options.ApiToken = envToken;  // ← Override config value!
}
```

**Priority:**
```
1. Environment variable (highest)
2. appsettings.json (fallback)
```

---

### **🌐 Section 8: HTTP Client Setup**

```csharp
builder.Services.AddHttpClient<AirQualityService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration.GetValue<string>("Aqicn:ApiUrl") ?? "https://api.waqi.info/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "BosniaAir/1.0");
    })
    .AddStandardResilienceHandler();
```

**Typed HTTP Client:**

```javascript
// Next.js - manual axios setup
const axios = require('axios')
const apiClient = axios.create({
  baseURL: 'https://api.waqi.info/',
  timeout: 30000,
  headers: { 'User-Agent': 'BosniaAir/1.0' }
})
```

```csharp
// ASP.NET Core - typed HttpClient (cleaner!)
builder.Services.AddHttpClient<AirQualityService>()
    .ConfigureHttpClient((sp, client) =>
    {
        client.BaseAddress = new Uri("https://api.waqi.info/");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "BosniaAir/1.0");
    });

// Use in service:
public class AirQualityService
{
    private readonly HttpClient _httpClient;
    
    public AirQualityService(HttpClient httpClient)  // ← Auto-configured!
    {
        _httpClient = httpClient;
    }
    
    public async Task<string> FetchAsync()
    {
        return await _httpClient.GetStringAsync("/feed/sarajevo");
        // ↑ BaseAddress already set!
    }
}
```

---

**Resilience Handler:**

```csharp
.AddStandardResilienceHandler();
```

**Što Radi:**
- **Retry** - Ako API call faila, pokušaj ponovo (3×)
- **Timeout** - Cancel ako traje predugo
- **Circuit Breaker** - Prestani zvati ako je API down
- **Rate Limiting** - Spriječi spam

**Next.js Equivalent:**

```typescript
// Manual retry logic
async function fetchWithRetry(url: string, retries = 3) {
  for (let i = 0; i < retries; i++) {
    try {
      return await fetch(url)
    } catch (err) {
      if (i === retries - 1) throw err
      await sleep(1000 * Math.pow(2, i))  // Exponential backoff
    }
  }
}
```

ASP.NET ima ovo **built-in**! 🎉

---

### **💉 Section 9: Dependency Injection Registration**

```csharp
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();
builder.Services.AddScoped<IAirQualityService>(sp => sp.GetRequiredService<AirQualityService>());
builder.Services.AddHostedService<AirQualityScheduler>();
builder.Services.AddHealthChecks();
```

**Service Lifetimes:**

| Lifetime | Description | React Equivalent |
|----------|-------------|------------------|
| **Singleton** | One instance for entire app | `const client = new ApiClient()` (outside component) |
| **Scoped** | One instance per HTTP request | `useMemo(() => new Client(), [])` (per render) |
| **Transient** | New instance every time | `new Client()` (every time) |

**Tvoj Kod:**

```csharp
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();
//                ^^^^^^^
//                Lifetime: SCOPED (per request)
```

**Why Scoped?**
- Repository koristi DbContext
- DbContext ne smije se dijeliti između requestova (concurrency issues!)
- Novi repo instance za svaki HTTP request ✅

---

**Interface → Implementation Mapping:**

```csharp
builder.Services.AddScoped<IAirQualityRepository, AirQualityRepository>();
//                         ^^^^^^^^^^^^^^^^^^^^^^  ^^^^^^^^^^^^^^^^^^^^
//                         Interface (contract)    Implementation (actual class)
```

**When You Request:**

```csharp
public class AirQualityService
{
    public AirQualityService(IAirQualityRepository repository)
    //                       ^^^^^^^^^^^^^^^^^^^^^^
    //                       .NET will inject AirQualityRepository instance here!
    {
        _repository = repository;
    }
}
```

**Next.js Equivalent:**

```tsx
// Interface (TypeScript)
interface IApiClient {
  fetch(url: string): Promise<any>
}

// Implementation
class ApiClient implements IApiClient {
  async fetch(url: string) { ... }
}

// Usage (manual injection)
const apiClient: IApiClient = new ApiClient()
```

ASP.NET radi ovo **automatski**! 🪄

---

**Background Service Registration:**

```csharp
builder.Services.AddHostedService<AirQualityScheduler>();
```

**`IHostedService` - Background Task:**

```csharp
// AirQualityScheduler runs in background
public class AirQualityScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Refresh data every 10 minutes
            await RefreshAllCitiesAsync();
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
```

**Next.js Equivalent:**

```typescript
// NO BUILT-IN SUPPORT!
// Need external cron service (Vercel Cron, GitHub Actions, etc.)

// Workaround: API route + external trigger
export async function GET() {
  await refreshAllCities()
  return Response.json({ ok: true })
}

// External cron hits: GET /api/cron/refresh
```

ASP.NET ima **built-in background services**! 🎉

---

**Health Checks:**

```csharp
builder.Services.AddHealthChecks();
```

**Što Radi:**
- Endpoint `/health` koji vraća status aplikacije
- Koristi se za load balancers, monitoring tools

```bash
$ curl http://localhost:5000/health
{
  "status": "Healthy",
  "checks": [
    { "name": "database", "status": "Healthy" }
  ]
}
```

---

### **🏗️ Section 10: Build The App**

```csharp
var app = builder.Build();
```

**`Build()` - Finalize Configuration:**

```
builder.Services.Add...()  → Register services
builder.Build()            → Compile everything into app
app.Use...()               → Add middleware
app.Run()                  → Start server
```

**Next.js Equivalent:**

```typescript
// Next.js does this automatically:
// next build → Compiles app
// next start → Starts server
```

---

### **🪵 Section 11: Middleware Pipeline**

```csharp
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("FrontendOnly");
```

**Middleware Order MATTERS!**

```
Request Flow:
    ↓
1. UseSerilogRequestLogging()  ← Log request
    ↓
2. ExceptionHandlingMiddleware  ← Catch errors
    ↓
3. UseCors("FrontendOnly")      ← CORS headers
    ↓
4. UseRouting()                 ← Match route
    ↓
5. MapControllers()             ← Execute controller
    ↓
Response
```

**Next.js Middleware:**

```typescript
// middleware.ts (runs BEFORE all routes)
export function middleware(request: NextRequest) {
  console.log('Request:', request.url)  // Logging
  
  // Error handling would be in try/catch
  // CORS is automatic in Next.js
  
  return NextResponse.next()
}
```

---

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

**Environment-Specific Code:**

```csharp
// Development:
app.UseSwagger();  // Enable Swagger UI

// Production:
// Swagger is disabled (not in if block)
```

**Next.js Equivalent:**

```typescript
if (process.env.NODE_ENV === 'development') {
  console.log('Dev mode')
}
```

---

### **🛣️ Section 12: Routing**

```csharp
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
```

**`MapControllers()` - Register All Controller Routes:**

```csharp
// Finds all classes with [ApiController] attribute:
[ApiController]
[Route("api/v1/air-quality")]
public class AirQualityController : ControllerBase
{
    [HttpGet("{city}/live")]
    public IActionResult GetLive(string city) { ... }
}

// Maps to: GET /api/v1/air-quality/{city}/live
```

**Next.js Equivalent:**

```
Next.js:
app/api/v1/air-quality/[city]/live/route.ts
    ↓ Automatic routing

ASP.NET:
Controllers/AirQualityController.cs
    ↓ Manual [Route] attributes
    ↓ MapControllers() registers them
```

---

**Health Check Endpoint:**

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new { status = report.Status.ToString(), ... };
        await context.Response.WriteAsJsonAsync(response);
    }
});
```

**Custom Health Response:**

```bash
GET /health

{
  "status": "Healthy",
  "checks": [
    { "name": "self", "status": "Healthy", "duration": "00:00:00.0012345" }
  ],
  "duration": "00:00:00.0012345"
}
```

---

### **🗄️ Section 13: Database Initialization**

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database ready");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize database");
    }
}
```

**Database Schema Creation:**

```csharp
context.Database.EnsureCreatedAsync();
// ↑ Creates tables if they don't exist (like Prisma migrate)
```

**Prisma Equivalent:**

```bash
# Prisma
npx prisma migrate deploy  # Apply migrations
npx prisma db push         # Or push schema directly
```

```csharp
// EF Core
context.Database.EnsureCreatedAsync();  // Create schema
// OR
context.Database.MigrateAsync();  // Apply migrations
```

---

**Dependency Injection Scope:**

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // ↑ Manually create scope to get DbContext
}
// ↑ Scope disposed here (DbContext cleaned up)
```

**Why Manual Scope?**
- Program.cs nema HTTP request context
- DbContext je `Scoped` (per request)
- Moramo manually kreirati scope za initialization

---

### **🚀 Section 14: Start The Server**

```csharp
Log.Information("BosniaAir API starting up...");
app.Run();
```

**`app.Run()` - Start Server:**

```
1. Listens on configured port (default: 5000)
2. Accepts HTTP requests
3. Routes through middleware pipeline
4. Executes controllers
5. Returns responses
```

**Next.js Equivalent:**

```bash
# Next.js
npm run dev  # Starts dev server
npm start    # Starts production server
```

```csharp
// ASP.NET Core
app.Run();  // Start server (blocking call)
```

---

**Logging Startup:**

```csharp
Log.Information("BosniaAir API starting up...");
// Output: [12:00:00 INF] BosniaAir API starting up...
```

---

### **🧪 Section 15: Test Entry Point**

```csharp
public partial class Program { }
```

**Why Empty Class?**
- Allows **integration tests** to reference `Program` class
- Tests can spin up the entire app

```csharp
// In test project:
public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();  // ← References Program class!
    }
    
    [Fact]
    public async Task GetLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/air-quality/Sarajevo/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

**Next.js Testing:**

```typescript
// Similar with Vitest/Jest
import { createServer } from 'http'
import handler from './app/api/route'

test('API returns OK', async () => {
  const res = await fetch('http://localhost:3000/api/...')
  expect(res.status).toBe(200)
})
```

---

## 🎯 **SUMMARY: Key Concepts**

### **1. Dependency Injection:**
```csharp
// Register:
builder.Services.AddScoped<IService, ServiceImpl>();

// Use:
public MyController(IService service) { ... }  // Auto-injected!
```

### **2. Middleware Pipeline:**
```csharp
Request → Logging → Error Handler → CORS → Routing → Controller → Response
```

### **3. Configuration:**
```csharp
appsettings.json → builder.Configuration["Key"]
Environment variables → override config
```

### **4. Entity Framework:**
```csharp
DbContext → LINQ queries → SQL generation → Database
```

### **5. Service Lifetimes:**
```
Singleton → One instance (app lifetime)
Scoped    → One per request
Transient → New every time
```

---

## 🎤 **Za Intervju - Quick Reference**

**"Objasni Program.cs":**

> "Program.cs je **entry point** ASP.NET Core aplikacije. Koristim `WebApplication.CreateBuilder()` da postavim sve servise:
>
> 1. **Dependency Injection** - Registrujem repository-e, servise, HTTP client-e
> 2. **Database** - Konfiguriram EF Core sa PostgreSQL/SQLite
> 3. **CORS** - Dozvolim frontend origin-e
> 4. **Middleware** - Logging, error handling, CORS headers
> 5. **Controllers** - Registrujem API endpoint-e
> 6. **Background Services** - Scheduler za periodic refresh
>
> Nakon build-a, dodajem middleware pipeline i pokrećem server sa `app.Run()`. **Sve je konfigurisano kroz dependency injection** što omogućava clean, testable kod."

---

Je li ti sada jasnije kako C# i ASP.NET Core rade? 🎯


Odlično! Ovaj fajl je **mnogo složeniji** - ima **pravu logiku**! Hajde da ga razložim **liniju po liniju**.

---

## **Šta je Svrha Ovog Fajla?**

Ovo je **custom JSON converter** koji osigurava da se **svi datumi uvijek serijalizuju i deserijalizuju kao UTC**.

**Problem koji rješava:**
```json
// ❌ Bez convertera - messy datumi
{
  "timestamp": "2024-01-15T14:30:00",      // Koja timezone?
  "timestamp": "2024-01-15T14:30:00+01:00" // CET timezone
}

// ✅ Sa converterom - uvijek UTC
{
  "timestamp": "2024-01-15T13:30:00.000000Z" // Z = UTC (Zulu time)
}
```

**React/Next.js Problem:**
```typescript
// Kada šalješ Date preko API-ja
const date = new Date(); // "Mon Jan 15 2024 14:30:00 GMT+0100"

// JSON.stringify() automatski konvertuje u ISO string
JSON.stringify({ timestamp: date })
// {"timestamp":"2024-01-15T13:30:00.000Z"} ✅ Uvijek UTC

// C# BEZ ovog convertera može vratiti:
// "2024-01-15T14:30:00" ❌ (nema timezone info)
```

---

## **Linija po Linija - Kompletna Analiza**

### **1️⃣ Import Statements**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using BosniaAir.Api.Utilities;
```

**`using`** = kao `import` u JavaScript-u

| C# | TypeScript Ekvivalent |
|----|----------------------|
| `using System.Text.Json;` | `import { } from 'json-library';` |
| `using X.Y.Z;` | `import { } from 'x/y/z';` |

**Šta se importuje:**
- **`System.Text.Json`** = Built-in JSON library (kao `JSON.parse/stringify`)
- **`System.Text.Json.Serialization`** = JSON converter base klase
- **`BosniaAir.Api.Utilities`** = Custom utilities (vjerovatno `TimeZoneHelper`)

---

### **2️⃣ Namespace**

```csharp
namespace BosniaAir.Api.Configuration;
```

Već smo objasnili - organizacija koda (kao folder struktura).

---

### **3️⃣ XML Dokumentacija**

```csharp
/// <summary>
/// JSON converter for DateTime values that ensures UTC serialization and proper parsing.
/// Handles conversion between local times and UTC for API communication.
/// </summary>
```

JSDoc ekvivalent - opisuje šta klasa radi.

---

### **4️⃣ Class Declaration sa Nasleđivanjem**

```csharp
public class UtcDateTimeConverter : JsonConverter<DateTime>
```

Hajde da razložimo **svaki dio**:

#### **`public class UtcDateTimeConverter`**

Već znamo - public klasa sa imenom `UtcDateTimeConverter`.

#### **`: JsonConverter<DateTime>`** - Inheritance (Nasleđivanje)

**Ovo je KLJUČNO!**

```csharp
public class UtcDateTimeConverter : JsonConverter<DateTime>
//                                  ↑
//                                  Nasljeđuje (extends) JsonConverter klasu
```

**React/TypeScript Ekvivalent:**

```typescript
// C# inheritance
public class UtcDateTimeConverter : JsonConverter<DateTime>

// TypeScript equivalent
export class UtcDateTimeConverter extends JsonConverter<Date> {
  // Ili implementira interface
  implements JsonConverter<Date>
}
```

**Šta je `JsonConverter<DateTime>`?**
- **Base klasa** iz `System.Text.Json.Serialization`
- **Generic klasa** - `<DateTime>` znači "converter za DateTime tip"
- Mora implementirati 2 metode: `Read()` i `Write()`

**Analogija:**
```typescript
// React Hook pattern
class MyHook extends React.Hook { // Mora implementirati lifecycle metode
  componentDidMount() { }
  componentWillUnmount() { }
}
```

---

### **5️⃣ `Read()` Metoda - Deserialization (JSON → C# Object)**

```csharp
public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    var value = reader.GetString();
    if (DateTime.TryParse(value, out var date))
    {
        return date.ToUniversalTime();
    }
    return TimeZoneHelper.GetSarajevoTime();
}
```

Hajde **red po red**:

---

#### **Signature:**

```csharp
public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
```

**`public`** - Svi mogu pozvati metodu

**`override`** - Override metode iz base klase

```csharp
// Base klasa ima apstraktnu metodu:
abstract DateTime Read(...);

// Mi je overridujemo (implementiramo):
public override DateTime Read(...) { }
```

**TypeScript Ekvivalent:**
```typescript
abstract class JsonConverter<T> {
  abstract read(data: string): T; // Mora biti implementirano
}

class UtcDateTimeConverter extends JsonConverter<Date> {
  override read(data: string): Date { // Override parent metode
    // implementacija
  }
}
```

**`DateTime`** - Return tip (vraća `DateTime` objekat)

**`Read`** - Ime metode

**Parametri:**

| Parametar | Tip | Šta je |
|-----------|-----|--------|
| `ref Utf8JsonReader reader` | JSON reader | Čita JSON byte-po-byte |
| `Type typeToConvert` | Type info | Meta-informacija o tipu (`typeof(DateTime)`) |
| `JsonSerializerOptions options` | Options | JSON serialization postavke |

**`ref`** keyword:
```csharp
// ref = Pass by reference (kao pointer u C/C++)
public void Method(ref int x) 
{
    x = 10; // Mijenja originalnu varijablu, ne kopiju
}

// Bez ref = Pass by value (kopija)
public void Method(int x) 
{
    x = 10; // Mijenja lokalnu kopiju
}
```

**JavaScript nema `ref`** - objekti se uvijek šalju po referenci, primitivni tipovi po vrijednosti.

---

#### **Tijelo Metode - Linija 1:**

```csharp
var value = reader.GetString();
```

**`var`** = Type inference (kompajler sam odredi tip)

```csharp
// Ove 2 linije su IDENTIČNE:
var value = reader.GetString();        // ✅ Kraće (kompajler zna da je string)
string value = reader.GetString();     // ✅ Eksplicitno
```

**TypeScript Ekvivalent:**
```typescript
const value = reader.getString();      // Type inference
const value: string = reader.getString(); // Eksplicitno
```

**`reader.GetString()`** - Čita string vrijednost iz JSON-a

```json
// JSON:
{ "timestamp": "2024-01-15T14:30:00Z" }

// reader.GetString() vraća:
"2024-01-15T14:30:00Z"
```

---

#### **Tijelo Metode - Linija 2-5:**

```csharp
if (DateTime.TryParse(value, out var date))
{
    return date.ToUniversalTime();
}
```

**`DateTime.TryParse(value, out var date)`** - Parse string u DateTime

Ovo je **POSEBAN C# pattern** - `TryParse` sa `out` parametrom!

**Kako radi:**
```csharp
DateTime.TryParse(value, out var date)
// ↓
// Ako parsing uspije: vraća true, date = parsed vrijednost
// Ako parsing ne uspije: vraća false, date = default(DateTime)
```

**React/JavaScript Ekvivalent:**
```javascript
// C# sa TryParse
if (DateTime.TryParse(value, out var date)) {
    return date.ToUniversalTime();
}

// JavaScript ekvivalent
try {
    const date = new Date(value);
    if (!isNaN(date.getTime())) {
        return date; // Uspješno parsovano
    }
} catch {
    // Parsing failed
}
```

**`out var date`** - Deklaracija varijable UNUTAR parametra!

```csharp
// ✅ Nova C# sintaksa (inline declaration)
if (DateTime.TryParse(value, out var date))

// ❌ Stara sintaksa (mora se deklarisati prije)
DateTime date;
if (DateTime.TryParse(value, out date))
```

**`date.ToUniversalTime()`** - Konvertuje u UTC

```csharp
var local = new DateTime(2024, 1, 15, 14, 30, 0); // 14:30 local time
var utc = local.ToUniversalTime();                // 13:30 UTC (ako je CET +01:00)
```

---

#### **Tijelo Metode - Linija 6:**

```csharp
return TimeZoneHelper.GetSarajevoTime();
```

**Fallback** - Ako parsing ne uspije, vrati trenutno vrijeme u Sarajevu.

```csharp
// Ako value nije validna datum string:
value = "invalid-date"
// TryParse vraća false
// Izvršava se fallback: return TimeZoneHelper.GetSarajevoTime();
```

---

### **6️⃣ `Write()` Metoda - Serialization (C# Object → JSON)**

```csharp
public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
{
    var utcDateTime = value.Kind == DateTimeKind.Unspecified ? 
        DateTime.SpecifyKind(value, DateTimeKind.Utc) : 
        value.ToUniversalTime();
        
    writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"));
}
```

---

#### **Signature:**

```csharp
public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
```

**`void`** - Ne vraća ništa (kao `void` u TypeScript ili nedostatak `return` u JS)

**Parametri:**

| Parametar | Tip | Šta je |
|-----------|-----|--------|
| `Utf8JsonWriter writer` | JSON writer | Piše JSON byte-po-byte |
| `DateTime value` | DateTime | Vrijednost koja se serijalizuje |
| `JsonSerializerOptions options` | Options | JSON serialization postavke |

---

#### **Tijelo Metode - Linija 1-3 (Ternary Operator):**

```csharp
var utcDateTime = value.Kind == DateTimeKind.Unspecified ? 
    DateTime.SpecifyKind(value, DateTimeKind.Utc) : 
    value.ToUniversalTime();
```

Ovo je **ternary operator** (kao u JavaScript-u):

```csharp
// C# ternary
condition ? ifTrue : ifFalse

// JavaScript ternary
condition ? ifTrue : ifFalse  // Isto!
```

**Raščlanjeno:**

```csharp
// 1. Provjera: Da li DateTime nema timezone info?
value.Kind == DateTimeKind.Unspecified

// 2. Ako DA (nema timezone): Postavi da je UTC
DateTime.SpecifyKind(value, DateTimeKind.Utc)

// 3. Ako NE (ima timezone): Konvertuj u UTC
value.ToUniversalTime()
```

**`DateTimeKind` enum:**

| Vrijednost | Znači | Primjer |
|-----------|-------|---------|
| **`Unspecified`** | Nema timezone info | `2024-01-15T14:30:00` |
| **`Local`** | Local timezone | `2024-01-15T14:30:00+01:00` |
| **`Utc`** | UTC timezone | `2024-01-15T13:30:00Z` |

**Zašto ova logika?**

```csharp
// Scenario 1: DateTime bez timezone (Kind = Unspecified)
var date = new DateTime(2024, 1, 15, 14, 30, 0); // Kind = Unspecified
// ↓ SpecifyKind kaže "ovo je zapravo UTC, samo nema oznaku"
var utc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
// ↓ Rezultat: "2024-01-15T14:30:00Z" (ista vrijednost, samo dodaje Z)

// Scenario 2: DateTime sa local timezone (Kind = Local)
var date = DateTime.Now; // Kind = Local, npr. 14:30 CET
// ↓ ToUniversalTime() konvertuje iz local u UTC
var utc = date.ToUniversalTime(); // 13:30 UTC
// ↓ Rezultat: "2024-01-15T13:30:00Z" (vrijednost se MIJENJA)
```

**TypeScript Ekvivalent:**

```typescript
// JavaScript Date uvijek ima timezone info
const date = new Date(); // Uvijek zna svoju timezone

// JSON.stringify automatski konvertuje u UTC
JSON.stringify({ timestamp: date })
// {"timestamp":"2024-01-15T13:30:00.000Z"}
```

---

#### **Tijelo Metode - Linija 4:**

```csharp
writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"));
```

**`writer.WriteStringValue(...)`** - Piše string u JSON

**`utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ")`** - Custom format string

**Format string specifikatori:**

| Simbol | Znači | Primjer |
|--------|-------|---------|
| **`yyyy`** | 4-digit godina | `2024` |
| **`MM`** | 2-digit mjesec | `01` (Januar) |
| **`dd`** | 2-digit dan | `15` |
| **`T`** | Literal "T" (separator) | `T` |
| **`HH`** | 2-digit sat (24h) | `14` |
| **`mm`** | 2-digit minute | `30` |
| **`ss`** | 2-digit sekunde | `00` |
| **`.ffffff`** | 6-digit microsekunde | `.123456` |
| **`Z`** | Literal "Z" (UTC oznaka) | `Z` |

**Rezultat:**
```
2024-01-15T14:30:00.123456Z
```

**JavaScript Ekvivalent:**

```typescript
// JavaScript Date formatting
const date = new Date();

// ISO format (automatic)
date.toISOString();
// "2024-01-15T14:30:00.123Z" (3 decimale)

// C# custom format daje 6 decimala:
// "2024-01-15T14:30:00.123456Z" (6 decimala)
```

---

## **Kompletan Flow - Kako Radi U Praksi**

### **Scenario 1: API Prima Request (Deserialization - Read)**

```json
POST /api/air-quality
{
  "timestamp": "2024-01-15T14:30:00+01:00"
}
```

**Šta se dešava:**

```csharp
// 1. ASP.NET Core poziva Read() metodu
var value = reader.GetString();
// value = "2024-01-15T14:30:00+01:00"

// 2. TryParse parsuje string
if (DateTime.TryParse(value, out var date)) // ✅ true
{
    // date = 2024-01-15 14:30:00 (Kind = Local, offset +01:00)
    
    return date.ToUniversalTime();
    // Vraća: 2024-01-15 13:30:00 UTC (konvertovano iz CET)
}
```

**Rezultat:** C# objekat ima datum u UTC (13:30), bez obzira što je poslano u CET (14:30).

---

### **Scenario 2: API Vraća Response (Serialization - Write)**

```csharp
// Controller vraća objekat
return new AirQualityData 
{
    Timestamp = DateTime.Now // Local time: 14:30 CET
};
```

**Šta se dešava:**

```csharp
// 1. ASP.NET Core poziva Write() metodu
// value = 2024-01-15 14:30:00 (Kind = Local)

// 2. Ternary provjera
var utcDateTime = value.Kind == DateTimeKind.Unspecified ? 
    DateTime.SpecifyKind(value, DateTimeKind.Utc) : 
    value.ToUniversalTime(); // ✅ Ova grana (Kind = Local)
// utcDateTime = 2024-01-15 13:30:00 UTC

// 3. Format u string
writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"));
// Piše: "2024-01-15T13:30:00.000000Z"
```

**JSON Response:**
```json
{
  "timestamp": "2024-01-15T13:30:00.000000Z"
}
```

**Rezultat:** Uvijek se vraća UTC, bez obzira u kojoj timezone je server!

---

## **Gdje Se Registruje Ovaj Converter?** 

Searched text for `UtcDateTimeConverter`, 7 results

U Program.cs:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter()); // ← Registracija
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
```

**Što znači:** Svaki `DateTime` u API requestovima i responseima će automatski proći kroz ovaj converter!

---

## **Kompletan TypeScript Ekvivalent**

```typescript
// Custom JSON converter u TypeScript
class UtcDateTimeConverter {
  // Deserialization (JSON string → Date object)
  static read(value: string): Date {
    try {
      const date = new Date(value);
      if (!isNaN(date.getTime())) {
        return date; // JavaScript Date je uvijek UTC-aware
      }
    } catch {
      // Fallback
      return new Date(); // Current time
    }
    return new Date();
  }

  // Serialization (Date object → JSON string)
  static write(value: Date): string {
    // JavaScript toISOString() uvijek vraća UTC
    return value.toISOString();
    // "2024-01-15T13:30:00.123Z"
  }
}

// U Next.js API route:
export async function POST(req: Request) {
  const body = await req.json();
  
  // Ručna konverzija (JavaScript nema auto-converters)
  if (body.timestamp) {
    body.timestamp = UtcDateTimeConverter.read(body.timestamp);
  }
  
  // ... obrada
  
  return NextResponse.json({
    timestamp: UtcDateTimeConverter.write(new Date())
  });
}
```

**Razlika:**
- **C#:** Converteri se automatski aktiviraju za SVE DateTime properties
- **JavaScript:** Moraš ručno konvertovati ili koristiti библиотеку (npr. `class-transformer`)

---

## **Za Intervju - Odgovor**

> "`UtcDateTimeConverter` je **custom JSON converter** koji nasljeđuje `JsonConverter<DateTime>`. Implementira 2 metode:
>
> **`Read()`** - Deserijalizacija: Prima JSON string, parsuje ga sa `TryParse`, i konvertuje u UTC sa `ToUniversalTime()`. Ako parsing ne uspije, koristi fallback (`TimeZoneHelper.GetSarajevoTime()`).
>
> **`Write()`** - Serijalizacija: Prima `DateTime` objekat, provjerava njegov `Kind` (timezone info). Ako nema timezone (`Unspecified`), označava ga kao UTC. Ako ima local timezone, konvertuje u UTC. Na kraju formatira u ISO 8601 string sa 6 decimala i 'Z' oznakom.
>
> **Svrha:** Osigurava da API UVIJEK radi sa UTC vremenom, bez obzira na server timezone ili client timezone. Sprečava timezone konfuziju i olakšava international deployments."

**TL;DR:** Automatski konvertuje sve datume u UTC tokom JSON serijalizacije/deserijalizacije! 🕒


"DataUnavailableException je custom exception class koja definiše specifičan tip greške sa dodatnim podacima (City i DataKind). Bacamo je kada nema keširanih podataka za grad.

ExceptionHandlingMiddleware je global error handler koji hvata SVE exceptione u aplikaciji i konvertuje ih u standardizovane HTTP responsove sa odgovarajućim status kodovima.

Rade zajedno: Service baca DataUnavailableException, Controller je ne hvata (propagira dalje), a Middleware je hvata i vraća HTTP 503 Service Unavailable sa JSON responsom u Problem Details formatu.

Ovaj pattern omogućava Separation of Concerns - Business layer definiše greške, Presentation layer ih obrađuje. Slično je React Error Boundary patteru gdje komponente bacaju greške, a Boundary ih hvata i prikazuje."