using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SarajevoAir.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Lat = table.Column<decimal>(type: "numeric", nullable: true),
                    Lon = table.Column<decimal>(type: "numeric", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "openaq"),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyAggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AvgPm25 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MaxPm25 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MinPm25 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    AvgPm10 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MaxPm10 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MinPm10 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    AvgO3 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MaxO3 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MinO3 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    AvgNo2 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MaxNo2 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MinNo2 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    AvgSo2 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MaxSo2 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MinSo2 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    AvgCo = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MaxCo = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    MinCo = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    AvgAqi = table.Column<decimal>(type: "numeric(5,1)", nullable: true),
                    MaxAqi = table.Column<int>(type: "integer", nullable: true),
                    MinAqi = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyAggregates_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Pm25 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    Pm10 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    O3 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    No2 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    So2 = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    Co = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    RawJson = table.Column<string>(type: "jsonb", nullable: true),
                    ComputedAqi = table.Column<int>(type: "integer", nullable: true),
                    AqiCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Measurements_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyAggregates_Date",
                table: "DailyAggregates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "UX_DailyAggregates_Location_Date",
                table: "DailyAggregates",
                columns: new[] { "LocationId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_ExternalId",
                table: "Locations",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Lat_Lon",
                table: "Locations",
                columns: new[] { "Lat", "Lon" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_Location_AQI",
                table: "Measurements",
                columns: new[] { "LocationId", "ComputedAqi" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_Timestamp",
                table: "Measurements",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "UX_Measurements_Location_Timestamp",
                table: "Measurements",
                columns: new[] { "LocationId", "TimestampUtc" },
                unique: true,
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyAggregates");

            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}
