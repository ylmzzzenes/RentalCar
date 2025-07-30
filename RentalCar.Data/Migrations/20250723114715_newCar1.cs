using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Data.Migrations
{
    /// <inheritdoc />
    public partial class newCar1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrls",
                table: "Cars",
                newName: "ImageUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Cars",
                newName: "ImageUrls");
        }
    }
}
