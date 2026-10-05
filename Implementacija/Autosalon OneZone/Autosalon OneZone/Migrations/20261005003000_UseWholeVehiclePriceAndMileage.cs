using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autosalon_OneZone.Migrations
{
    /// <inheritdoc />
    public partial class UseWholeVehiclePriceAndMileage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Vozila]
                SET [Cijena] = ROUND([Cijena], 0)
                WHERE [Cijena] IS NOT NULL;

                UPDATE [Vozila]
                SET [Kilometraza] = ROUND([Kilometraza], 0)
                WHERE [Kilometraza] IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Cijena",
                table: "Vozila",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Kilometraza",
                table: "Vozila",
                type: "int",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Cijena",
                table: "Vozila",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Kilometraza",
                table: "Vozila",
                type: "float",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
