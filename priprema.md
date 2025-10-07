Summarized conversation historyRealno, **ne moraš učiti C# od nule** za ovaj razgovor! Evo zašto:

## Tvoja trenutna situacija ✅

1. **Već si napravio funkcionalan projekat u C#/.NET** - to je najvažnije
2. **Imaš React/frontend iskustvo** - to pokazuje da razumiješ programiranje
3. **Razumiješ kako tvoj kod radi** - upravo smo prošli kroz data flow
4. **Imaš 4 dana** - dovoljno da naučiš **samo ono što koristiš u projektu**

## Što TREBAŠ znati (2-3 sata učenja)

### 1. **Osnove sintakse koje KORISTIŠ** (1 sat)
```csharp
// Razlike između C# i JavaScript/TypeScript:
public class AirQualityService  // Klase umjesto export class
{
    private readonly IAqiRepository _repository;  // Dependency injection
    
    public async Task<LiveAqiResponse> GetLatestAqi(City city)  // Task umjesto Promise
    {
        var data = await _repository.GetLatestAqi(city);  // var = let/const
        return data ?? throw new Exception();  // ?? null coalescing
    }
}
```

**Što STVARNO trebaš razumjeti:**
- `public/private/protected` - vidljivost metoda
- `async/await` - isto kao u JS-u, samo sa `Task` umjesto `Promise`
- `var` vs `string/int/bool` - type inference vs explicit types
- `??` operator - null coalescing (kao `||` u JS-u)
- LINQ basics - `.Where()`, `.Select()`, `.FirstOrDefault()` (kao array methods)

### 2. **Koncepti iz TVOG projekta** (1-2 sata)

**Dependency Injection:**
```csharp
// Program.cs
builder.Services.AddScoped<IAirQualityService, AirQualityService>();
// ↓
// AirQualityController.cs
private readonly IAirQualityService _service;  // automatski injected
```

**Background Service:**
```csharp
public class AirQualityScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(...)  // Radi u pozadini
}
```

**Entity Framework:**
```csharp
_context.AirQualityRecords.Add(record);  // Dodaj u bazu
await _context.SaveChangesAsync();       // Spremi promjene
```

### 3. **Priprema za intervju pitanja** (1 sat)

**Moguća pitanja i odgovori:**

**Q: "Zašto si koristio C#/.NET?"**
- ✅ "Želio sam naučiti enterprise tehnologiju koja se koristi u velikim firmama"
- ✅ "Odličan je za API development sa ugrađenim background services"
- ✅ ".NET 8 je brz, besplatan i cross-platform"

**Q: "Kako funkcionira Background Service?"**
- ✅ "Koristi BackgroundService klasu koja radi neovisno od HTTP requesta"
- ✅ "ExecuteAsync metoda se izvršava svkih 10 minuta"
- ✅ "Koristi IServiceScopeFactory jer je background service Singleton a treba Scoped servise"

**Q: "Objasni Dependency Injection"**
- ✅ "Registrujem servise u Program.cs sa AddScoped/AddSingleton"
- ✅ "Framework automatski injektuje dependencije kroz constructor"
- ✅ "Olakšava testiranje i maintainability - mogu zamijeniti implementaciju"

## Što NE MORAŠ učiti ❌

- ❌ Naslijeđivanje i kompleksni OOP pattern
- ❌ Generics u detalje (samo razumjeti `Task<T>`, `List<T>`)
- ❌ Delegates, Events, Lambdas (osim osnovnih LINQ lambda izraza)
- ❌ Advanced LINQ queries
- ❌ Memory management i GC
- ❌ Threading i parallel programming dubinski

## Plan za 4 dana 📅

### **Dan 1 (danas, 2-3 sata)**
1. Pročitaj prepare.md dokumentaciju koju smo napravili
2. Pogledaj [Microsoft C# Tour (30 min)](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/)
3. Pregledaj svoj kod file po file - **razumjeti što svaka linija radi**

### **Dan 2 (2 sata)**
1. Napravi mini prezentaciju projekta (10 slajdova max)
2. Vježbaj objasniti: "Kako radi Background Service?" kao da objašnjavaš nekome
3. Napiši odgovore na pitanja iz prepare.md

### **Dan 3 (2 sata)**
1. Pokreni projekat lokalno i prođi kroz sve endpointe
2. Otvori Developer Tools i prati network requestove
3. Promijeni nešto sitno u kodu da vidiš kako build radi

### **Dan 4 (dan prije razgovora, 1 sat)**
1. Pregledaj svoje bilješke
2. Vježbaj demo prezentaciju (5-10 minuta)
3. Pripremi 2-3 pitanja za njih

## Brzi cheat sheet

Napravio bih ti brzi **C# za React developere** cheat sheet - hoćeš?

**Iskreno mišljenje:** Sa 4 dana i tvojim iskustvom, fokusiraj se na **razumijevanje TVOG koda**, ne na učenje cijelog C#. Oni te testiraju kako prezentiraš svoj rad, ne koliko znaš C# sintaksu napamet! 

Što te najviše brine za razgovor? Da fokusiramo učenje na to!