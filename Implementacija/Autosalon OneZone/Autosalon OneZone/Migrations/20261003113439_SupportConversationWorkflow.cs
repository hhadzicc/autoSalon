using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autosalon_OneZone.Migrations
{
    /// <inheritdoc />
    public partial class SupportConversationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PodrskaUpiti",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumDodjele",
                table: "PodrskaUpiti",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumPodsjetnika",
                table: "PodrskaUpiti",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumRjesavanja",
                table: "PodrskaUpiti",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumZadnjeAktivnosti",
                table: "PodrskaUpiti",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumZatvaranja",
                table: "PodrskaUpiti",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DodijeljenKorisnikId",
                table: "PodrskaUpiti",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Jezik",
                table: "PodrskaUpiti",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en-US");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PodrskaUpiti",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "PorukePodrske",
                columns: table => new
                {
                    PorukaPodrskeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UpitID = table.Column<int>(type: "int", nullable: false),
                    PosiljalacId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TipAutora = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Sadrzaj = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    DatumSlanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcitanaUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PorukePodrske", x => x.PorukaPodrskeID);
                    table.ForeignKey(
                        name: "FK_PorukePodrske_AspNetUsers_PosiljalacId",
                        column: x => x.PosiljalacId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PorukePodrske_PodrskaUpiti_UpitID",
                        column: x => x.UpitID,
                        principalTable: "PodrskaUpiti",
                        principalColumn: "UpitID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailPodrskeOutbox",
                columns: table => new
                {
                    EmailPodrskeOutboxID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PorukaPodrskeID = table.Column<int>(type: "int", nullable: false),
                    Primalac = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Jezik = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    KreiranoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SljedeciPokusajUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BrojPokusaja = table.Column<int>(type: "int", nullable: false),
                    PoslanoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ZadnjaGreska = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DetaljiUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailPodrskeOutbox", x => x.EmailPodrskeOutboxID);
                    table.ForeignKey(
                        name: "FK_EmailPodrskeOutbox_PorukePodrske_PorukaPodrskeID",
                        column: x => x.PorukaPodrskeID,
                        principalTable: "PorukePodrske",
                        principalColumn: "PorukaPodrskeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [PorukePodrske] ([UpitID], [PosiljalacId], [TipAutora], [Sadrzaj], [DatumSlanja], [ProcitanaUtc])
                SELECT [UpitID], [KorisnikId], N'Korisnik', [Sadrzaj], [DatumUpita], NULL
                FROM [PodrskaUpiti];

                UPDATE [PodrskaUpiti]
                SET [DatumZadnjeAktivnosti] = [DatumUpita],
                    [Jezik] = N'bs-Latn-BA',
                    [Status] = CASE
                        WHEN [Status] = N'Poslat' THEN N'CekaPodrsku'
                        WHEN [Status] = N'Odgovoren' THEN N'Rijesen'
                        WHEN [Status] = N'UObradi' THEN N'CekaPodrsku'
                        WHEN [Status] = N'Zatvoren' THEN N'Zatvoren'
                        ELSE N'CekaPodrsku'
                    END;
                """);

            migrationBuilder.DropColumn(
                name: "Sadrzaj",
                table: "PodrskaUpiti");

            migrationBuilder.CreateIndex(
                name: "IX_PodrskaUpiti_DodijeljenKorisnikId",
                table: "PodrskaUpiti",
                column: "DodijeljenKorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_PodrskaUpiti_Status_DatumZadnjeAktivnosti",
                table: "PodrskaUpiti",
                columns: new[] { "Status", "DatumZadnjeAktivnosti" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailPodrskeOutbox_PorukaPodrskeID",
                table: "EmailPodrskeOutbox",
                column: "PorukaPodrskeID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailPodrskeOutbox_PoslanoUtc_SljedeciPokusajUtc",
                table: "EmailPodrskeOutbox",
                columns: new[] { "PoslanoUtc", "SljedeciPokusajUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PorukePodrske_PosiljalacId",
                table: "PorukePodrske",
                column: "PosiljalacId");

            migrationBuilder.CreateIndex(
                name: "IX_PorukePodrske_UpitID_DatumSlanja",
                table: "PorukePodrske",
                columns: new[] { "UpitID", "DatumSlanja" });

            migrationBuilder.CreateIndex(
                name: "IX_PorukePodrske_UpitID_TipAutora_ProcitanaUtc",
                table: "PorukePodrske",
                columns: new[] { "UpitID", "TipAutora", "ProcitanaUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_PodrskaUpiti_AspNetUsers_DodijeljenKorisnikId",
                table: "PodrskaUpiti",
                column: "DodijeljenKorisnikId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sadrzaj",
                table: "PodrskaUpiti",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE ticket
                SET ticket.[Sadrzaj] = COALESCE(firstMessage.[Sadrzaj], N'')
                FROM [PodrskaUpiti] AS ticket
                OUTER APPLY (
                    SELECT TOP (1) message.[Sadrzaj]
                    FROM [PorukePodrske] AS message
                    WHERE message.[UpitID] = ticket.[UpitID]
                    ORDER BY message.[DatumSlanja], message.[PorukaPodrskeID]
                ) AS firstMessage;

                UPDATE [PodrskaUpiti]
                SET [Status] = CASE
                    WHEN [Status] = N'CekaPodrsku' THEN N'Poslat'
                    WHEN [Status] IN (N'CekaKorisnika', N'Rijesen') THEN N'Odgovoren'
                    ELSE [Status]
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_PodrskaUpiti_AspNetUsers_DodijeljenKorisnikId",
                table: "PodrskaUpiti");

            migrationBuilder.DropTable(
                name: "EmailPodrskeOutbox");

            migrationBuilder.DropTable(
                name: "PorukePodrske");

            migrationBuilder.DropIndex(
                name: "IX_PodrskaUpiti_DodijeljenKorisnikId",
                table: "PodrskaUpiti");

            migrationBuilder.DropIndex(
                name: "IX_PodrskaUpiti_Status_DatumZadnjeAktivnosti",
                table: "PodrskaUpiti");

            migrationBuilder.DropColumn(
                name: "DatumDodjele",
                table: "PodrskaUpiti");

            migrationBuilder.DropColumn(
                name: "DatumPodsjetnika",
                table: "PodrskaUpiti");

            migrationBuilder.DropColumn(
                name: "DatumRjesavanja",
                table: "PodrskaUpiti");

            migrationBuilder.DropColumn(
                name: "DatumZadnjeAktivnosti",
                table: "PodrskaUpiti");

            migrationBuilder.DropColumn(
                name: "DatumZatvaranja",
                table: "PodrskaUpiti");

            migrationBuilder.DropColumn(
                name: "DodijeljenKorisnikId",
                table: "PodrskaUpiti");

            migrationBuilder.DropColumn(
                name: "Jezik",
                table: "PodrskaUpiti");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PodrskaUpiti");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PodrskaUpiti",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

        }
    }
}
