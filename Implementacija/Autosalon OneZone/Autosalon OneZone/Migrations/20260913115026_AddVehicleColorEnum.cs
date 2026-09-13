using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autosalon_OneZone.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleColorEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Vozila]
                SET [Boja] = CASE [Boja]
                    WHEN N'Bijela' THEN N'Bijela'
                    WHEN N'White' THEN N'Bijela'
                    WHEN N'Crna' THEN N'Crna'
                    WHEN N'Black' THEN N'Crna'
                    WHEN N'Siva' THEN N'Siva'
                    WHEN N'Gray' THEN N'Siva'
                    WHEN N'Grey' THEN N'Siva'
                    WHEN N'Srebrna' THEN N'Srebrna'
                    WHEN N'Silver' THEN N'Srebrna'
                    WHEN N'Zlatna' THEN N'Zlatna'
                    WHEN N'Gold' THEN N'Zlatna'
                    WHEN N'Bež' THEN N'Bez'
                    WHEN N'Beige' THEN N'Bez'
                    WHEN N'Smeđa' THEN N'Smedja'
                    WHEN N'Brown' THEN N'Smedja'
                    WHEN N'Crvena' THEN N'Crvena'
                    WHEN N'Red' THEN N'Crvena'
                    WHEN N'Plava' THEN N'Plava'
                    WHEN N'Blue' THEN N'Plava'
                    WHEN N'Zelena' THEN N'Zelena'
                    WHEN N'Green' THEN N'Zelena'
                    WHEN N'Žuta' THEN N'Zuta'
                    WHEN N'Yellow' THEN N'Zuta'
                    WHEN N'Narandžasta' THEN N'Narandzasta'
                    WHEN N'Orange' THEN N'Narandzasta'
                    WHEN N'Ljubičasta' THEN N'Ljubicasta'
                    WHEN N'Purple' THEN N'Ljubicasta'
                    WHEN N'Roza' THEN N'Roza'
                    WHEN N'Pink' THEN N'Roza'
                    WHEN N'Višebojna' THEN N'Visebojna'
                    WHEN N'Multicolor' THEN N'Visebojna'
                    WHEN N'Ostalo' THEN N'Ostalo'
                    WHEN N'Other' THEN N'Ostalo'
                    ELSE N'Ostalo'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Boja",
                table: "Vozila",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Vozila]
                SET [Boja] = CASE [Boja]
                    WHEN N'Bez' THEN N'Bež'
                    WHEN N'Smedja' THEN N'Smeđa'
                    WHEN N'Zuta' THEN N'Žuta'
                    WHEN N'Narandzasta' THEN N'Narandžasta'
                    WHEN N'Ljubicasta' THEN N'Ljubičasta'
                    WHEN N'Visebojna' THEN N'Višebojna'
                    ELSE [Boja]
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Boja",
                table: "Vozila",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
