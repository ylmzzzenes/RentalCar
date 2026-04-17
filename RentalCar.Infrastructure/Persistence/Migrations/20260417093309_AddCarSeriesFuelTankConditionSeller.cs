using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarSeriesFuelTankConditionSeller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "arac_durumu",
                table: "Cars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "kimden",
                table: "Cars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "seri",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "takasa_uygun",
                table: "Cars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "yakit_deposu_lt",
                table: "Cars",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "arac_durumu",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "kimden",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "seri",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "takasa_uygun",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "yakit_deposu_lt",
                table: "Cars");
        }
    }
}
