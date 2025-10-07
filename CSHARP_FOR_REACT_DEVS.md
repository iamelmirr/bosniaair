# C# za React Developere - Brzi Cheat Sheet 🚀

> Sve što React/TypeScript developer treba znati za C#/.NET razgovor

## 1. Sintaksa - Paralela sa TypeScript

### Varijable i Tipovi

```typescript
// TypeScript/React
const name: string = "Sarajevo";
let count: number = 42;
const isActive: boolean = true;
const data: string | null = null;
```

```csharp
// C#
string name = "Sarajevo";        // explicit type
var count = 42;                  // type inference (isto kao 'int count = 42')
bool isActive = true;
string? data = null;             // ? znači nullable
```

**Key points:**
- `const` → `var` (read-only) ili `string/int/bool` (explicit)
- `let` → `var` (mutable, type inference)
- `string | null` → `string?` (nullable reference type)

### Funkcije

```typescript
// TypeScript
async function fetchData(city: string): Promise<AqiData> {
    const response = await fetch(`/api/${city}`);
    return response.json();
}

// Arrow function
const getCity = (id: number): string => {
    return cities[id];
};
```

```csharp
// C#
public async Task<AqiData> FetchData(string city)
{
    var response = await _httpClient.GetAsync($"/api/{city}");
    return await response.Content.ReadFromJsonAsync<AqiData>();
}

// Lambda (inline)
var getCity = (int id) => cities[id];
```

**Key points:**
- `Promise<T>` → `Task<T>`
- `async/await` radi identično!
- Template literals `` `text ${var}` `` → String interpolation `$"text {var}"`
- `=>` arrow functions postoje i u C#!

### Klase

```typescript
// TypeScript/React
export class AirQualityService {
    private apiKey: string;
    
    constructor(apiKey: string) {
        this.apiKey = apiKey;
    }
    
    async getAqi(city: string): Promise<number> {
        // ...
    }
}
```

```csharp
// C#
public class AirQualityService
{
    private readonly string _apiKey;
    
    public AirQualityService(string apiKey)  // Constructor
    {
        _apiKey = apiKey;
    }
    
    public async Task<int> GetAqi(string city)
    {
        // ...
    }
}
```

**Key points:**
- `export class` → `public class`
- `private` postoji, ali dodaj `readonly` ako ne mijenja vrijednost
- Konvencija: private fields počinju sa `_underscore`

## 2. Array Methods = LINQ

### JavaScript/TypeScript Array Methods

```typescript
// TypeScript
const cities = ["Sarajevo", "Banja Luka", "Mostar"];

// Filter
const longNames = cities.filter(c => c.length > 7);

// Map
const lengths = cities.map(c => c.length);

// Find
const sarajevo = cities.find(c => c === "Sarajevo");

// Some/Every
const hasLongName = cities.some(c => c.length > 10);
const allShort = cities.every(c => c.length < 20);
```

```csharp
// C# LINQ
var cities = new List<string> { "Sarajevo", "Banja Luka", "Mostar" };

// Filter → Where
var longNames = cities.Where(c => c.Length > 7);

// Map → Select
var lengths = cities.Select(c => c.Length);

// Find → FirstOrDefault
var sarajevo = cities.FirstOrDefault(c => c == "Sarajevo");

// Some → Any
var hasLongName = cities.Any(c => c.Length > 10);

// Every → All
var allShort = cities.All(c => c.Length < 20);
```

**Cheat Sheet:**
- `.filter()` → `.Where()`
- `.map()` → `.Select()`
- `.find()` → `.FirstOrDefault()`
- `.some()` → `.Any()`
- `.every()` → `.All()`

### U tvom projektu:

```csharp
// AqiRepository.cs
var records = await _context.AirQualityRecords
    .Where(r => r.City == city)                    // filter
    .OrderByDescending(r => r.Timestamp)           // sort
    .ToListAsync();                                // execute query
```

## 3. Null Handling

```typescript
// TypeScript
const data = apiResponse ?? defaultValue;        // Nullish coalescing
const name = user?.profile?.name;                // Optional chaining
if (data !== null && data !== undefined) { }     // Null check
```

```csharp
// C#
var data = apiResponse ?? defaultValue;          // Isto!
var name = user?.Profile?.Name;                  // Isto!
if (data != null) { }                            // Null check
```

**Key point:** `??` i `?.` rade identično kao u TypeScript!

## 4. Dependency Injection (Nema u React-u!)

### React način (Context/Props)

