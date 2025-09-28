using SarajevoAir.Api.Enums;

namespace SarajevoAir.Api.Services;

public class DataUnavailableException : Exception
{
    public City City { get; }
    public string DataKind { get; }

    public DataUnavailableException(City city, string dataKind)
        : base($"No cached {dataKind} data available for {city}.")
    {
        City = city;
        DataKind = dataKind;
    }
}
