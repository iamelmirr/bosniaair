namespace SarajevoAir.Api.Dtos;

public record DailyAqiEntry(
    string Date,
    string DayName,
    string ShortDay,
    int Aqi,
    string Category,
    string Color
);

public record DailyAqiResponse(
    string City,
    string Period,
    IReadOnlyList<DailyAqiEntry> Data,
    DateTime Timestamp
);
