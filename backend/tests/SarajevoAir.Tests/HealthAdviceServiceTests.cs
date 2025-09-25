using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SarajevoAir.Api.Dtos;
using SarajevoAir.Api.Services;
using Xunit;

namespace SarajevoAir.Tests;

public class HealthAdviceServiceTests
{
    [Fact]
    public async Task BuildGroupsResponseAsync_WithVeryHighAqi_ProducesElevatedRiskLevels()
    {
        var liveResponse = new LiveAqiResponse(
            City: "Sarajevo",
            OverallAqi: 220,
            AqiCategory: "Very Unhealthy",
            Color: "#8f3f97",
            HealthMessage: "Ostanite u zatvorenom prostoru.",
            Timestamp: DateTime.UtcNow,
            Measurements: Array.Empty<MeasurementDto>(),
            DominantPollutant: "pm25");

        var airQualityMock = new Mock<IAirQualityService>();
        airQualityMock
            .Setup(x => x.GetLiveAqiAsync("Sarajevo", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(liveResponse);

        var service = new HealthAdviceService(airQualityMock.Object, NullLogger<HealthAdviceService>.Instance);

        var response = await service.BuildGroupsResponseAsync("Sarajevo");

        response.City.Should().Be("Sarajevo");
        response.CurrentAqi.Should().Be(220);
        response.Groups.Should().HaveCount(4);

        var athletes = response.Groups.Single(g => g.Group.GroupName == "Sportisti");
        athletes.RiskLevel.Should().Be("very-high");
        athletes.CurrentRecommendation.Should().Contain("zatvorenim prostorima");

        var asthmatics = response.Groups.Single(g => g.Group.GroupName == "Astmatičari");
    asthmatics.RiskLevel.Should().Be("very-high");
    asthmatics.CurrentRecommendation.Should().Contain("Ostanite unutra");

        airQualityMock.Verify(x => x.GetLiveAqiAsync("Sarajevo", false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
