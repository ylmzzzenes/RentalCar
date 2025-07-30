using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Data.Migrations
{
    /// <inheritdoc />
    public partial class newTypeofprice1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyPrice",
                table: "Cars",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                table: "Cars",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyPrice",
                table: "Cars",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
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

            migrationBuilder.DropColumn(
                name: "WeeklyPrice",
                table: "Cars");
        }
    }
}
