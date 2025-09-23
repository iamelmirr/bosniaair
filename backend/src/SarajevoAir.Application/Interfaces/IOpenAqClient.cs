using SarajevoAir.Application.Dtos;

namespace SarajevoAir.Application.Interfaces;

public interface IOpenAqClient
{
    Task<List<LocationDto>> GetLocationsAsync(
        double lat, 
        double lon, 
        int radiusKm, 
        CancellationToken cancellationToken = default
    );

    Task<List<SensorDto>> GetSensorsForLocationAsync(
        string externalLocationId, 
        CancellationToken cancellationToken = default
    );

    Task<List<MeasurementDto>> GetMeasurementsForSensorAsync(
        long sensorId, 
        DateTime sinceUtc, 
        int limit = 1000,
        CancellationToken cancellationToken = default
    );

    Task<List<MeasurementDto>> GetLatestMeasurementsAsync(
        CancellationToken cancellationToken = default
    );
}