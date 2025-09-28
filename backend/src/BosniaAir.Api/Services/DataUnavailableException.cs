using BosniaAir.Api.Enums;

namespace BosniaAir.Api.Services;

/// <summary>
/// Exception thrown when air quality data is not available for a specific city.
/// Used to indicate that cached data has expired or is missing.
/// </summary>
public class DataUnavailableException : Exception
{
    /// <summary>
    /// The city for which data is unavailable
    /// </summary>
    public City City { get; }

    /// <summary>
    /// The type of data that is unavailable (e.g., "live", "forecast")
    /// </summary>
    public string DataKind { get; }

    /// <summary>
    /// Initializes a new instance of DataUnavailableException.
    /// </summary>
    /// <param name="city">The city identifier</param>
    /// <param name="dataKind">The type of data that is unavailable</param>
    public DataUnavailableException(City city, string dataKind)
        : base($"No cached {dataKind} data available for {city}.")
    {
        City = city;
        DataKind = dataKind;
    }
}
