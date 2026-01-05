using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Data.Migrations
{
    /// <inheritdoc />
    public partial class newColumn3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "yil",
                table: "Cars",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<string>(
                name: "degisenBoyanan",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "donanimSeviyesi",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "hasarKaydi",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kasaTipi",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "motorGuc_hp",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sahipSayisi",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sehir",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "servisGecmisi",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tork_nm",
                table: "Cars",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "degisenBoyanan",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "donanimSeviyesi",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "hasarKaydi",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "kasaTipi",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "motorGuc_hp",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "sahipSayisi",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "sehir",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "servisGecmisi",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "tork_nm",
                table: "Cars");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "yil",
                table: "Cars",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
