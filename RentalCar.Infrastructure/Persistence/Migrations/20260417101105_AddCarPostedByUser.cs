using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarPostedByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostedByUserId",
                table: "Cars",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cars_PostedByUserId",
                table: "Cars",
                column: "PostedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_AspNetUsers_PostedByUserId",
                table: "Cars",
                column: "PostedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cars_AspNetUsers_PostedByUserId",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_PostedByUserId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "PostedByUserId",
                table: "Cars");
        }
    }
}
