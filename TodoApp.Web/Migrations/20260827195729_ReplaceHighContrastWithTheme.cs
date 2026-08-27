using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceHighContrastWithTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighContrastMode",
                table: "UserPreferences");

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "UserPreferences",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Theme",
                table: "UserPreferences");

            migrationBuilder.AddColumn<bool>(
                name: "HighContrastMode",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
