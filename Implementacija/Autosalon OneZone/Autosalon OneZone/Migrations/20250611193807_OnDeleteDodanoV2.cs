using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autosalon_OneZone.Migrations
{
    /// <inheritdoc />
    public partial class OnDeleteDodanoV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Narudzbe_AspNetUsers_KorisnikId",
                table: "Narudzbe");

            migrationBuilder.DropForeignKey(
                name: "FK_PodrskaUpiti_AspNetUsers_KorisnikId",
                table: "PodrskaUpiti");

            migrationBuilder.DropForeignKey(
                name: "FK_StavkeKorpe_Narudzbe_NarudzbaID",
                table: "StavkeKorpe");

            migrationBuilder.DropForeignKey(
                name: "FK_StavkeKorpe_Vozila_VoziloID",
                table: "StavkeKorpe");

            migrationBuilder.AddForeignKey(
                name: "FK_Narudzbe_AspNetUsers_KorisnikId",
                table: "Narudzbe",
                column: "KorisnikId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PodrskaUpiti_AspNetUsers_KorisnikId",
                table: "PodrskaUpiti",
                column: "KorisnikId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StavkeKorpe_Narudzbe_NarudzbaID",
                table: "StavkeKorpe",
                column: "NarudzbaID",
                principalTable: "Narudzbe",
                principalColumn: "NarudzbaID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StavkeKorpe_Vozila_VoziloID",
                table: "StavkeKorpe",
                column: "VoziloID",
                principalTable: "Vozila",
                principalColumn: "VoziloID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Narudzbe_AspNetUsers_KorisnikId",
                table: "Narudzbe");

            migrationBuilder.DropForeignKey(
                name: "FK_PodrskaUpiti_AspNetUsers_KorisnikId",
                table: "PodrskaUpiti");

            migrationBuilder.DropForeignKey(
                name: "FK_StavkeKorpe_Narudzbe_NarudzbaID",
                table: "StavkeKorpe");

            migrationBuilder.DropForeignKey(
                name: "FK_StavkeKorpe_Vozila_VoziloID",
                table: "StavkeKorpe");

            migrationBuilder.AddForeignKey(
                name: "FK_Narudzbe_AspNetUsers_KorisnikId",
                table: "Narudzbe",
                column: "KorisnikId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PodrskaUpiti_AspNetUsers_KorisnikId",
                table: "PodrskaUpiti",
                column: "KorisnikId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StavkeKorpe_Narudzbe_NarudzbaID",
                table: "StavkeKorpe",
                column: "NarudzbaID",
                principalTable: "Narudzbe",
                principalColumn: "NarudzbaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StavkeKorpe_Vozila_VoziloID",
                table: "StavkeKorpe",
                column: "VoziloID",
                principalTable: "Vozila",
                principalColumn: "VoziloID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
