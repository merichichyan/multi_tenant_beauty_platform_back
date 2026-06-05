using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace multi_tenant_beauty_platform_back.Migrations
{
    /// <inheritdoc />
    public partial class AddFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteSalons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteSalons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FavoriteSpecialists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialistId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteSpecialists", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteSalons_UserId_SalonId",
                table: "FavoriteSalons",
                columns: new[] { "UserId", "SalonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteSpecialists_UserId_SpecialistId",
                table: "FavoriteSpecialists",
                columns: new[] { "UserId", "SpecialistId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteSalons");

            migrationBuilder.DropTable(
                name: "FavoriteSpecialists");
        }
    }
}
