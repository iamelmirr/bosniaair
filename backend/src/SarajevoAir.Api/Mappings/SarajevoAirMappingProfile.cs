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
        /// Mapiranje SarajevoMeasurement Entity → LiveAqiResponse DTO
        /// Pokazuje kako domain objekti postaju presentation objekti
        /// </summary>
        CreateMap<SarajevoMeasurement, LiveAqiResponse>()
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => "Sarajevo"))
            .ForMember(dest => dest.OverallAqi, opt => opt.MapFrom(src => src.AqiValue))
            .ForMember(dest => dest.AqiCategory, opt => opt.Ignore()) // Service će podesiti
            .ForMember(dest => dest.Color, opt => opt.Ignore()) // Service će podesiti
            .ForMember(dest => dest.HealthMessage, opt => opt.Ignore()) // Service će podesiti
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.Measurements, opt => opt.Ignore()) // Service će podesiti
            .ForMember(dest => dest.DominantPollutant, opt => opt.Ignore()); // Service će podesiti

        // Note: Complex custom mappings will be handled in Service layer
        // AutoMapper works best for simple property mappings

        /// <summary>
        /// SarajevoForecast Entity → ForecastResponse DTO
        /// </summary>
        CreateMap<SarajevoForecast, ForecastResponse>()
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => "Sarajevo"))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => DateTime.UtcNow));

        // ===== REQUEST → SERVICE PARAMETER MAPPINGS =====
        // Ove će trebati kada refaktoriramo Service metode
        
        /// <summary>
        /// Request DTOs se mogu koristiti direktno u Service layer-u
        /// ili mapirati u internal service parametere
        /// </summary>
    }
}