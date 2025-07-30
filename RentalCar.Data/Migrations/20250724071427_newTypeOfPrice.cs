using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Data.Migrations
{
    /// <inheritdoc />
    public partial class newTypeOfPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Cars",
                newName: "WeeklyPrice");

            migrationBuilder.AddColumn<string>(
                name: "DailyPrice",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MonthlyPrice",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyPrice",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                table: "Cars");

            migrationBuilder.RenameColumn(
                name: "WeeklyPrice",
                table: "Cars",
                newName: "Price");
        }
    }
}
