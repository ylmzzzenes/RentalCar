using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Data.Migrations
{
    /// <inheritdoc />
    public partial class newAreaColumn2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Vergi",
                table: "Cars",
                newName: "vergi");

            migrationBuilder.RenameColumn(
                name: "Paket",
                table: "Cars",
                newName: "paket");

            migrationBuilder.RenameColumn(
                name: "MotorHacmi",
                table: "Cars",
                newName: "motorHacmi");

            migrationBuilder.RenameColumn(
                name: "ModelRaw",
                table: "Cars",
                newName: "modelraw");

            migrationBuilder.RenameColumn(
                name: "Model",
                table: "Cars",
                newName: "model");

            migrationBuilder.RenameColumn(
                name: "Marka",
                table: "Cars",
                newName: "marka");

            migrationBuilder.RenameColumn(
                name: "Lt_100Km",
                table: "Cars",
                newName: "lt_100km");

            migrationBuilder.RenameColumn(
                name: "Kilometre",
                table: "Cars",
                newName: "kilometre");

            migrationBuilder.RenameColumn(
                name: "Fiyat",
                table: "Cars",
                newName: "fiyat");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "Cars",
                newName: "yil");

            migrationBuilder.RenameColumn(
                name: "SanzimanKodu",
                table: "Cars",
                newName: "sanziman_kodu");

            migrationBuilder.RenameColumn(
                name: "MotorKodu",
                table: "Cars",
                newName: "renk");

            migrationBuilder.RenameColumn(
                name: "ModelAdi",
                table: "Cars",
                newName: "motor_kodu");

            migrationBuilder.RenameColumn(
                name: "Gear",
                table: "Cars",
                newName: "yakitTuru");

            migrationBuilder.RenameColumn(
                name: "FuelType",
                table: "Cars",
                newName: "vites");

            migrationBuilder.RenameColumn(
                name: "DriveType",
                table: "Cars",
                newName: "cekis");

            migrationBuilder.RenameColumn(
                name: "Colour",
                table: "Cars",
                newName: "model_adi");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrls",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "vergi",
                table: "Cars",
                newName: "Vergi");

            migrationBuilder.RenameColumn(
                name: "paket",
                table: "Cars",
                newName: "Paket");

            migrationBuilder.RenameColumn(
                name: "motorHacmi",
                table: "Cars",
                newName: "MotorHacmi");

            migrationBuilder.RenameColumn(
                name: "modelraw",
                table: "Cars",
                newName: "ModelRaw");

            migrationBuilder.RenameColumn(
                name: "model",
                table: "Cars",
                newName: "Model");

            migrationBuilder.RenameColumn(
                name: "marka",
                table: "Cars",
                newName: "Marka");

            migrationBuilder.RenameColumn(
                name: "lt_100km",
                table: "Cars",
                newName: "Lt_100Km");

            migrationBuilder.RenameColumn(
                name: "kilometre",
                table: "Cars",
                newName: "Kilometre");

            migrationBuilder.RenameColumn(
                name: "fiyat",
                table: "Cars",
                newName: "Fiyat");

            migrationBuilder.RenameColumn(
                name: "yil",
                table: "Cars",
                newName: "Year");

            migrationBuilder.RenameColumn(
                name: "yakitTuru",
                table: "Cars",
                newName: "Gear");

            migrationBuilder.RenameColumn(
                name: "vites",
                table: "Cars",
                newName: "FuelType");

            migrationBuilder.RenameColumn(
                name: "sanziman_kodu",
                table: "Cars",
                newName: "SanzimanKodu");

            migrationBuilder.RenameColumn(
                name: "renk",
                table: "Cars",
                newName: "MotorKodu");

            migrationBuilder.RenameColumn(
                name: "motor_kodu",
                table: "Cars",
                newName: "ModelAdi");

            migrationBuilder.RenameColumn(
                name: "model_adi",
                table: "Cars",
                newName: "Colour");

            migrationBuilder.RenameColumn(
                name: "cekis",
                table: "Cars",
                newName: "DriveType");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrls",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
