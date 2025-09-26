/*
=== REPOSITORY PATTERN IMPLEMENTATION ===

HIGH LEVEL OVERVIEW:
Repository pattern enkapsulira data access logiku i pruža clean interface za CRUD operations
Odvaja business logiku (Services) od database detalja (Entity Framework)

DESIGN BENEFITS:
1. SEPARATION OF CONCERNS - Service layer ne zna ništa o EF Core ili SQL-u
2. TESTABILITY - lako mock-ovati repository u unit testovima
3. ABSTRACTION - možemo zameniti EF Core sa drugim ORM-om bez menjanja Service-a
4. CONSISTENCY - centralizovane database operations sa uniform API-jem

INTERFACE-FIRST DESIGN:
IAqiRepository definiše contract, AqiRepository implementira
Service layer zavisi samo od interface-a (Dependency Inversion Principle)

ASYNC PATTERNS:
Sve metode su async sa CancellationToken support za scalability
*/

using Microsoft.EntityFrameworkCore;
using SarajevoAir.Api.Data;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Utilities;

namespace SarajevoAir.Api.Repositories;

/*
=== AQI REPOSITORY INTERFACE ===
Definiše contract za AQI data access operations
Service layer koristi ovaj interface, ne konkretnu implementaciju

DEPENDENCY INVERSION PRINCIPLE:
High-level modules (Services) ne treba da zavise od low-level modules (Repositories)
Oba treba da zavise od abstractions (interfaces)
*/
public interface IAqiRepository
{
    /*
    === CREATE OPERATION ===
    Dodaje novi AQI record u bazu podataka
    
    PARAMETERS:
    - record: Entity objekat za dodavanje
    - cancellationToken: omogućava prekid operacije (HTTP request cancel, shutdown)
    
    BUSINESS USE CASE:
    Background service poziva ovu metodu svakih 10 minuta da sačuva novi AQI snapshot
    */
    
    /// <summary>
    /// Dodaje novi AQI record u bazu podataka
    /// </summary>
    Task AddRecordAsync(SimpleAqiRecord record, CancellationToken cancellationToken = default);

    /*
    === READ OPERATIONS - VARIOUS QUERY PATTERNS ===
    */
    
