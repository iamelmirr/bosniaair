using System.Text.Json.Serialization;

namespace SarajevoAir.Api.Models;

public class AqicnResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public AqicnData? Data { get; set; }
}

public class AqicnData
{
    [JsonPropertyName("aqi")]
    public int Aqi { get; set; }
    
    [JsonPropertyName("idx")]
    public int Idx { get; set; }
    
    [JsonPropertyName("city")]
    public AqicnCity? City { get; set; }
    
    [JsonPropertyName("dominentpol")]
    public string DominentPol { get; set; } = string.Empty;
    
    [JsonPropertyName("iaqi")]
    public AqicnIaqi? Iaqi { get; set; }
    
    [JsonPropertyName("time")]
    public AqicnTime? Time { get; set; }
    
    [JsonPropertyName("forecast")]
    public AqicnForecast? Forecast { get; set; }
    
    [JsonPropertyName("attributions")]
    public AqicnAttribution[]? Attributions { get; set; }
}

public class AqicnCity
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("geo")]
    public double[]? Geo { get; set; }
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}

public class AqicnIaqi
{
    [JsonPropertyName("pm25")]
    public AqicnMeasurement? Pm25 { get; set; }
    
    [JsonPropertyName("pm10")]
    public AqicnMeasurement? Pm10 { get; set; }
    
    [JsonPropertyName("o3")]
    public AqicnMeasurement? O3 { get; set; }
    
    [JsonPropertyName("no2")]
    public AqicnMeasurement? No2 { get; set; }
    
    [JsonPropertyName("so2")]
    public AqicnMeasurement? So2 { get; set; }
    
    [JsonPropertyName("co")]
    public AqicnMeasurement? Co { get; set; }
    
    [JsonPropertyName("t")]
    public AqicnMeasurement? Temperature { get; set; }
    
    [JsonPropertyName("h")]
    public AqicnMeasurement? Humidity { get; set; }
    
    [JsonPropertyName("p")]
    public AqicnMeasurement? Pressure { get; set; }
    
    [JsonPropertyName("w")]
    public AqicnMeasurement? Wind { get; set; }
    
    [JsonPropertyName("dew")]
    public AqicnMeasurement? Dew { get; set; }
}

public class AqicnMeasurement
{
    [JsonPropertyName("v")]
    public double V { get; set; }
}

public class AqicnTime
{
    [JsonPropertyName("s")]
    public string S { get; set; } = string.Empty;
    
    [JsonPropertyName("tz")]
    public string Tz { get; set; } = string.Empty;
    
    [JsonPropertyName("v")]
    public long V { get; set; }
    
    [JsonPropertyName("iso")]
    public string Iso { get; set; } = string.Empty;
}

public class AqicnForecast
{
    [JsonPropertyName("daily")]
    public AqicnDailyForecast? Daily { get; set; }
}

public class AqicnDailyForecast
{
    [JsonPropertyName("pm25")]
    public AqicnDayForecast[]? Pm25 { get; set; }
    
    [JsonPropertyName("pm10")]
    public AqicnDayForecast[]? Pm10 { get; set; }
    
    [JsonPropertyName("o3")]
    public AqicnDayForecast[]? O3 { get; set; }
    
    [JsonPropertyName("uvi")]
    public AqicnDayForecast[]? Uvi { get; set; }
}

public class AqicnDayForecast
{
    [JsonPropertyName("avg")]
    public int Avg { get; set; }
    
    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;
    
    [JsonPropertyName("max")]
    public int Max { get; set; }
    
    [JsonPropertyName("min")]
    public int Min { get; set; }
}

public class AqicnAttribution
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}