using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SarajevoAir.Api.Data;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Entities;
using SarajevoAir.Api.Repositories;
using SarajevoAir.Api.Services;
using Xunit;

namespace SarajevoAir.Tests;

public class DailyAqiServiceTests
{
    private static DailyAqiService CreateService(AppDbContext context, Mock<IAirQualityService> airQualityMock)
    {
        var repository = new AqiRepository(context);
        return new DailyAqiService(repository, airQualityMock.Object, NullLogger<DailyAqiService>.Instance);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetDailyAqiAsync_WithSparseData_ReturnsSevenDayTimeline()
    {
        await using var context = CreateContext();
        var repository = new AqiRepository(context);
        var airQualityMock = new Mock<IAirQualityService>();

        airQualityMock
            .Setup(x => x.GetLiveAqiAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveAqiResponse(
                City: "Sarajevo",
                OverallAqi: 80,
                AqiCategory: "Moderate",
                Color: "#ffff00",
                HealthMessage: "Ostanite oprezni vani.",
                Timestamp: DateTime.UtcNow,
                Measurements: Array.Empty<MeasurementDto>(),
                DominantPollutant: "pm25"));

        var today = DateTime.UtcNow.Date;

        await repository.AddRecordAsync(new SimpleAqiRecord
        {
            City = "Sarajevo",
            Timestamp = today.AddDays(-1).AddHours(2),
            AqiValue = 90
        });

        await repository.AddRecordAsync(new SimpleAqiRecord
        {
            City = "Sarajevo",
            Timestamp = today.AddDays(-1).AddHours(8),
            AqiValue = 94
        });

        await repository.AddRecordAsync(new SimpleAqiRecord
        {
            City = "Sarajevo",
            Timestamp = today.AddDays(-3).AddHours(6),
            AqiValue = 60
        });

        var service = CreateService(context, airQualityMock);

        var response = await service.GetDailyAqiAsync("Sarajevo");

        response.City.Should().Be("Sarajevo");
        response.Data.Should().HaveCount(7);
        response.Data.Last().Date.Should().Be(today.ToString("yyyy-MM-dd"));

        var yesterday = response.Data.Single(e => e.Date == today.AddDays(-1).ToString("yyyy-MM-dd"));
        yesterday.Aqi.Should().Be(92);
        yesterday.Category.Should().Be("Moderate");

        var todayEntry = response.Data.Single(e => e.Date == today.ToString("yyyy-MM-dd"));
        todayEntry.Aqi.Should().Be(92);
        todayEntry.Category.Should().Be("Moderate");

        var earliest = response.Data.First();
    earliest.Aqi.Should().Be(94);
    earliest.Category.Should().Be("Moderate");
    }

    [Fact]
    public async Task GetDailyAqiAsync_WithNoRecentData_UsesRepositoryFallback()
    {
        await using var context = CreateContext();
        var repository = new AqiRepository(context);
        var airQualityMock = new Mock<IAirQualityService>();

        var today = DateTime.UtcNow.Date;

        await repository.AddRecordAsync(new SimpleAqiRecord
        {
            City = "Sarajevo",
            Timestamp = today.AddDays(-10),
            AqiValue = 55
        });

        airQualityMock
            .Setup(x => x.GetLiveAqiAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveAqiResponse(
                City: "Sarajevo",
                OverallAqi: 120,
                AqiCategory: "Unhealthy",
                Color: "#ff0000",
                HealthMessage: "Izbjegavajte vanjske aktivnosti.",
                Timestamp: DateTime.UtcNow,
                Measurements: Array.Empty<MeasurementDto>(),
                DominantPollutant: "pm25"));

        var service = CreateService(context, airQualityMock);

        var response = await service.GetDailyAqiAsync("Sarajevo");

        response.Data.Should().HaveCount(7);
        response.Data.Should().OnlyContain(entry => entry.Aqi == 55);
        response.Data.Should().OnlyContain(entry => entry.Category == "Moderate");

        airQualityMock.Verify(x => x.GetLiveAqiAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDailyAqiAsync_WithEmptyRepository_UsesLiveServiceFallback()
    {
        await using var context = CreateContext();
        var airQualityMock = new Mock<IAirQualityService>();

        airQualityMock
            .Setup(x => x.GetLiveAqiAsync("Sarajevo", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveAqiResponse(
                City: "Sarajevo",
                OverallAqi: 77,
                AqiCategory: "Moderate",
                Color: "#ffff00",
                HealthMessage: "Blago zagađenje.",
                Timestamp: DateTime.UtcNow,
                Measurements: Array.Empty<MeasurementDto>(),
                DominantPollutant: "pm25"));

        var service = CreateService(context, airQualityMock);

        var response = await service.GetDailyAqiAsync("Sarajevo");

        response.Data.Should().HaveCount(7);
        response.Data.Should().OnlyContain(entry => entry.Aqi == 77);

        airQualityMock.Verify(x => x.GetLiveAqiAsync("Sarajevo", true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
