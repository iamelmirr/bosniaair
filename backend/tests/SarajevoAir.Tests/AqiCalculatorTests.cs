using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SarajevoAir.Tests;

public class CityComparisonServiceTests
{
    [Fact]
    public async Task CompareCitiesAsync_RemovesDuplicateCitiesAndForcesFreshFetch()
    {
        var airQualityMock = new Mock<IAirQualityService>();
        airQualityMock
            .Setup(x => x.GetLiveAqiAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .Returns<string, bool, CancellationToken>((city, _, _) => Task.FromResult(new LiveAqiResponse(
                City: city,
                OverallAqi: 75,
                AqiCategory: "Moderate",
                Color: "#ffff00",
                HealthMessage: "Uživajte, ali budite oprezni.",
                Timestamp: DateTime.UtcNow,
                Measurements: Array.Empty<MeasurementDto>(),
                DominantPollutant: "pm25")));

    var service = new CityComparisonService(airQualityMock.Object, NullLogger<CityComparisonService>.Instance);

        var response = await service.CompareCitiesAsync("Sarajevo, Tuzla, Sarajevo");

        response.TotalCities.Should().Be(2);
        response.Cities.Select(c => c.City).Should().BeEquivalentTo(new[] { "Sarajevo", "Tuzla" });

        airQualityMock.Verify(x => x.GetLiveAqiAsync("Sarajevo", true, It.IsAny<CancellationToken>()), Times.Once);
        airQualityMock.Verify(x => x.GetLiveAqiAsync("Tuzla", true, It.IsAny<CancellationToken>()), Times.Once);
        airQualityMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompareCitiesAsync_WhenFetchFails_ReturnsErrorEntry()
    {
        var airQualityMock = new Mock<IAirQualityService>();
        airQualityMock
            .Setup(x => x.GetLiveAqiAsync("Sarajevo", true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Boom"));

    var service = new CityComparisonService(airQualityMock.Object, NullLogger<CityComparisonService>.Instance);

        var response = await service.CompareCitiesAsync("Sarajevo");

        response.TotalCities.Should().Be(1);
        var entry = response.Cities.Single();
        entry.City.Should().Be("Sarajevo");
        entry.Aqi.Should().BeNull();
        entry.Category.Should().Be("No Data");
        entry.Color.Should().Be("#cccccc");
        entry.Error.Should().Be("Boom");
    }
}