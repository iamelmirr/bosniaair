/*
=== ENTITY FRAMEWORK CORE ENTITY MODEL ===

HIGH LEVEL OVERVIEW:
SimpleAqiRecord je DATABASE ENTITY klasa koja predstavlja jednu tabelu u bazi
Koristi se za čuvanje historical AQI snapshots-ova po gradovima

DESIGN DECISIONS:
1. SIMPLE STRUCTURE - samo najvažnije informacije (City, AQI, Time)
2. DENORMALIZED - ne koristi foreign keys radi jednostavnosti
3. UTC TIMESTAMPS - izbegava timezone probleme
4. AUTO-INCREMENT PK - standardni pristup za audit tabele

TABLE PURPOSE:
- Historical tracking AQI vrednosti  
- Analytics i trending
- Backup podataka iz external API-ja
- Performance - ne zavisi od live API-ja za historical data

ENTITY vs DTO PATTERN:
Entity = database representation (ova klasa)
DTO = API response model (različite klase u Models folderu)
*/

using System.ComponentModel.DataAnnotations;

namespace SarajevoAir.Api.Entities;

/*
=== AQI HISTORICAL RECORD ENTITY ===
Predstavlja jedan snapshot AQI vrednosti za određeni grad u određeno vreme
Mapira se na "SimpleAqiRecords" tabelu u bazi podataka
*/
public class SimpleAqiRecord
{
    /*
    === PRIMARY KEY ===
    [Key] data annotation označava da je ovo primary key kolona
    
    ALTERNATIVE APPROACHES:
    1. Data Annotation: [Key] (trenutni pristup)
    2. Fluent API: modelBuilder.Entity<T>().HasKey(x => x.Id)
    3. Convention: property named "Id" ili "ClassNameId" se automatski tretira kao PK
    
    AUTO-INCREMENT:
    EF Core automatski konfigurira INTEGER PRIMARY KEY kao auto-increment u SQLite
    */
    
    /// <summary>
    /// Jedinstveni identifikator record-a - auto-increment primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /*
    === TIMESTAMP FIELD ===
    
    UTC BEST PRACTICE:
    Uvek koristiti UTC za database timestamps da se izbegnu timezone problemi
    DateTime.UtcNow = trenutno UTC vreme bez timezone offset-a
    
    DEFAULT VALUE:
    = DateTime.UtcNow postavlja default vrednost ako se eksplicitno ne setuje
    Korisno za audit purposes - svaki record automatski ima timestamp
    
    INDEXING:
    Ovo polje je deo composite index-a (City, Timestamp) definisanog u AppDbContext
    */
    
    /// <summary>
    /// UTC timestamp kada je AQI vrednost zabeležena
    /// Default vrednost je trenutno vreme kada se kreira objekat
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /*
    === AQI VALUE FIELD ===
    
    AQI RANGE:
    AQI (Air Quality Index) je standardizovana skala 0-500+
    0-50 = Good
    51-100 = Moderate  
    101-150 = Unhealthy for Sensitive Groups
    151-200 = Unhealthy
    201-300 = Very Unhealthy
    301-500 = Hazardous
    
    DATA TYPE:
    int je dovoljno jer AQI je ceo broj u praktičnim slučajevima
    */
    
    /// <summary>
    /// AQI (Air Quality Index) vrednost - ceo broj obično između 0-500
    /// Veća vrednost = gora kvaliteta vazduha
    /// </summary>
    public int AqiValue { get; set; }

    /*
    === CITY FIELD ===
    
    DENORMALIZATION DECISION:
    Čuvamo ime grada kao string umesto foreign key reference
    PREDNOSTI: jednostavnost, performance, nezavisnost od City master tabele
    MANE: moguće duplicate/inconsistent names, veća storage potreba
    
    DEFAULT VALUE:
    = "Sarajevo" jer je to glavni grad aplikacije
    Osigurava da nema null reference exceptions
    
    CASE SENSITIVITY:
    Queries po gradu koriste StringComparer.OrdinalIgnoreCase u servisima
    */
    
    /// <summary>
    /// Ime grada za koji je zabeleška AQI vrednost
    /// Default je "Sarajevo" kao glavni grad aplikacije
    /// </summary>
    public string City { get; set; } = "Sarajevo";
}

/*
USAGE EXAMPLES:

// Kreiranje novog record-a
var record = new SimpleAqiRecord 
{
    City = "Tuzla",
    AqiValue = 67,
    // Timestamp se automatski setuje na DateTime.UtcNow
};

// Repository usage
await _context.SimpleAqiRecords.AddAsync(record);
await _context.SaveChangesAsync();

// Query examples
var latest = await _context.SimpleAqiRecords
    .Where(x => x.City == "Sarajevo")
    .OrderByDescending(x => x.Timestamp)
    .FirstOrDefaultAsync();

var weeklyData = await _context.SimpleAqiRecords
    .Where(x => x.City == "Sarajevo" && x.Timestamp > DateTime.UtcNow.AddDays(-7))
    .OrderBy(x => x.Timestamp)
    .ToListAsync();
*/
