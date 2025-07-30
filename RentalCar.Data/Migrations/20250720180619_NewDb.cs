using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Vin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Year = table.Column<DateOnly>(type: "date", nullable: false),
                    FuelType = table.Column<int>(type: "int", nullable: false),
                    Gear = table.Column<int>(type: "int", nullable: false),
                    BodyType = table.Column<int>(type: "int", nullable: false),
                    Colour = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Plate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Security = table.Column<int>(type: "int", nullable: false),
                    InternalEquipment = table.Column<int>(type: "int", nullable: false),
                    ExternalEquipment = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarImages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Imagepath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarImages", x => x.id);
                    table.ForeignKey(
                        name: "FK_CarImages_Cars_CardId",
                        column: x => x.CardId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarImages_CardId",
                table: "CarImages",
                column: "CardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarImages");

            migrationBuilder.DropTable(
                name: "Cars");
        }
    }
}
