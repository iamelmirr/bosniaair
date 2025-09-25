/*
=== SARAJEVO AIR API - MAIN STARTUP FILE ===

HIGH LEVEL OVERVIEW:
- Ova klasa je ENTRY POINT cele aplikacije
- Konfigurira Dependency Injection Container (DI)
- Postavlja HTTP request pipeline (middleware)
- Inicijalizuje bazu podataka
- Pokreće background servise za prikupljanje podataka

ARHITEKTURA PATTERN: 
Program.cs koristi "Minimal API" pattern (.NET 6+)
umesto starih Startup.cs klasa

DEPENDENCY FLOW:
Program.cs → konfigurira sve servise → servisi se injektuju u Controller-e
Controller → poziva Service → Service poziva Repository → Repository pristupa bazi
*/

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SarajevoAir.Api.Configuration;
using SarajevoAir.Api.Data;
using SarajevoAir.Api.Middleware;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Services;
using Serilog;

// WebApplication.CreateBuilder() - kreira builder objekat za konfiguraciju aplikacije
// args - komandni argumenti prosleđeni aplikaciji (npr. --urls, --environment)
var builder = WebApplication.CreateBuilder(args);

/*
=== SERILOG KONFIGURACIJA ===
Serilog je STRUCTURED LOGGING framework - bolji od default ASP.NET Core logger-a
PREDNOSTI: JSON output, filtering, rich context, performance

LOW LEVEL OBJAŠNJENJE:
- ReadFrom.Configuration() - čita postavke iz appsettings.json sekcije "Serilog"
- Enrich.FromLogContext() - dodaje kontekst info (user ID, request ID, itd.)
- WriteTo.Console() - šalje log-ove na konzolu (stdout)
- CreateLogger() - kreira globalni logger objekat

USAGE PATTERN:
U servisu: _logger.LogInformation("Fetched data for {City}", cityName);
*/

// Kreiranje globalnog Serilog logger-a koji će se koristiti kroz celu aplikaciju
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  // Čita config iz appsettings.json
    .Enrich.FromLogContext()                       // Dodaje rich context u log-ove
    .WriteTo.Console()                            // Output ide na konzolu
    .CreateLogger();

// Zamenjuje default ASP.NET Core logger sa Serilog-om
builder.Host.UseSerilog();

/*
=== CORS (Cross-Origin Resource Sharing) KONFIGURACIJA ===
CORS je BROWSER SECURITY FEATURE koji sprečava frontend da poziva backend sa drugog domena

PROBLEM: Browser blokira pozive između http://localhost:3000 (frontend) i http://localhost:5000 (backend)
REŠENJE: Eksplicitno dozvoljivamo cross-origin pozive sa određenih domena

LOW LEVEL OBJAŠNJENJE:
- WithOrigins() - lista dozvoljenih domena koji mogu da pozivaju API
- AllowAnyHeader() - dozvoljava sve HTTP zaglavlja (Content-Type, Authorization, itd.)
- AllowAnyMethod() - dozvoljava sve HTTP metode (GET, POST, PUT, DELETE)
- AllowCredentials() - dozvoljava slanje cookies-a i auth tokena

SECURITY NOTE: U produkciji, nikad ne koristi "*" wildcard nego specifične domene!
*/

// Čita frontend URL iz environment varijabli ili koristi default za development
var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:3000";

// Dodaje CORS servise u Dependency Injection container
builder.Services.AddCors(options =>
{
    // Kreira CORS policy sa nazivom "FrontendOnly"
    options.AddPolicy("FrontendOnly", policy =>
    {
        // Lista dozvoljenih domena - uključuje development portove i production domain
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002", frontendOrigin)
              .AllowAnyHeader()      // Dozvoljava sva HTTP zaglavlja
              .AllowAnyMethod()      // Dozvoljava sve HTTP metode (GET, POST, PUT, DELETE)
              .AllowCredentials();   // Dozvoljava cookies i auth tokene
    });
});

