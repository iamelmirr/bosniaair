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

    /// <summary>
    /// Vraća sve AQI record-e u bazi (admin functionality)
    /// PAŽNJA: može biti spor za velike baze podataka
    /// </summary>
    Task<IReadOnlyList<SimpleAqiRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    /*
    === DELETE OPERATION ===
    */
    
    /// <summary>
    /// Briše AQI record po ID-u (admin functionality)
    /// Silently ignoriše ako record ne postoji
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
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
            .ToListAsync(cancellationToken);       // FULL TABLE SCAN - pažnja na performance!
    }

    /*
    === DELETE OPERATION ===
    
    SAFE DELETE PATTERN:
    1. Prvo Find() da proveri da li record postoji
    2. Ako ne postoji, silently return (idempotent operation)
    3. Ako postoji, Remove() + SaveChanges()
    
    FINDVS WHERE DIFFERENCE:
    FindAsync() koristi primary key lookup (najbrža operacija)
    Where() generiše punu query sa WHERE clause
    */
    
    /// <summary>
    /// Briše AQI record po ID-u (admin functionality)
    /// Idempotent operation - ne baca grešku ako record ne postoji
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Primary key lookup - najbrža operacija u bazi
        var record = await _context.SimpleAqiRecords.FindAsync(new object[] { id }, cancellationToken);
        
        // Idempotent behavior - ne baca exception ako record ne postoji
        if (record is null)
        {
            return;
        }

        // Označava entity kao "Deleted" u change tracker-u
        _context.SimpleAqiRecords.Remove(record);
        
        // Generiše i izvršava SQL DELETE statement
        // SQL: DELETE FROM SimpleAqiRecords WHERE Id = @id
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/*
=== REPOSITORY PATTERN BENEFITS SUMMARY ===

1. TESTABILITY:
   Mock IAqiRepository u unit testovima umesto stvarne baze
   
2. SEPARATION OF CONCERNS:
   Service layer ne zna ništa o SQL-u ili EF Core-u
   
3. CONSISTENCY:
   Svi database operations idu preko uniform API-ja
   
4. PERFORMANCE OPTIMIZATION:
   Centralizovane query optimizations (AsNoTracking, indexes)
   
5. MAINTAINABILITY:
   Lako menjati database logic bez uticaja na business layer

USAGE EXAMPLE iz Service-a:
public class AirQualityService 
{
    private readonly IAqiRepository _repository;
    
    public async Task<int?> GetLatestAqi(string city)
    {
        var latest = await _repository.GetMostRecentAsync(city);
        return latest?.AqiValue;
    }
}
*/