```typescript
// React
import { useContext } from 'react';

function MyComponent() {
    const service = useContext(ServiceContext);  // Manual wiring
    const data = service.getData();
}
```

### C# način (Automatic!)

```csharp
// Program.cs - REGISTRUJ servise
builder.Services.AddScoped<IAirQualityService, AirQualityService>();
builder.Services.AddScoped<IAqiRepository, AqiRepository>();

// AirQualityController.cs - KORISTI automatski
public class AirQualityController : ControllerBase
{
    private readonly IAirQualityService _service;
    
    // Framework AUTOMATSKI injektuje service!
    public AirQualityController(IAirQualityService service)
    {
        _service = service;
    }
}
```

**Zašto je ovo važno:**
- Testiranje - lako mockuješ servise
- Loose coupling - promijeniš implementaciju bez mijenjanja controllera
- Lifecycle management - framework se brine o tome

**Lifetimes:**
- `AddSingleton` - jedna instanca za cijelu aplikaciju (kao React Context)
- `AddScoped` - jedna instanca po HTTP requestu
- `AddTransient` - nova instanca svaki put

## 5. Async/Await - Identično!

```typescript
// TypeScript
async function loadData() {
    try {
        const data = await fetchApi();
        console.log(data);
    } catch (error) {
        console.error(error);
    }
}
```

```csharp
// C# - ISTO!
public async Task LoadData()
{
    try
    {
        var data = await FetchApi();
        Console.WriteLine(data);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}
```

**Jedina razlika:** `Promise<T>` → `Task<T>`

## 6. Interface (TypeScript vs C#)

```typescript
// TypeScript
interface IAirQualityService {
    getAqi(city: string): Promise<number>;
}

class AirQualityService implements IAirQualityService {
    async getAqi(city: string): Promise<number> {
        // implementation
    }
}
```

```csharp
// C# - Gotovo identično!
public interface IAirQualityService
{
    Task<int> GetAqi(string city);
}

public class AirQualityService : IAirQualityService
{
    public async Task<int> GetAqi(string city)
    {
        // implementation
    }
}
```

**U tvom projektu:** Koristiš `IAirQualityService` i `IAqiRepository` interfacese za DI.

## 7. HTTP Requests

```typescript
// TypeScript/React (fetch API)
const response = await fetch('https://api.waqi.info/feed/sarajevo/', {
    headers: { 'Authorization': `Bearer ${token}` }
});
const data = await response.json();
```

```csharp
// C# (HttpClient)
var response = await _httpClient.GetAsync("https://api.waqi.info/feed/sarajevo/");
var data = await response.Content.ReadFromJsonAsync<WaqiResponse>();
```

**Key point:** Umjesto `.json()` koristiš `ReadFromJsonAsync<T>()`

## 8. Modifiers - Access Levels

```csharp
public class MyClass          // Dostupno svuda
{
    public string Name;       // Dostupno svuda
    private int _id;          // Samo unutar ove klase
    protected bool isActive;  // Ova klasa + child classes
    internal void Log() { }   // Samo unutar ovog projekta
}
```

**Pravilo:** Ako ne znaš što staviti, stavi `public` za metode/properties, `private` za fields.

## 9. Properties (Getters/Setters)

```typescript
// TypeScript
class User {
    private _name: string;
    
    get name(): string {
        return this._name;
    }
    
    set name(value: string) {
        this._name = value;
    }
}
```

```csharp
// C# - Kraći način!
public class User
{
    public string Name { get; set; }           // Auto-property
    public int Age { get; private set; }       // Read-only outside class
    public string Email { get; init; }         // Set only in constructor
}
```

**U tvom projektu:**

```csharp
public class AirQualityRecord
{
    public int Id { get; set; }
    public City City { get; set; }
    public int Aqi { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 10. Tvoj Projekat - Key Patterns

### Pattern 1: Background Service

```csharp
// Radi automatski svkih 10 minuta
public class AirQualityScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Refresh data za sve gradove
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
```

**Objasni ovako:** "Kao setInterval u JavaScriptu, ali robusnije - ugrađeno u .NET"

### Pattern 2: Repository Pattern

```csharp
// Controller → Service → Repository → Database

// 1. Controller (primi HTTP request)
[HttpGet("latest/{city}")]
public async Task<ActionResult<LiveAqiResponse>> GetLatestAqi(City city)
{
    return Ok(await _service.GetLatestAqi(city));
}