/*
=== MVC CONTROLLERS I API DOKUMENTACIJA ===
ASP.NET Core koristi MVC (Model-View-Controller) pattern za API endpoints

LOW LEVEL OBJAŠNJENJE:
- AddControllers() - registruje sve Controller klase u DI container
- AddEndpointsApiExplorer() - omogućava Swagger-u da automatski otkrije API endpoints
- AddSwaggerGen() - generiše OpenAPI dokumentaciju iz Controller-a i atributa

SWAGGER BENEFITS:
1. Automatska API dokumentacija
2. Interactive API testiranje u browser-u
3. Client code generation (TypeScript, C#, itd.)
4. API versioning support

CONTROLLER DISCOVERY:
ASP.NET automatski pronalazi sve klase koje:
- Nasleđuju ControllerBase klasu
- Ili imaju [ApiController] atribut
- I nalaze se u namespace-u koji završava sa ".Controllers"
*/

// Registruje MVC Controller servise - automatski pronalazi sve Controller klase
builder.Services.AddControllers();

// Omogućava Swagger-u da automatski analizira Controller-e i generiše dokumentaciju
builder.Services.AddEndpointsApiExplorer();

// Konfiguracija Swagger/OpenAPI dokumentacije
builder.Services.AddSwaggerGen(c =>
{
    // Definiše osnovne informacije o API-ju koja se prikazuju u Swagger UI
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SarajevoAir API",                                    // Naslov API-ja
        Version = "v1",                                               // Verzija API-ja
        Description = "Air quality monitoring API for Sarajevo and surrounding areas",  // Opis
        Contact = new OpenApiContact                                  // Kontakt informacije
        {
            Name = "SarajevoAir Team",
            Url = new Uri("https://sarajevoair.vercel.app")
        }
    });
});

/*
=== DATABASE KONFIGURACIJA ===
Entity Framework Core (EF Core) je Microsoft-ov ORM (Object-Relational Mapping) framework

SQLITE PREDNOSTI za ovaj projekat:
1. FILE-BASED - ne zahteva instalaciju database server-a
2. ZERO CONFIGURATION - automatski kreira fajl ako ne postoji
3. PORTABLE - može se pomeriti sa aplikacijom
4. LIGHTWEIGHT - idealno za development i manje aplikacije

CONNECTION STRING HIERARCHY (fallback pattern):
1. "ConnectionStrings:DefaultConnection" iz appsettings.json
2. "CONNECTION_STRING" environment varijabla (za deployment)
3. Default path "Data Source=sarajevoair-aqi.db" (za local development)

EF CORE FEATURES koje koristimo:
- Code First approach (entiteti definišu strukturu baze)
- Automatic migrations
- LINQ query syntax
*/

// Connection string sa fallback hijerarhijom za različite environment-e
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")  // Prvi pokušaj: appsettings.json
                       ?? builder.Configuration["CONNECTION_STRING"]                   // Drugi pokušaj: env var
                       ?? "Data Source=sarajevoair-aqi.db";                          // Fallback: local file

// Registruje Entity Framework DbContext u DI container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));  // SQLite provider sa connection string-om

/*
=== OPTIONS PATTERN KONFIGURACIJA ===
Options pattern je MICROSOFT RECOMMENDED WAY za konfiguraciju servisa

BENEFITS:
1. Type-safe pristup konfiguraciji (umesto stringing-ova)
2. Automatic binding iz appsettings.json
3. Validation support
4. IOptionsMonitor za runtime changes

HTTP CLIENT + POLLY RESILIENCE:
AddHttpClient() - registruje named/typed HTTP client
AddStandardResilienceHandler() - dodaje retry policies, circuit breaker, timeout
*/

// Options pattern: bind "Aqicn" sekciju iz appsettings.json na AqicnConfiguration klasu
builder.Services.Configure<AqicnConfiguration>(builder.Configuration.GetSection("Aqicn"));

