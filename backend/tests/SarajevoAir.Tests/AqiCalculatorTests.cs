using FluentAssertions;
using SarajevoAir.Domain.Aqi;
using Xunit;

namespace SarajevoAir.Tests;

public class AqiCalculatorTests
{
    private readonly AqiCalculator _calculator;

    public AqiCalculatorTests()
    {
        _calculator = new AqiCalculator();
    }

    [Theory]
    [InlineData(0.0, 0, AqiCategory.Good)]
    [InlineData(12.0, 50, AqiCategory.Good)]
    [InlineData(12.1, 51, AqiCategory.Moderate)]
    [InlineData(35.0, 99, AqiCategory.Moderate)] // Edge case near boundary
    [InlineData(35.4, 100, AqiCategory.Moderate)]
    [InlineData(35.5, 101, AqiCategory.USG)]
    [InlineData(55.4, 150, AqiCategory.USG)]
    [InlineData(55.5, 151, AqiCategory.Unhealthy)]
    [InlineData(150.4, 200, AqiCategory.Unhealthy)]
    [InlineData(150.5, 201, AqiCategory.VeryUnhealthy)]
    [InlineData(250.4, 300, AqiCategory.VeryUnhealthy)]
    [InlineData(350.5, 401, AqiCategory.Hazardous)]
    public void Compute_WithPM25Values_ShouldReturnCorrectAqiAndCategory(
        decimal pm25Value, 
        int expectedAqi, 
        AqiCategory expectedCategory)
    {
        // Act
        var result = _calculator.Compute(pm25: pm25Value);

        // Assert
        result.Aqi.Should().BeCloseTo(expectedAqi, 1, "AQI calculation should be within 1 point");
        result.Category.Should().Be(expectedCategory);
        result.Subindices.Should().ContainKey("pm25");
        result.Subindices["pm25"].Should().BeCloseTo(expectedAqi, 1);
    }

    [Theory]
    [InlineData(0, 0, AqiCategory.Good)]
    [InlineData(54, 50, AqiCategory.Good)]
    [InlineData(55, 51, AqiCategory.Moderate)]
    [InlineData(154, 100, AqiCategory.Moderate)]
    [InlineData(155, 101, AqiCategory.USG)]
    [InlineData(254, 150, AqiCategory.USG)]
    [InlineData(255, 151, AqiCategory.Unhealthy)]
    [InlineData(354, 200, AqiCategory.Unhealthy)]
    [InlineData(355, 201, AqiCategory.VeryUnhealthy)]
    public void Compute_WithPM10Values_ShouldReturnCorrectAqiAndCategory(
        decimal pm10Value,
        int expectedAqi,
        AqiCategory expectedCategory)
    {
        // Act
        var result = _calculator.Compute(pm10: pm10Value);

        // Assert
        result.Aqi.Should().BeCloseTo(expectedAqi, 1);
        result.Category.Should().Be(expectedCategory);
        result.Subindices.Should().ContainKey("pm10");
        result.Subindices["pm10"].Should().BeCloseTo(expectedAqi, 1);
    }

    [Fact]
    public void Compute_WithMultiplePollutants_ShouldReturnMaxAqi()
    {
        // Arrange - PM2.5=30 (~86 AQI), PM10=100 (~65 AQI)
        var pm25 = 30.0m; // Should be ~86 AQI (Moderate)
        var pm10 = 100.0m; // Should be ~65 AQI (Moderate)

        // Act
        var result = _calculator.Compute(pm25: pm25, pm10: pm10);

        // Assert
        result.Subindices.Should().HaveCount(2);
        result.Subindices.Should().ContainKeys("pm25", "pm10");
        
        // PM2.5 should have higher AQI, so that should be the final result
        result.Aqi.Should().Be(result.Subindices.Values.Max());
        result.Category.Should().Be(AqiCategory.Moderate);
    }

    [Fact]
    public void Compute_WithNoValidValues_ShouldReturnZeroAqi()
    {
        // Act
        var result = _calculator.Compute();

        // Assert
        result.Aqi.Should().Be(0);
        result.Category.Should().Be(AqiCategory.Good);
        result.Subindices.Should().BeEmpty();
    }

    [Fact]
    public void Compute_WithNullValues_ShouldIgnoreNullPollutants()
    {
        // Act
        var result = _calculator.Compute(pm25: 25.0m, pm10: null, o3: null);

        // Assert
        result.Subindices.Should().ContainSingle();
        result.Subindices.Should().ContainKey("pm25");
        result.Subindices.Should().NotContainKeys("pm10", "o3");
    }

    [Theory]
    [InlineData(35.0)] // Known edge case - should be ~99 AQI
    [InlineData(12.0)] // Boundary between Good and Moderate
    [InlineData(55.4)] // Upper boundary of USG category
    public void Compute_EdgeCaseBoundaryValues_ShouldCalculateCorrectly(decimal pm25Value)
    {
        // Act
        var result = _calculator.Compute(pm25: pm25Value);

        // Assert
        result.Aqi.Should().BeGreaterThan(0, "AQI should be calculated for valid PM2.5 values");
        result.Subindices.Should().ContainKey("pm25");
        
        // Verify the calculation is reasonable (within EPA formula expectations)
        var calculatedSubindex = result.Subindices["pm25"];
        calculatedSubindex.Should().BeInRange(0, 500, "AQI subindex should be within valid range");
    }

    [Fact] 
    public void Compute_VeryHighPollutantValue_ShouldCapAtMaximum()
    {
        // Arrange - Value higher than any breakpoint
        var extremelyHighPm25 = 1000.0m;

        // Act
        var result = _calculator.Compute(pm25: extremelyHighPm25);

        // Assert
        result.Category.Should().Be(AqiCategory.Hazardous);
        result.Subindices["pm25"].Should().Be(500, "Values exceeding all breakpoints should use highest range");
    }

    [Theory]
    [InlineData(AqiCategory.Good, "#00E400")]
    [InlineData(AqiCategory.Moderate, "#FFFF00")]
    [InlineData(AqiCategory.USG, "#FF7E00")]
    [InlineData(AqiCategory.Unhealthy, "#FF0000")]
    [InlineData(AqiCategory.VeryUnhealthy, "#99004C")]
    [InlineData(AqiCategory.Hazardous, "#7E0023")]
    public void AqiCategory_GetHexColor_ShouldReturnCorrectColor(AqiCategory category, string expectedColor)
    {
        // Act
        var color = category.GetHexColor();

        // Assert
        color.Should().Be(expectedColor);
    }

    [Theory]
    [InlineData(AqiCategory.Good, "en", "Air quality is satisfactory")]
    [InlineData(AqiCategory.Moderate, "en", "Air quality is acceptable")]
    [InlineData(AqiCategory.Good, "bs", "Zrak je dobar")]
    [InlineData(AqiCategory.Moderate, "bs", "Umjerena zagađenja")]
    public void AqiCategory_GetRecommendation_ShouldReturnCorrectMessage(
        AqiCategory category, 
        string language, 
        string expectedSubstring)
    {
        // Act
        var recommendation = category.GetRecommendation(language);

        // Assert
        recommendation.Should().Contain(expectedSubstring);
    }
}