# 🏗️ SARAJEVO AIR - CLEAN ARCHITECTURE OVERVIEW

## 📋 **PROJECT FOR JOB INTERVIEW - TECHNICAL DOCUMENTATION**

> **Demonstracija enterprise-level ASP.NET Core development skills**
> 
> Projekt pokazuje: Clean Architecture, SOLID principi, best practices, testabilnost, maintainability

---

## 🎯 **ARCHITECTURE PATTERN: CLEAN ARCHITECTURE**

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                       │
│  Controllers → Request DTOs → Response DTOs                 │ 
│  [SarajevoController] [LiveDataRequest] [LiveAqiResponse]   │
└─────────────────────┬───────────────────────────────────────┘
                      │ AutoMapper
┌─────────────────────▼───────────────────────────────────────┐
│                   APPLICATION LAYER                         │
│  Services → Business Logic → External API Integration       │
│  [SarajevoService] [AqicnService] [AirQualityRefreshService]│
└─────────────────────┬───────────────────────────────────────┘
                      │ Entity Framework
┌─────────────────────▼───────────────────────────────────────┐
│                    DOMAIN LAYER                             │
│  Entities → Domain Models → Business Rules                  │
│  [SarajevoMeasurement] [SarajevoForecast]                   │
└─────────────────────┬───────────────────────────────────────┘
                      │ Repository Pattern
┌─────────────────────▼───────────────────────────────────────┐
│                INFRASTRUCTURE LAYER                         │
│  Database → External APIs → Background Services             │
│  [SQLite] [WAQI API] [Hosted Services]                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔧 **KEY TECHNICAL IMPLEMENTATIONS**

### **1. REQUEST/RESPONSE PATTERN**
```csharp
// ❌ PRIJE - Primitive parameters
public async Task<LiveAqiResponse> GetLive(bool forceFresh)

// ✅ SADA - Request DTO pattern
public async Task<LiveAqiResponse> GetLive(LiveDataRequest request)
```

**PREDNOSTI:**
- ✅ Type safety
- ✅ Extensibility - lako dodavanje novih parametara
- ✅ Validation centralization  
- ✅ Better testing support

### **2. AUTOMAPPER INTEGRATION**
```csharp
// Eliminates manual mapping boilerplate
CreateMap<SarajevoMeasurement, LiveAqiResponse>()
    .ForMember(dest => dest.City, opt => opt.MapFrom(src => "Sarajevo"))
    .ForMember(dest => dest.OverallAqi, opt => opt.MapFrom(src => src.AqiValue));
```

### **3. FLUENTVALIDATION**
```csharp
public class LiveDataRequestValidator : AbstractValidator<LiveDataRequest>
{
    public LiveDataRequestValidator()
    {
        RuleFor(x => x.TimeoutSeconds)
            .InclusiveBetween(5, 300)
            .WithMessage("Timeout must be between 5 and 300 seconds");
    }
}
```

---

## 🚀 **DATA FLOW EXPLANATION**

### **LIVE DATA REQUEST:**
```
1. HTTP Request → SarajevoController.GetLive(LiveDataRequest)
2. FluentValidation → validates Request DTO
3. Controller → calls SarajevoService.GetLiveAsync()  
4. Service → calls AqiRepository.GetLatestMeasurement()
5. Repository → queries SQLite database
6. AutoMapper → maps Entity to Response DTO
7. Controller → returns LiveAqiResponse JSON
```

### **BACKGROUND REFRESH:**
```
1. AirQualityRefreshService (Hosted Service) → runs every 10 minutes
2. Service → calls WAQI API for fresh data
3. Service → saves to SQLite via Repository  
4. Service → logs success/failure
5. Next HTTP requests → get fresh data from database
```

---

## 🎪 **DEPENDENCY INJECTION SETUP**

```csharp
// Clean separation of concerns through DI
builder.Services.AddAutoMapper(typeof(SarajevoAirMappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<LiveDataRequestValidator>();

builder.Services.AddScoped<IAqiRepository, AqiRepository>();
builder.Services.AddScoped<ISarajevoService, SarajevoService>();
builder.Services.AddScoped<IAqicnService, AqicnService>();

builder.Services.AddHostedService<AirQualityRefreshService>();
```

---

## 🧪 **TESTABILITY IMPROVEMENTS**

### **PRIJE vs SADA:**
```csharp
// ❌ PRIJE - Hard to test  
public async Task<IActionResult> GetLive(bool forceFresh)
{
    var result = await _service.GetLiveAsync(forceFresh);
    return Ok(result);
}

// ✅ SADA - Easy to mock and test
public async Task<IActionResult> GetLive(LiveDataRequest request) 
{
    var result = await _service.GetLiveAsync(request.ForceFresh);
    return Ok(result);
}

// Unit test example:
var request = new LiveDataRequest { ForceFresh = true };
var result = await controller.GetLive(request);
```

---

## 🏢 **INTERVIEW TALKING POINTS**

### **1. CLEAN ARCHITECTURE**
- "Implementirao sam layered architecture sa jasnom separation of concerns"
- "Domain layer je nezavisan od infrastructure"
- "Easy to test, maintain i extend"

### **2. SOLID PRINCIPLES**
- **Single Responsibility:** Svaki servis ima jednu odgovornost
- **Dependency Inversion:** Controller zavisi od interface-a, ne konkretnih klasa  
- **Open/Closed:** Lako proširivanje kroz nova DTO-a

### **3. ENTERPRISE PATTERNS**
- **Repository Pattern:** Data access abstraction
- **DTO Pattern:** Clean data contracts
- **Hosted Services:** Background processing
- **AutoMapper:** Object-object mapping

### **4. PERFORMANCE & SCALABILITY**
- **Database caching:** SQLite za brži pristup
- **Background refresh:** Non-blocking data updates
- **Async/await:** Non-blocking I/O operations

---

## 🎯 **NEXT STEPS (Future Improvements)**

1. **Unit Testing:** xUnit test suite
2. **Integration Testing:** TestServer setup
3. **Caching:** Redis za distributed caching
4. **Monitoring:** Health checks, metrics
5. **Error Handling:** Global exception middleware
6. **API Versioning:** Multiple API versions support

---

## 🔍 **CODE QUALITY METRICS**

- ✅ **Separation of Concerns:** Clear layer boundaries
- ✅ **DRY Principle:** No code duplication
- ✅ **Type Safety:** Strong typing throughout
- ✅ **Error Handling:** Comprehensive try-catch blocks
- ✅ **Logging:** Structured logging with Serilog
- ✅ **Documentation:** Comprehensive XML comments

---

*Ovaj projekt demonstrira production-ready ASP.NET Core development skills potrebne za enterprise development pozicije.*