/*
=== HTTP CLIENT SA RESILIENCE PATTERNS ===
Polly je .NET library za resilience patterns (retry, circuit breaker, timeout)

STANDARD RESILIENCE HANDLER uključuje:
1. RETRY POLICY - automatski ponavlja failed HTTP pozive (exponential backoff)
2. CIRCUIT BREAKER - prekida pozive ako server konstantno pada
3. TIMEOUT - prekida pozive koji traju predugo
4. BULKHEAD - limitira concurrent pozive

REAL-WORLD SCENARIO:
Ako WAQI API vrati 500 error, automatski će se pokušati ponovo 3 puta
sa delays: 2s, 4s, 8s (exponential backoff)
*/

// Registruje typed HTTP client sa Polly resilience policies
builder.Services.AddHttpClient<IAqicnClient, AqicnClient>()
    .AddStandardResilienceHandler();  // Dodaje retry, circuit breaker, timeout patterns

/*
=== DEPENDENCY INJECTION REGISTRACIJA ===
ASP.NET Core ima ugrađeni DI container koji automatski kreira i ubrizgava objekte

LIFETIME SCOPES:
1. SINGLETON - jedan objekat za celu aplikaciju (memory cache, configurations)
2. SCOPED - jedan objekat po HTTP request-u (Controllers, Services, Repositories)  
3. TRANSIENT - novi objekat svaki put kada se traži (stateless utilities)

INTERFACE-BASED DEPENDENCY INJECTION:
Registruje se Interface → Implementation mapping
Controller traži IService → DI daje Service implementaciju
OVO OMOGUĆAVA: unit testing, loose coupling, SOLID principles

LAYERED ARCHITECTURE PATTERN:
Controller → Service → Repository → Database
Svaki layer ima svoju odgovornost i ne zna za implementaciju drugih layera
*/

// SINGLETON SERVICES - žive kroz ceo lifecycle aplikacije
builder.Services.AddSingleton<AirQualityCache>();          // In-memory cache za brže response-e

// SCOPED SERVICES - jedan objekat po HTTP request-u, automatski se dispose-uju
builder.Services.AddScoped<IAqiRepository, AqiRepository>(); // Data access layer

// BUSINESS LOGIC SERVICES - svaki ima specifičnu odgovornost (Single Responsibility Principle)
builder.Services.AddScoped<IAirQualityService, AirQualityService>();           // Live AQI data processing
builder.Services.AddScoped<IForecastService, ForecastService>();               // Weather forecast logic
builder.Services.AddScoped<IHealthAdviceService, HealthAdviceService>();       // Health recommendations
builder.Services.AddScoped<IDailyAqiService, DailyAqiService>();              // Daily aggregations
builder.Services.AddScoped<ICityComparisonService, CityComparisonService>();   // Multi-city comparisons
builder.Services.AddScoped<IAqiAdminService, AqiAdminService>();              // Admin operations

/*
=== BACKGROUND SERVICES ===
HostedService radi nezavisno od HTTP request-ova, u background thread-u
KORISTI SE ZA: scheduled tasks, data collection, cleanup operations

AirQualityRefreshService:
- Pokreće se automatski kada app startuje
- Radi u background-u svakih 10 minuta
- Poziva WAQI API i čuva podatke u bazu
- Ne blokira HTTP request-ove
*/

// BACKGROUND SERVICE - radi kontinuirano u pozadini
builder.Services.AddHostedService<AirQualityRefreshService>(); // Prikuplja podatke svakih 10min

/*
=== HEALTH CHECKS ===
Health checks omogućavaju monitoring infrastrukturi (Docker, Kubernetes, Load Balancers)
da provere da li je aplikacija zdrava i može da primi request-e

BENEFITS:
1. Automatic failover u clustered environment-u
2. Monitoring i alerting
3. Graceful shutdowns
4. Load balancer može da ukloni unhealthy instance-e

ENDPOINT: GET /health vraća 200 OK ili 503 Service Unavailable
*/

// Dodaje health check servise (endpoint: /health)
builder.Services.AddHealthChecks();

// TODO: Rate limiting će biti konfigurisan kasnije za production security

