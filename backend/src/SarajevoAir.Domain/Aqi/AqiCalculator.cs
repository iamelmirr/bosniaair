using System.Text.Json;

namespace SarajevoAir.Domain.Aqi;

public class AqiCalculator : IAqiCalculator
{
    private readonly Dictionary<string, List<Breakpoint>> _breakpoints;

    public AqiCalculator()
    {
        var breakpointsPath = Path.Combine(AppContext.BaseDirectory, "Aqi", "Breakpoints.json");
        if (!File.Exists(breakpointsPath))
        {
            // Try relative path for development/testing
            breakpointsPath = Path.Combine(Directory.GetCurrentDirectory(), "Breakpoints.json");
        }

        if (!File.Exists(breakpointsPath))
        {
            throw new FileNotFoundException($"Breakpoints.json not found at {breakpointsPath}");
        }

        var json = File.ReadAllText(breakpointsPath);
        _breakpoints = JsonSerializer.Deserialize<Dictionary<string, List<Breakpoint>>>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? new Dictionary<string, List<Breakpoint>>();
    }

    public AqiResult Compute(
        decimal? pm25 = null,
        decimal? pm10 = null,
        decimal? o3 = null,
        decimal? no2 = null,
        decimal? so2 = null,
        decimal? co = null)
    {
        var subindices = new Dictionary<string, int>();

        AddSubindex(subindices, "pm25", pm25);
        AddSubindex(subindices, "pm10", pm10);
        AddSubindex(subindices, "o3", o3);
        AddSubindex(subindices, "no2", no2);
        AddSubindex(subindices, "so2", so2);
        AddSubindex(subindices, "co", co);

        var finalAqi = subindices.Count > 0 ? subindices.Values.Max() : 0;
        var category = CategoryFromAqi(finalAqi);

        return new AqiResult(finalAqi, category, subindices);
    }

    private void AddSubindex(Dictionary<string, int> subindices, string pollutant, decimal? concentration)
    {
        if (concentration is null) return;

        if (!_breakpoints.TryGetValue(pollutant, out var breakpoints) || breakpoints.Count == 0)
            return;

        var value = (double)concentration.Value;
        
        foreach (var bp in breakpoints)
        {
            if (value >= bp.CLo && value <= bp.CHi)
            {
                // Linear interpolation formula: I = ((IHi - ILo) / (CHi - CLo)) * (C - CLo) + ILo
                var slope = (bp.IHi - bp.ILo) / (bp.CHi - bp.CLo);
                var aqi = slope * (value - bp.CLo) + bp.ILo;
                subindices[pollutant] = (int)Math.Round(aqi);
                return;
            }
        }

        // If concentration exceeds all breakpoints, use the highest range
        var highestBp = breakpoints.LastOrDefault();
        if (highestBp != null && value > highestBp.CHi)
        {
            subindices[pollutant] = highestBp.IHi;
        }
    }

    private static AqiCategory CategoryFromAqi(int aqi) => aqi switch
    {
        <= 50 => AqiCategory.Good,
        <= 100 => AqiCategory.Moderate,
        <= 150 => AqiCategory.USG,
        <= 200 => AqiCategory.Unhealthy,
        <= 300 => AqiCategory.VeryUnhealthy,
        _ => AqiCategory.Hazardous
    };

    private record Breakpoint(double CLo, double CHi, int ILo, int IHi);
}