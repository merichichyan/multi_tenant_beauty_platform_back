using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace multi_tenant_beauty_platform_back.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonIdToSpecialist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SalonId",
                table: "Specialists",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalonId",
                table: "Specialists");
        }
    }
}