// 2. Service (business logic)
public async Task<LiveAqiResponse> GetLatestAqi(City city)
{
    var record = await _repository.GetLatestAqi(city);
    return MapToResponse(record);
}

// 3. Repository (data access)
public async Task<AirQualityRecord> GetLatestAqi(City city)
{
    return await _context.AirQualityRecords
        .Where(r => r.City == city)
        .OrderByDescending(r => r.Timestamp)
        .FirstOrDefaultAsync();
}
```

**Objasni ovako:** "Separation of concerns - kao da imaš API route, service layer, i database query odvojeno"

### Pattern 3: Entity Framework (ORM)

```csharp
// Kao Prisma u Node.js

// Dodaj novi record
_context.AirQualityRecords.Add(newRecord);
await _context.SaveChangesAsync();

// Query
var records = await _context.AirQualityRecords
    .Where(r => r.City == City.Sarajevo)
    .ToListAsync();
```

**Objasni ovako:** "ORM kao Prisma - mapira C# objekte na SQL tabele automatski"

## 11. Česte Greške React Dev → C#

### ❌ Greška 1: Zaboraviti await

```csharp
// ❌ KRIVO
var data = _repository.GetData();  // Returns Task<Data>, ne Data!

// ✅ DOBRO
var data = await _repository.GetData();
```

### ❌ Greška 2: Null reference

```csharp
// ❌ KRIVO
var name = user.Profile.Name;  // NullReferenceException ako je Profile null

// ✅ DOBRO
var name = user?.Profile?.Name ?? "Unknown";
```

### ❌ Greška 3: Zaboraviti async u signaturi

```csharp
// ❌ KRIVO
public Task<Data> GetData()  // Bez async
{
    return _repository.GetData();  // OK, ali bolje sa async
}

// ✅ DOBRO
public async Task<Data> GetData()
{
    return await _repository.GetData();
}
```

## 12. VS Code Extensions za C#

Ako već nisi instalirao:

1. **C# Dev Kit** - IntelliSense, debugging
2. **C#** - Syntax highlighting
3. **NuGet Package Manager** - Dependency management

## 13. Brze Komande (kao npm)

```bash
# NPM equivalent
npm install <package>          → dotnet add package <PackageName>
npm run start                  → dotnet run
npm run build                  → dotnet build
npm test                       → dotnet test
npm run dev                    → dotnet watch run
```

## 14. Interview Quick Answers

**Q: "Razlika između var i explicit type?"**
- `var count = 42;` - compiler odredi tip (type inference)
- `int count = 42;` - explicit type
- Oba su strongly typed, var nije kao JS var!

**Q: "Što je Task<T>?"**
- Isto kao `Promise<T>` u TypeScriptu
- Predstavlja asinkronu operaciju koja vraća rezultat tipa T

**Q: "Zašto koristiš interface?"**
- Dependency Injection - lako mockujem za testove
- Loose coupling - mogu promijeniti implementaciju
- Contract - definiram što service mora imati

**Q: "Što je AddScoped?"**
- Lifetime management za DI
- Scoped = nova instanca za svaki HTTP request
- Singleton = jedna instanca za cijelu aplikaciju
- Transient = nova instanca svaki put

**Q: "Kako radi Background Service?"**
- Nasljeđuje BackgroundService klasu
- ExecuteAsync metoda radi neovisno od HTTP requesta
- Koristi IServiceScopeFactory jer je singleton, a treba scoped servise
- Refresh data svkih 10 minuta automatski

## 15. Zadnji Savjet 💡

**Ne moraš znati sve!** Ako te pitaju nešto što ne znaš:

✅ "To nisam koristio u ovom projektu, ali razumijem koncept..."
✅ "Trebao bih pogledati dokumentaciju za taj edge case..."
✅ "U ovom projektu sam koristio X pristup jer..."

**Pokažu im što ZNAŠ, ne skrivaj što NE znaš!**

---

## Quick Reference Card

```
TypeScript          →  C#
────────────────────────────────────
Promise<T>          →  Task<T>
async/await         →  async/await (isto!)
const/let           →  var/explicit type
.filter()           →  .Where()
.map()              →  .Select()
.find()             →  .FirstOrDefault()
??                  →  ?? (isto!)
?.                  →  ?. (isto!)
interface           →  interface (gotovo isto!)
class               →  class (gotovo isto!)
export class        →  public class
```

---

**Sretno na razgovoru! 🍀 Ti ovo već znaš, samo drugačija sintaksa!**
