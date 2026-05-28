using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace multi_tenant_beauty_platform_back.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingPriceAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "Specialists",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Specialists",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "StartingPrice",
                table: "Specialists",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "Salons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Salons",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "StartingPrice",
                table: "Salons",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "StartingPrice",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "Salons");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Salons");

            migrationBuilder.DropColumn(
                name: "StartingPrice",
                table: "Salons");
        }
    }
}
