using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Data.Migrations
{
    /// <inheritdoc />
    public partial class newCarColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Fiyat",
                table: "Cars",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kilometre",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Lt_100Km",
                table: "Cars",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MotorHacmi",
                table: "Cars",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Vergi",
                table: "Cars",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fiyat",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Kilometre",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Lt_100Km",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "MotorHacmi",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Vergi",
                table: "Cars");
        }
    }
}
