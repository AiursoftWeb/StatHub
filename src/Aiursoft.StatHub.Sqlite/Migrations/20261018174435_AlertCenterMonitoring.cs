using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.StatHub.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AlertCenterMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentMonitoringStates",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrentState = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    LastReportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StateSinceUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ObservationSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PendingRecoveryAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextReportId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMonitoringStates", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "AlertMonitoringPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonitorAllServers = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoResolveOnGreen = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertMonitoringPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertMonitoringRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PolicyId = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertMonitoringRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertMonitoringRules_AlertMonitoringPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "AlertMonitoringPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertMonitoringTargets",
                columns: table => new
                {
                    PolicyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsExcluded = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertMonitoringTargets", x => new { x.PolicyId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_AlertMonitoringTargets_AlertMonitoringPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "AlertMonitoringPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertMonitoringRules_PolicyId",
                table: "AlertMonitoringRules",
                column: "PolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentMonitoringStates");

            migrationBuilder.DropTable(
                name: "AlertMonitoringRules");

            migrationBuilder.DropTable(
                name: "AlertMonitoringTargets");

            migrationBuilder.DropTable(
                name: "AlertMonitoringPolicies");
        }
    }
}
