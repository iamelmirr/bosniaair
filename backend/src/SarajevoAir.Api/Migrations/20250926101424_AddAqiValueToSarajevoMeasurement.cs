using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarajevoAir.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAqiValueToSarajevoMeasurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AqiValue",
                table: "SarajevoMeasurements",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AqiValue",
                table: "SarajevoMeasurements");
        }
    }
}
