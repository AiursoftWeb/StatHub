using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.StatHub.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class RemoveThemeInDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferDarkTheme",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PreferDarkTheme",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
