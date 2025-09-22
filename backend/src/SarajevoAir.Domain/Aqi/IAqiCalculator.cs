namespace SarajevoAir.Domain.Aqi;

public interface IAqiCalculator
{
    AqiResult Compute(
        decimal? pm25 = null,
        decimal? pm10 = null,
        decimal? o3 = null,
        decimal? no2 = null,
        decimal? so2 = null,
        decimal? co = null
    );
}