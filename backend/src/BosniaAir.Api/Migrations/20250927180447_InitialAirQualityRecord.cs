using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosniaAir.Api.Migrations
{
    public partial class InitialAirQualityRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AirQualityRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    City = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RecordType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    StationId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AqiValue = table.Column<int>(type: "INTEGER", nullable: true),
                    DominantPollutant = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Pm25 = table.Column<double>(type: "REAL", nullable: true),
                    Pm10 = table.Column<double>(type: "REAL", nullable: true),
                    O3 = table.Column<double>(type: "REAL", nullable: true),
                    No2 = table.Column<double>(type: "REAL", nullable: true),
                    Co = table.Column<double>(type: "REAL", nullable: true),
                    So2 = table.Column<double>(type: "REAL", nullable: true),
                    ForecastJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirQualityRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AirQuality_CityTypeTimestamp",
                table: "AirQualityRecords",
                columns: new[] { "City", "RecordType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "UX_AirQuality_ForecastPerCity",
                table: "AirQualityRecords",
                columns: new[] { "City", "RecordType" },
                unique: true,
                filter: "[RecordType] = 'Forecast'");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AirQualityRecords");
        }
    }
}
