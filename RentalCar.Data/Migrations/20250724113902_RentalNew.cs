using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Data.Migrations
{
    /// <inheritdoc />
    public partial class RentalNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyPrice",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyPrice",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyPrice",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "WeeklyPrice",
                table: "Rentals");
        }
    }
}
