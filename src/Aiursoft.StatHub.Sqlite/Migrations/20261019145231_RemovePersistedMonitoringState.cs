using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.StatHub.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class RemovePersistedMonitoringState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentMonitoringStates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentMonitoringStates",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrentState = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Hostname = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LastReportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NextReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObservationSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PendingRecoveryAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RedOrWorseSinceUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StateSinceUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    YellowOrWorseSinceUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMonitoringStates", x => x.ClientId);
                });
        }
    }
}
