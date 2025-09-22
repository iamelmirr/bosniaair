using SarajevoAir.Application.Dtos;

namespace SarajevoAir.Application.Interfaces;

public interface IMeasurementService
{
    Task<LiveAirQualityDto?> GetLiveDataAsync(string city, CancellationToken cancellationToken = default);
    Task<HistoryResponseDto> GetHistoryDataAsync(string city, int days, string resolution, CancellationToken cancellationToken = default);
    Task<CompareResponseDto> GetCompareDataAsync(string[] cities, CancellationToken cancellationToken = default);
    Task<List<LocationInfoDto>> GetLocationsAsync(string city, CancellationToken cancellationToken = default);
    Task FetchAndStoreAsync(string city, CancellationToken cancellationToken = default);
    Task GenerateDailyAggregatesAsync(DateOnly date, CancellationToken cancellationToken = default);
}