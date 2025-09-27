/*
=== WAQI (World Air Quality Index) API CONFIGURATION ===

HIGH LEVEL OVERVIEW:
Ova klasa definiše konfiguraciju za spajanje na WAQI API (api.waqi.info)
Koristi se kroz Options Pattern u ASP.NET Core

DESIGN PATTERNS:
1. OPTIONS PATTERN - type-safe binding iz appsettings.json
2. STATIC CONFIGURATION - city-to-station mapping za performance
3. FALLBACK VALUES - default prazan string umesto null reference exceptions

INTEGRATION FLOW:
appsettings.json → IConfiguration → AqicnConfiguration → AqicnClient → HTTP API

LOW LEVEL DETAILS:
- Properties se bind-uju automatski iz "Aqicn" sekcije u appsettings.json
- CityStations Dictionary omogućava O(1) lookup performanse
- StringComparer.OrdinalIgnoreCase čini gradove case-insensitive
*/

namespace SarajevoAir.Api.Configuration;

/*
=== WAQI API CONFIGURATION CLASS ===
Ova klasa se koristi za type-safe pristup WAQI API konfiguraciji
Registruje se u Program.cs sa: Configure<AqicnConfiguration>(config.GetSection("Aqicn"))
*/
public class AqicnConfiguration
{
    /*
    === API CONNECTION PROPERTIES ===
    Ove properties se automatski populate-uju iz appsettings.json:
    
    appsettings.json:
    {
      "Aqicn": {
        "ApiUrl": "https://api.waqi.info",
        "ApiToken": "your-api-token-here",
        "City": "sarajevo"
      }
    }
    */
    
    /// <summary>
    /// Base URL za WAQI API - obično "https://api.waqi.info"
    /// Koristi se kao prefix za sve API pozive
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API token za autentifikaciju sa WAQI servisom
    /// Dobija se registracijom na https://aqicn.org/data-platform/token/
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Default grad za background service koji automatski prikuplja podatke
    /// Obično "sarajevo" jer je to glavni grad aplikacije
    /// </summary>
    public string City { get; set; } = string.Empty;
    
    /*
    === CITY-TO-STATION MAPPING ===
    WAQI API zahteva specifične station ID-jeve umesto imena gradova
    
    STATION ID TYPES:
    1. @XXXXX format - javni station ID (npr. @9265 za Sarajevo Ivan Sedlo)
    2. AXXXXXX format - alternative station format (npr. A462985 za Tuzla)
    
    PERFORMANCE OPTIMIZATION:
    - static readonly = kreiran jednom pri load-u klase, deli se među svim instance-ovima
    - Dictionary<string,string> = O(1) lookup complexity
    - StringComparer.OrdinalIgnoreCase = "Sarajevo", "sarajevo", "SARAJEVO" su isti ključ
    
    BUSINESS LOGIC:
    Korisnik poziva /api/v1/live?city=Tuzla
    → AqicnClient gleda CityStations["Tuzla"] = "A462985"
    → Poziva https://api.waqi.info/feed/A462985/?token=xxx
    */
    
    /// <summary>
    /// Static mapping gradova na njihove specifične WAQI station ID-jeve
    /// Koristi se u AqicnClient-u za konvertovanje imena grada u station ID
    /// </summary>
    public static readonly Dictionary<string, string> CityStations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Sarajevo", "@10557" },    // Sarajevo correct station
        { "Tuzla", "@9321" },        // Tuzla Bukinje station with forecast  
        { "Mostar", "@14726" },      // Mostar bijeli brijeg station
        { "Vitez", "A475627" },      // Vitez LUCIUS station
        { "Zenica", "@9267" },       // Zenica Centar station
        { "Bihac", "@13578" }        // Bihać nova četvrt station
    };
}