    /// <summary>
    /// Vraća najnoviji AQI record za određeni grad
    /// Koristi se za live AQI display na frontend-u
    /// </summary>
    Task<SimpleAqiRecord?> GetMostRecentAsync(string city, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vraća poslednje N AQI record-ova za grad (obrnuto hronološki)
    /// Koristi se za kratak historical timeline
    /// </summary>
    Task<IReadOnlyList<SimpleAqiRecord>> GetRecentAsync(string city, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vraća sve AQI record-e za grad od određenog datuma (hronološki sortiran)
    /// Koristi se za dnevni/nedeljni/mesečni analytics
    /// </summary>
    Task<IReadOnlyList<SimpleAqiRecord>> GetRangeAsync(string city, DateTime fromUtc, CancellationToken cancellationToken = default);

    /*
    === SARAJEVO-SPECIFIC OPERATIONS ===
    Nove metode za measurements i forecast podatke specifične za Sarajevo
    */

    /// <summary>
    /// Dodaje novi measurements record za Sarajevo
    /// </summary>
    Task AddSarajevoMeasurementAsync(SarajevoMeasurement measurement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vraća najnoviji measurements record za Sarajevo
    /// </summary>
    Task<SarajevoMeasurement?> GetLatestSarajevoMeasurementAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaje ili ažurira forecast record za Sarajevo na određeni datum
    /// </summary>
    Task UpsertSarajevoForecastAsync(SarajevoForecast forecast, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vraća forecast podatke za Sarajevo za sledeće dane (počevši od danas)
    /// </summary>
    Task<IReadOnlyList<SarajevoForecast>> GetSarajevoForecastAsync(int daysCount = 5, CancellationToken cancellationToken = default);
}

/*
=== CONCRETE REPOSITORY IMPLEMENTATION ===
Implementira IAqiRepository interface koristeći Entity Framework Core
Ova klasa sadrži stvarne SQL query operacije preko EF Core LINQ provider-a
*/
public class AqiRepository : IAqiRepository
{
    /*
    === DEPENDENCY INJECTION ===
    DbContext se injektuje kroz konstruktor iz DI container-a
    
    SCOPED LIFETIME:
    - AppDbContext je registrovan kao Scoped service u Program.cs
    - Jedan DbContext po HTTP request-u
    - Automatski dispose na kraju request-a
    - Thread-safe unutar jednog request-a
    */
    private readonly AppDbContext _context;

    /// <summary>
    /// Konstruktor prima DbContext iz Dependency Injection container-a
    /// </summary>
    public AqiRepository(AppDbContext context)
    {
        _context = context;
    }

    /*
    === CREATE OPERATION IMPLEMENTATION ===
    
    EF CORE CHANGE TRACKING:
    1. Add() označava entity kao "Added" u change tracker-u
    2. SaveChangesAsync() generiše i izvršava INSERT SQL statement
    3. Id se automatski populate-uje nakon save (IDENTITY column)
    
    TRANSACTION BEHAVIOR:
    SaveChangesAsync() automatski wrappuje sve changes u transaction
    Ako bilo koja operacija ne uspe, sve se rollback-uje
    */
    
    /// <summary>
    /// Dodaje novi AQI record u bazu podataka
    /// EF Core automatski generiše INSERT SQL statement
    /// </summary>
    public async Task AddRecordAsync(SimpleAqiRecord record, CancellationToken cancellationToken = default)
    {
        // Dodaje entity u EF change tracker kao "Added"
        _context.SimpleAqiRecords.Add(record);
        
        // Generiše i izvršava SQL INSERT statement
        // SQL: INSERT INTO SimpleAqiRecords (Timestamp, AqiValue, City) VALUES (?, ?, ?)
        await _context.SaveChangesAsync(cancellationToken);
    }

    /*
    === PERFORMANCE OPTIMIZED READ OPERATION ===
    
    ASNOTRACKING OPTIMIZATION:
    AsNoTracking() govori EF Core-u da ne prati changes na return-ovanim objektima
    BENEFITS: 
    - 30-40% brža query performance
    - Manja memory consumption 
    - Bez change detection overhead
    USE WHEN: read-only scenarios (što je slučaj za historical data)
    
    LINQ TO SQL TRANSLATION:
    EF Core prevodi LINQ u SQL:
    Where(r => r.City == city) → WHERE City = @p0
    OrderByDescending(r => r.Timestamp) → ORDER BY Timestamp DESC  
    FirstOrDefaultAsync() → TOP 1 ili LIMIT 1
    */
    
    /// <summary>
    /// Vraća najnoviji AQI record za grad - optimizovano za performance
    /// </summary>
    public async Task<SimpleAqiRecord?> GetMostRecentAsync(string city, CancellationToken cancellationToken = default)
    {
        // GENERATED SQL: 
        // SELECT Id, Timestamp, AqiValue, City FROM SimpleAqiRecords 
        // WHERE City = @city ORDER BY Timestamp DESC LIMIT 1
        return await _context.SimpleAqiRecords
            .AsNoTracking()                           // Performance optimization - no change tracking
            .Where(r => r.City == city)              // Filter po gradu
            .OrderByDescending(r => r.Timestamp)     // Sort po vremenu (najnoviji prvi)
            .FirstOrDefaultAsync(cancellationToken); // Uzmi prvi ili null ako nema
    }

    /*
    === LIMITED RECENT RECORDS QUERY ===
    
    BUSINESS USE CASE: Timeline widget na frontend-u koji prikazuje poslednje N merenja
    
    TAKE() OPTIMIZATION:
    Take(count) se prevodi u SQL LIMIT clause što je vrlo efikasno
    Baza vraća samo potreban broj redova umesto svih pa filtering u memory
    */
    
    /// <summary>
    /// Vraća poslednje N AQI record-ova za grad (najnoviji prvo)
    /// Koristi se za timeline widgets i kratak historical view
    /// </summary>
    public async Task<IReadOnlyList<SimpleAqiRecord>> GetRecentAsync(string city, int count, CancellationToken cancellationToken = default)
    {
        // GENERATED SQL:
        // SELECT Id, Timestamp, AqiValue, City FROM SimpleAqiRecords 
        // WHERE City = @city ORDER BY Timestamp DESC LIMIT @count
        return await _context.SimpleAqiRecords
            .AsNoTracking()                           // Performance optimization
            .Where(r => r.City == city)              // Filter po gradu  
            .OrderByDescending(r => r.Timestamp)     // Najnoviji prvi
            .Take(count)                             // Limituj na N record-ova
            .ToListAsync(cancellationToken);         // Materijalizuj u List<T>
    }

    /*
    === DATE RANGE QUERY ===
    
    BUSINESS USE CASE: Daily/Weekly/Monthly analytics, trending charts
    
    COMPOUND WHERE CLAUSE:
    city && r.Timestamp >= fromUtc se prevodi u SQL WHERE sa AND operatorom
    Database index (City, Timestamp) efikasno pokriva ovaj query pattern
    
    SORT ORDER:
    OrderBy (ascending) jer trebamo chronological order za trend charts
    */
    
    /// <summary>
    /// Vraća sve AQI record-e za grad od određenog datuma (chronologically sorted)  
    /// Koristi se za analytics i trend analysis
    /// </summary>
    public async Task<IReadOnlyList<SimpleAqiRecord>> GetRangeAsync(string city, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        // GENERATED SQL:
        // SELECT Id, Timestamp, AqiValue, City FROM SimpleAqiRecords 
        // WHERE City = @city AND Timestamp >= @fromUtc ORDER BY Timestamp ASC
        return await _context.SimpleAqiRecords
            .AsNoTracking()                                      // Performance optimization
            .Where(r => r.City == city && r.Timestamp >= fromUtc) // Filter po gradu i datumu
            .OrderBy(r => r.Timestamp)                           // Chronological order (oldest first)
            .ToListAsync(cancellationToken);                     // Materijalizuj u List<T>
    }

    /*
    === ADMIN QUERY - FULL TABLE SCAN ===
    
    WARNING: Ova query može biti SPORA za velike baze podataka!
    Koristi se samo za admin dashboard gde performance nije kritična
    
    SCALABILITY CONCERN:
    U production-u, treba dodati paginaciju:
    .Skip(pageSize * pageNumber).Take(pageSize)
    */
    
    /// <summary>
    /// Vraća SVE AQI record-e u bazi (admin functionality)
    /// PAŽNJA: Može biti sporo za velike datasets - koristiti samo za admin
    /// </summary>
    public async Task<IReadOnlyList<SimpleAqiRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // GENERATED SQL:
        // SELECT Id, Timestamp, AqiValue, City FROM SimpleAqiRecords ORDER BY Timestamp DESC
        return await _context.SimpleAqiRecords
            .AsNoTracking()                        // Performance optimization
            .OrderByDescending(r => r.Timestamp)   // Najnoviji prvi
            .ToListAsync(cancellationToken);                     // Materijalizuj u List<T>
    }

    /*
    === SARAJEVO-SPECIFIC IMPLEMENTATIONS ===
    Implementacije novih metoda za Sarajevo measurements i forecast podatke
    */

    /// <summary>
    /// Dodaje novi measurements record za Sarajevo
    /// </summary>
    public async Task AddSarajevoMeasurementAsync(SarajevoMeasurement measurement, CancellationToken cancellationToken = default)
    {
        _context.SarajevoMeasurements.Add(measurement);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Vraća najnoviji measurements record za Sarajevo
    /// </summary>
    public async Task<SarajevoMeasurement?> GetLatestSarajevoMeasurementAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SarajevoMeasurements
            .AsNoTracking()
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Dodaje ili ažurira forecast record za Sarajevo na određeni datum
    /// Koristi upsert pattern - update ako postoji, insert ako ne postoji
    /// </summary>
    public async Task UpsertSarajevoForecastAsync(SarajevoForecast forecast, CancellationToken cancellationToken = default)
    {
        // Pokušaj da nađeš postojeći record za taj datum
        var existing = await _context.SarajevoForecasts
            .FirstOrDefaultAsync(f => f.Date.Date == forecast.Date.Date, cancellationToken);

        if (existing != null)
        {
            // Ažuriraj postojeći record
            existing.Aqi = forecast.Aqi;
            existing.Pm25Min = forecast.Pm25Min;
            existing.Pm25Max = forecast.Pm25Max;
            existing.Pm25Avg = forecast.Pm25Avg;
            existing.CreatedAt = TimeZoneHelper.GetSarajevoTime();
        }
        else
        {
            // Dodaj novi record
            _context.SarajevoForecasts.Add(forecast);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Vraća forecast podatke za Sarajevo za sledeće dane (počevši od sutrašnjeg dana)
    /// </summary>
    public async Task<IReadOnlyList<SarajevoForecast>> GetSarajevoForecastAsync(int daysCount = 5, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        
        return await _context.SarajevoForecasts
            .AsNoTracking()
            .Where(f => f.Date > today)
            .OrderBy(f => f.Date)
            .Take(daysCount)
            .ToListAsync(cancellationToken);
    }
}
