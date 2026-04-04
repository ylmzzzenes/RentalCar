using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalCar.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ReplaceCarYearWithModelYear : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ModelYear",
            table: "Cars",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE [Cars] SET [ModelYear] = YEAR([yil]) WHERE [yil] IS NOT NULL;
            """);

        migrationBuilder.DropColumn(
            name: "yil",
            table: "Cars");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "yil",
            table: "Cars",
            type: "datetime2",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE [Cars] SET [yil] = DATEFROMPARTS(COALESCE([ModelYear], 2000), 1, 1) WHERE [ModelYear] IS NOT NULL;
            """);

        migrationBuilder.DropColumn(
            name: "ModelYear",
            table: "Cars");
    }
}