/*
=== APPLICATION BUILD ===
builder.Build() kreira WebApplication instancu sa svim konfiguriranim servisima
Od ovog trenutka, ne mogu više da se dodaju servisi - samo middleware konfiguracija
*/

// Kreira konkretan WebApplication objekat sa svim konfiguriranim servisima
var app = builder.Build();

/*
=== HTTP REQUEST PIPELINE KONFIGURACIJA ===
Middleware se izvršavaju sekvencijalno za svaki HTTP request
REDOSLED JE KRITIČAN! Middleware se pozivaju odozgo nasdole

PIPELINE FLOW:
Request → Logging → Exception Handling → CORS → ... → Controller → ... → Response

MIDDLEWARE PATTERN:
Svaki middleware može da:
1. Obradi request pre sledećeg middleware-a
2. Pozove next() da prenese kontrolu
3. Obradi response nakon što se vrati iz sledećeg middleware-a
*/

/*
=== SERILOG REQUEST LOGGING ===
Automatski loguje sve HTTP request-e sa informacijama:
- HTTP method (GET, POST, PUT, DELETE)
- Request path i query parametri
- Response status code
- Response time u millisekundama
- Request size i response size

EXAMPLE LOG:
[INF] HTTP GET /api/v1/live responded 200 in 156.7ms
*/

// Aktivira automatsko logovanje svih HTTP request-ova
app.UseSerilogRequestLogging();

/*
=== GLOBAL EXCEPTION HANDLING ===
ExceptionHandlingMiddleware hvata SVE unhanded exception-e kroz celu aplikaciju
i konvertuje ih u user-friendly HTTP response-e

BENEFITS:
1. Consistent error format kroz ceo API
2. Security - ne leak-uje internal implementation details
3. Centralized error logging
4. Graceful degradation umesto crash-a
*/

// Globalni exception handler - hvata sve greške i vraća konzistentne error response-e
app.UseMiddleware<ExceptionHandlingMiddleware>();

/*
=== CORS MIDDLEWARE AKTIVACIJA ===
Mora biti pozvan NAKON UseRouting() (ako se koristi)
Koristi "FrontendOnly" policy koji je definisan ranije u konfiguraciji
*/

// Aktivira CORS policy za cross-origin request-e sa frontend-a
app.UseCors("FrontendOnly");

/*
=== DEVELOPMENT-ONLY MIDDLEWARE ===
Swagger UI se aktivira SAMO u Development environment-u
U Production-u, API dokumentacija je obično skrivena iz security razloga

SWAGGER ENDPOINTS:
- /swagger - Swagger UI interface za interactive testing
- /swagger/v1/swagger.json - OpenAPI specification u JSON formatu
*/

// Swagger middleware - SAMO za development environment
if (app.Environment.IsDevelopment())
{
    // Generiše OpenAPI JSON specification na /swagger/v1/swagger.json
    app.UseSwagger();
    
    // Interaktivni Swagger UI na /swagger
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SarajevoAir API V1");
        c.RoutePrefix = "swagger";  // Dostupno na /swagger umesto /swagger/index.html
    });
}

/*
=== STATIC FILES MIDDLEWARE ===
Servira statičke fajlove iz wwwroot foldera (CSS, JS, images)
Korisno za admin dashboard, error pages, ili API dokumentaciju

SECURITY NOTE: 
Sve što se stavi u wwwroot folder je javno dostupno!
Ne stavljati sensitive fajlove tamo
*/

// Servira statičke fajlove iz wwwroot foldera
app.UseStaticFiles();

/*
=== ROUTING MIDDLEWARE ===
Analizira URL path i određuje koji Controller/Action treba da se pozove
Mora biti pozvan PRE MapControllers()
*/

// Aktivira URL routing sistem
app.UseRouting();

