using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autosalon_OneZone.Migrations
{
    /// <inheritdoc />
    public partial class InitialModelWithIdentityAndKorpaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prezime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kartice",
                columns: table => new
                {
                    KarticaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrojKartice = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DatumIsteka = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Cvv = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    ImeVlasnika = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kartice", x => x.KarticaID);
                });

            migrationBuilder.CreateTable(
                name: "Krediti",
                columns: table => new
                {
                    KreditID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BrojRata = table.Column<int>(type: "int", nullable: false),
                    KamatnaStopa = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MjesecnaRata = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Krediti", x => x.KreditID);
                });

            migrationBuilder.CreateTable(
                name: "Vozila",
                columns: table => new
                {
                    VoziloID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marka = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Godiste = table.Column<int>(type: "int", nullable: false),
                    Gorivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kubikaza = table.Column<double>(type: "float", nullable: false),
                    Boja = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Kilometraza = table.Column<double>(type: "float", nullable: false),
                    Cijena = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Slika = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vozila", x => x.VoziloID);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Korpe",
                columns: table => new
                {
                    KorpaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UkupnaCijena = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korpe", x => x.KorpaID);
                    table.ForeignKey(
                        name: "FK_Korpe_AspNetUsers_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Narudzbe",
                columns: table => new
                {
                    NarudzbaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatumNarudzbe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UkupnaCijena = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KorisnikId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Narudzbe", x => x.NarudzbaID);
                    table.ForeignKey(
                        name: "FK_Narudzbe_AspNetUsers_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PodrskaUpiti",
                columns: table => new
                {
                    UpitID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naslov = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sadrzaj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatumUpita = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KorisnikId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PodrskaUpiti", x => x.UpitID);
                    table.ForeignKey(
                        name: "FK_PodrskaUpiti_AspNetUsers_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recenzije",
                columns: table => new
                {
                    RecenzijaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ocjena = table.Column<int>(type: "int", nullable: false),
                    Komentar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DatumRecenzije = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KorisnikId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VoziloID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recenzije", x => x.RecenzijaID);
                    table.ForeignKey(
                        name: "FK_Recenzije_AspNetUsers_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recenzije_Vozila_VoziloID",
                        column: x => x.VoziloID,
                        principalTable: "Vozila",
                        principalColumn: "VoziloID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Placanja",
                columns: table => new
                {
                    NarudzbaID = table.Column<int>(type: "int", nullable: false),
                    DatumPlacanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KarticaID = table.Column<int>(type: "int", nullable: true),
                    KreditID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Placanja", x => x.NarudzbaID);
                    table.ForeignKey(
                        name: "FK_Placanja_Kartice_KarticaID",
                        column: x => x.KarticaID,
                        principalTable: "Kartice",
                        principalColumn: "KarticaID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Placanja_Krediti_KreditID",
                        column: x => x.KreditID,
                        principalTable: "Krediti",
                        principalColumn: "KreditID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Placanja_Narudzbe_NarudzbaID",
                        column: x => x.NarudzbaID,
                        principalTable: "Narudzbe",
                        principalColumn: "NarudzbaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StavkeKorpe",
                columns: table => new
                {
                    StavkaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kolicina = table.Column<int>(type: "int", nullable: false),
                    CijenaStavke = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VoziloID = table.Column<int>(type: "int", nullable: false),
                    KorpaID = table.Column<int>(type: "int", nullable: true),
                    NarudzbaID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StavkeKorpe", x => x.StavkaID);
                    table.ForeignKey(
                        name: "FK_StavkeKorpe_Korpe_KorpaID",
                        column: x => x.KorpaID,
                        principalTable: "Korpe",
                        principalColumn: "KorpaID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StavkeKorpe_Narudzbe_NarudzbaID",
                        column: x => x.NarudzbaID,
                        principalTable: "Narudzbe",
                        principalColumn: "NarudzbaID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StavkeKorpe_Vozila_VoziloID",
                        column: x => x.VoziloID,
                        principalTable: "Vozila",
                        principalColumn: "VoziloID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Korpe_KorisnikId",
                table: "Korpe",
                column: "KorisnikId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Narudzbe_KorisnikId",
                table: "Narudzbe",
                column: "KorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Placanja_KarticaID",
                table: "Placanja",
                column: "KarticaID");

            migrationBuilder.CreateIndex(
                name: "IX_Placanja_KreditID",
                table: "Placanja",
                column: "KreditID");

            migrationBuilder.CreateIndex(
                name: "IX_PodrskaUpiti_KorisnikId",
                table: "PodrskaUpiti",
                column: "KorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Recenzije_KorisnikId",
                table: "Recenzije",
                column: "KorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_Recenzije_VoziloID",
                table: "Recenzije",
                column: "VoziloID");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeKorpe_KorpaID",
                table: "StavkeKorpe",
                column: "KorpaID");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeKorpe_NarudzbaID",
                table: "StavkeKorpe",
                column: "NarudzbaID");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeKorpe_VoziloID",
                table: "StavkeKorpe",
                column: "VoziloID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Placanja");

            migrationBuilder.DropTable(
                name: "PodrskaUpiti");

            migrationBuilder.DropTable(
                name: "Recenzije");

            migrationBuilder.DropTable(
                name: "StavkeKorpe");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Kartice");

            migrationBuilder.DropTable(
                name: "Krediti");

            migrationBuilder.DropTable(
                name: "Korpe");

            migrationBuilder.DropTable(
                name: "Narudzbe");

            migrationBuilder.DropTable(
                name: "Vozila");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
