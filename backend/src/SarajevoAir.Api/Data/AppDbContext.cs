/*
=== ENTITY FRAMEWORK CORE DATABASE CONTEXT ===

HIGH LEVEL OVERVIEW:
AppDbContext je MAIN DATABASE ACCESS POINT za celu aplikaciju
Nasleđuje od DbContext klase koja je srce Entity Framework Core ORM-a

DESIGN PATTERNS:
1. UNIT OF WORK PATTERN - DbContext tracks sve changes i commit-uje ih odjednom
2. REPOSITORY PATTERN - DbSet<T> služi kao in-memory kolekcija entiteta  
3. CHANGE TRACKING - EF automatski prati izmene na objektima
4. LAZY LOADING - related data se učitava tek kada se pristupa

DATABASE PROVIDER:
Koristi SQLite provider konfigurisan u Program.cs
SQLite je file-based baza idealna za development i manje aplikacije

ENTITY REGISTRATION:
Sve database tabele moraju biti registrovane kao DbSet<T> properties
*/

using Microsoft.EntityFrameworkCore;
using SarajevoAir.Api.Entities;

namespace SarajevoAir.Api.Data;

/*
=== APPLICATION DATABASE CONTEXT ===
Glavni DbContext za SarajevoAir aplikaciju
Registruje se u DI container-u u Program.cs i injektuje u Repository klase
*/
public class AppDbContext : DbContext
{
    /*
    === CONSTRUCTOR SA DEPENDENCY INJECTION ===
    DbContextOptions se injektuje iz DI container-a i sadrži:
    - Database provider (SQLite)
    - Connection string
    - Logging configuration
    - Performance options
    
    SCOPED LIFETIME:
    DbContext je registrovan kao Scoped service što znači:
    - Jedan DbContext po HTTP request-u
    - Automatski se dispose-uje na kraju request-a
    - Thread-safe unutar jednog request-a
    */
    
    /// <summary>
    /// Konstruktor koji prima konfiguraciju iz Dependency Injection container-a
    /// </summary>
    /// <param name="options">Database provider i connection string konfiguracija</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /*
    === DATABASE TABLE REGISTRATION ===
    Svaki DbSet<T> predstavlja jednu tabelu u bazi podataka
    
    DBSET BENEFITS:
    1. CRUD operations - Add, Update, Remove, Find
    2. LINQ queries - Where, Select, Join, GroupBy
    3. Change tracking - automatski prati izmene
    4. Async support - AddAsync, SaveChangesAsync
    
    NAMING CONVENTION:
    Property name = table name u bazi (SimpleAqiRecords tabela)
    */
    
    /// <summary>
    /// Tabela za čuvanje AQI snapshots-ova po gradovima i vremenu
    /// Koristi se za historical data i analytics
    /// </summary>
    public DbSet<SimpleAqiRecord> SimpleAqiRecords => Set<SimpleAqiRecord>();

    /// <summary>
    /// Tabela za čuvanje detaljnih pollutant measurements specifično za Sarajevo
    /// Refreshuje se svakih 10 minuta iz WAQI API-ja
    /// </summary>
    public DbSet<SarajevoMeasurement> SarajevoMeasurements => Set<SarajevoMeasurement>();

    /// <summary>
    /// Tabela za čuvanje daily forecast podataka specifično za Sarajevo  
    /// Refreshuje se svakih 10 minuta iz WAQI API-ja (forecast.daily)
    /// </summary>
    public DbSet<SarajevoForecast> SarajevoForecasts => Set<SarajevoForecast>();

    /*
    === MODEL CONFIGURATION (FLUENT API) ===
    OnModelCreating se poziva jednom kada se kreira model baze
    Ovde se definiše:
    1. Table schemas i constraints
    2. Relationships između entiteta  
    3. Indexes za performance optimization
    4. Data seeding za initial data
    
    FLUENT API vs DATA ANNOTATIONS:
    Fluent API (ovde) = centralizovana konfiguracija u DbContext
    Data Annotations = atributi direktno na Entity klase
    Fluent API ima precedence nad Data Annotations
    */
    
    /// <summary>
    /// Konfiguracija database schema-e i relationships
    /// Poziva se automatski od strane EF Core-a pri kreiranju modela
    /// </summary>
    /// <param name="modelBuilder">Builder za konfiguraciju entiteta i tabela</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        /*
        === COMPOSITE INDEX ZA PERFORMANCE ===
        
        BUSINESS REASONING:
        Najčešći query patterns su:
        1. "Daj mi poslednji AQI za Sarajevo" - WHERE City = 'Sarajevo' ORDER BY Timestamp DESC
        2. "Daj mi sve AQI za period" - WHERE City = 'X' AND Timestamp BETWEEN date1 AND date2
        
        COMPOSITE INDEX (City, Timestamp):
        - Omogućava brze queries po gradu
        - Timestamp je sorted unutar svakog grada
        - Idealno za ORDER BY Timestamp operacije
        - Pokriva oba najčešća query pattern-a
        
        INDEX NAMING:
        HasDatabaseName() eksplicitno definiše ime u bazi umesto auto-generated
        */
        
        // Konfiguracija SimpleAqiRecord entiteta
        modelBuilder.Entity<SimpleAqiRecord>(entity =>
        {
            // Composite index za optimizaciju queries po gradu i vremenu
            // Pokriva patterns: WHERE City = 'X' ORDER BY Timestamp DESC
            entity.HasIndex(e => new { e.City, e.Timestamp })
                  .HasDatabaseName("IX_AqiRecords_CityTimestamp");
        });

        // Konfiguracija SarajevoMeasurement entiteta
        modelBuilder.Entity<SarajevoMeasurement>(entity =>
        {
            // Index po Timestamp za brže queries (ORDER BY Timestamp DESC)
            entity.HasIndex(e => e.Timestamp)
                  .HasDatabaseName("IX_SarajevoMeasurements_Timestamp");
        });

        // Konfiguracija SarajevoForecast entiteta
        modelBuilder.Entity<SarajevoForecast>(entity =>
        {
            // Unique index po datumu (jedan forecast po danu)
            entity.HasIndex(e => e.Date)
                  .IsUnique()
                  .HasDatabaseName("IX_SarajevoForecast_Date");
        });
        
        /*
        ADDITIONAL CONFIGURATIONS koje bi mogle biti dodane:
        
        // Unique constraints
        entity.HasIndex(e => new { e.City, e.Timestamp }).IsUnique();
        
        // String length limits  
        entity.Property(e => e.City).HasMaxLength(100);
        
        // Decimal precision
        entity.Property(e => e.Value).HasPrecision(18, 2);
        
        // Default values
        entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
        */
    }
    
    /*
    USAGE EXAMPLES iz Repository-ja:
    
    // Dodavanje novog record-a
    await _context.SimpleAqiRecords.AddAsync(newRecord);
    await _context.SaveChangesAsync();
    
    // Query sa LINQ
    var latestAqi = await _context.SimpleAqiRecords
        .Where(x => x.City == "Sarajevo")
        .OrderByDescending(x => x.Timestamp)
        .FirstOrDefaultAsync();
    
    // Bulk operations
    var records = await _context.SimpleAqiRecords
        .Where(x => x.Timestamp > DateTime.UtcNow.AddDays(-7))
        .ToListAsync();
    */
}