/*
=== CONTROLLER MAPPING ===
Mapira HTTP request-e na Controller action metode na osnovu:
1. Route atributa na Controller-u i Action-u
2. HTTP method atributa ([HttpGet], [HttpPost], itd.)
3. Parameter binding iz URL-a, query string-a, i request body-ja

EXAMPLE:
GET /api/v1/live?city=Sarajevo 
→ LiveController.GetLiveData(city: "Sarajevo")
*/

// Mapira HTTP request-e na Controller action metode
app.MapControllers();

/*
=== HEALTH CHECKS ENDPOINT ===
/health endpoint vraća status aplikacije u JSON formatu

HEALTH CHECK STATUSES:
- Healthy: sve proverke su prošle uspešno
- Degraded: neke proverke su prošle sa upozorenjem
- Unhealthy: kritične proverke nisu prošle

RESPONSE EXAMPLE:
{
  "status": "Healthy",
  "checks": [
    {"name": "database", "status": "Healthy", "duration": "00:00:00.0234567"}
  ],
  "duration": "00:00:00.0456789"
}

INFRASTRUCTURE INTEGRATION:
Docker HEALTHCHECK, Kubernetes liveness/readiness probes, Load balancers
*/

// Mapira health check endpoint sa custom JSON response writer-om
app.MapHealthChecks("/health", new HealthCheckOptions
{
    // Custom response writer koji formatira health check rezultate u JSON
    ResponseWriter = async (context, report) =>
    {
        // Postavlja Content-Type header na JSON
        context.Response.ContentType = "application/json";
        
        // Kreira structured response objekat
        var response = new
        {
            status = report.Status.ToString(),  // Overall status: Healthy/Degraded/Unhealthy
            checks = report.Entries.Select(x => new  // Detalji za svaku individual proverku
            {
                name = x.Key,                           // Ime health check-a
                status = x.Value.Status.ToString(),     // Status ove specifične proverke
                exception = x.Value.Exception?.Message, // Error message ako je failed
                duration = x.Value.Duration.ToString()  // Vreme izvršavanja proverke
            }),
            duration = report.TotalDuration.ToString()  // Ukupno vreme svih proveravanja
        };
        
        // Serijalizuje response u JSON i šalje client-u
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

/*
=== DATABASE INITIALIZATION NA STARTUP ===
EnsureCreatedAsync() kreira bazu ako ne postoji, ali NE RADI MIGRATIONS!
Za production, trebalo bi koristiti context.Database.MigrateAsync()

SCOPE PATTERN:
CreateScope() kreira novi DI scope da dobije DbContext
using statement automatski dispose-uje scope kada se završi

STARTUP RESILIENCE:
Ako baza ne može da se inicijalizuje, aplikacija neće crashovati
već će logovati grešku i pokušati da radi bez baze
*/

// Inicijalizuje bazu podataka na startup-u aplikacije
using (var scope = app.Services.CreateScope())
{
    // Dobija DbContext iz DI container-a kroz novi scope
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Kreira bazu i tabele ako ne postoje (Code First approach)
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database ready");
    }
    catch (Exception ex)
    {
        // Loguje grešku ali ne prekida startup aplikacije
        Log.Error(ex, "Failed to initialize database");
    }
}

/*
=== APPLICATION STARTUP ===
app.Run() blokira main thread i pokreće web server
Server sluša na portovima definisanim u launchSettings.json ili --urls argumentu

GRACEFUL SHUTDOWN:
Kada se primi SIGTERM signal, aplikacija:
1. Prekida primanje novih request-ova
2. Čeka da se završe aktivni request-ovi (sa timeout-om)
3. Dispose-uje sve servise
4. Zatvara database konekcije
*/

Log.Information("SarajevoAir API starting up...");
// Pokreće web server i blokira thread dok se aplikacija ne zaustavi
app.Run();

/*
=== INTEGRATION TESTING SUPPORT ===
partial class Program omogućava integration test-ovima da referenciraju Program klasu
Potrebno za WebApplicationFactory<Program> u test projektima

EXAMPLE u test projektu:
public class ApiTestsFixture : WebApplicationFactory<Program>
{
    // Test setup
}
*/

public partial class Program { } // Omogućava integration testing