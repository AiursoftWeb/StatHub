using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.StatHub.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class MetricsExportSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetricsExportPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExportAllServers = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricsExportPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetricsExportTargets",
                columns: table => new
                {
                    PolicyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsExcluded = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricsExportTargets", x => new { x.PolicyId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_MetricsExportTargets_MetricsExportPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "MetricsExportPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetricsExportTargets");

            migrationBuilder.DropTable(
                name: "MetricsExportPolicies");
        }
    }
}
