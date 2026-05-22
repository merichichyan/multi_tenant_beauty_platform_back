using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace multi_tenant_beauty_platform_back.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToTpcMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceItems_SpecialistProfiles_SpecialistProfileId",
                table: "ServiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_SalonProfiles_SalonProfileId",
                table: "StaffMembers");

            migrationBuilder.DropTable(
                name: "SalonProfiles");

            migrationBuilder.DropTable(
                name: "SpecialistProfiles");

            migrationBuilder.RenameColumn(
                name: "SalonProfileId",
                table: "StaffMembers",
                newName: "SalonId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMembers_SalonProfileId",
                table: "StaffMembers",
                newName: "IX_StaffMembers_SalonId");

            migrationBuilder.RenameColumn(
                name: "SpecialistProfileId",
                table: "ServiceItems",
                newName: "SpecialistId");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceItems_SpecialistProfileId",
                table: "ServiceItems",
                newName: "IX_ServiceItems_SpecialistId");

            migrationBuilder.CreateTable(
                name: "Salons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    Birthday = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    IsOnboardingCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SalonName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SocialMedias = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    PreferredColors = table.Column<string>(type: "text", nullable: true),
                    OperatingHours = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    Birthday = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    IsOnboardingCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SocialMedias = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    PreferredColors = table.Column<string>(type: "text", nullable: true),
                    WorkingHours = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialists", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Salons_Email",
                table: "Salons",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialists_Email",
                table: "Specialists",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceItems_Specialists_SpecialistId",
                table: "ServiceItems",
                column: "SpecialistId",
                principalTable: "Specialists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_Salons_SalonId",
                table: "StaffMembers",
                column: "SalonId",
                principalTable: "Salons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceItems_Specialists_SpecialistId",
                table: "ServiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_Salons_SalonId",
                table: "StaffMembers");

            migrationBuilder.DropTable(
                name: "Salons");

            migrationBuilder.DropTable(
                name: "Specialists");

            migrationBuilder.RenameColumn(
                name: "SalonId",
                table: "StaffMembers",
                newName: "SalonProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMembers_SalonId",
                table: "StaffMembers",
                newName: "IX_StaffMembers_SalonProfileId");

            migrationBuilder.RenameColumn(
                name: "SpecialistId",
                table: "ServiceItems",
                newName: "SpecialistProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceItems_SpecialistId",
                table: "ServiceItems",
                newName: "IX_ServiceItems_SpecialistProfileId");

            migrationBuilder.CreateTable(
                name: "SalonProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    OperatingHours = table.Column<string>(type: "text", nullable: true),
                    PreferredColors = table.Column<string>(type: "text", nullable: true),
                    SalonName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SocialMedias = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalonProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalonProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecialistProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    PreferredColors = table.Column<string>(type: "text", nullable: true),
                    SocialMedias = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkingHours = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialistProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialistProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalonProfiles_UserId",
                table: "SalonProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecialistProfiles_UserId",
                table: "SpecialistProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceItems_SpecialistProfiles_SpecialistProfileId",
                table: "ServiceItems",
                column: "SpecialistProfileId",
                principalTable: "SpecialistProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_SalonProfiles_SalonProfileId",
                table: "StaffMembers",
                column: "SalonProfileId",
                principalTable: "SalonProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
