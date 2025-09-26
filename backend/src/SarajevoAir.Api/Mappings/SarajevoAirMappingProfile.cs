/*
=== AUTOMAPPER PROFILE ===
Definira mapping između Entity objekata i DTOs
Demonstrira clean separation of concerns

PREDNOSTI AutoMapper-a:
1. Eliminira boilerplate mapping kod
2. Type-safe mapiranje 
3. Konfigurabilno za complex scenarije
4. Testabilnost - easy unit testing
5. Maintainability - centralizirane mapping rules

INTERVJU PLUS:
- Pokazuje znanje enterprise patterns
- Separation between domain i presentation layer
- Maintainable kod za timski rad
*/

using AutoMapper;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Dtos.Requests;

namespace SarajevoAir.Api.Mappings;

/// <summary>
/// AutoMapper profil za mapiranje između Entities i DTOs
/// Centralizira sve mapping logic na jednom mjestu
/// </summary>
public class SarajevoAirMappingProfile : Profile
{
    public SarajevoAirMappingProfile()
    {
        // ===== ENTITY → DTO MAPPINGS =====
        
        /// <summary>
        /// SarajevoMeasurement Entity → LiveAqiResponse DTO
        /// AutoMapper kopira matching properties, mi definiramo custom logic
        /// </summary>
        CreateMap<SarajevoMeasurement, LiveAqiResponse>()
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => "Sarajevo"))
            .ForMember(dest => dest.OverallAqi, opt => opt.MapFrom(src => src.AqiValue ?? 0))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.DominantPollutant, opt => opt.MapFrom(src => "PM2.5"))
            // Ove properties Service mora manuelno da postavi:
            .ForMember(dest => dest.AqiCategory, opt => opt.Ignore()) 
            .ForMember(dest => dest.Color, opt => opt.Ignore()) 
            .ForMember(dest => dest.HealthMessage, opt => opt.Ignore()) 
            .ForMember(dest => dest.Measurements, opt => opt.Ignore()); 

        /// <summary>
        /// SarajevoForecast Entity → ForecastDayDto 
        /// Mapira pojedinačni forecast dan
        /// </summary>
        CreateMap<SarajevoForecast, ForecastDayDto>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.Aqi, opt => opt.MapFrom(src => (int)(src.Pm25Avg ?? 0)))
            // Category i Color će Service podesiti na osnovu AQI
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.Color, opt => opt.Ignore())
            .ForMember(dest => dest.Pollutants, opt => opt.MapFrom(src => 
                new ForecastDayPollutants(
                    new PollutantRangeDto((int)(src.Pm25Avg ?? 0), (int)(src.Pm25Min ?? 0), (int)(src.Pm25Max ?? 0)),
                    null,
                    null
                )
            ));


    }
}