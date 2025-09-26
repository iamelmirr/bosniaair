using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarajevoAir.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SarajevoForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Aqi = table.Column<int>(type: "INTEGER", nullable: true),
                    Pm25Min = table.Column<double>(type: "REAL", nullable: true),
                    Pm25Max = table.Column<double>(type: "REAL", nullable: true),
                    Pm25Avg = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SarajevoForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SarajevoMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Pm25 = table.Column<double>(type: "REAL", nullable: true),
                    Pm10 = table.Column<double>(type: "REAL", nullable: true),
                    O3 = table.Column<double>(type: "REAL", nullable: true),
                    No2 = table.Column<double>(type: "REAL", nullable: true),
                    Co = table.Column<double>(type: "REAL", nullable: true),
                    So2 = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SarajevoMeasurements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimpleAqiRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AqiValue = table.Column<int>(type: "INTEGER", nullable: false),
                    City = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleAqiRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SarajevoForecast_Date",
                table: "SarajevoForecasts",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SarajevoMeasurements_Timestamp",
                table: "SarajevoMeasurements",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AqiRecords_CityTimestamp",
                table: "SimpleAqiRecords",
                columns: new[] { "City", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SarajevoForecasts");

            migrationBuilder.DropTable(
                name: "SarajevoMeasurements");

            migrationBuilder.DropTable(
                name: "SimpleAqiRecords");
        }
    }
}
