using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace multi_tenant_beauty_platform_back.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCategoryTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ServiceCategories",
                newName: "NameRu");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "ServiceCategories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameHy",
                table: "ServiceCategories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "ServiceCategories");

            migrationBuilder.DropColumn(
                name: "NameHy",
                table: "ServiceCategories");

            migrationBuilder.RenameColumn(
                name: "NameRu",
                table: "ServiceCategories",
                newName: "Name");
        }
    }
}
