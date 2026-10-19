using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.StatHub.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class SeverityThresholdTimers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RedOrWorseSinceUtc",
                table: "AgentMonitoringStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "YellowOrWorseSinceUtc",
                table: "AgentMonitoringStates",
                type: "TEXT",
                nullable: true);
            // Only the last exact-color interval is known for existing records.
            migrationBuilder.Sql("UPDATE AgentMonitoringStates SET YellowOrWorseSinceUtc = StateSinceUtc WHERE CurrentState IN ('yellow', 'red', 'grey')");
            migrationBuilder.Sql("UPDATE AgentMonitoringStates SET RedOrWorseSinceUtc = StateSinceUtc WHERE CurrentState IN ('red', 'grey')");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedOrWorseSinceUtc",
                table: "AgentMonitoringStates");

            migrationBuilder.DropColumn(
                name: "YellowOrWorseSinceUtc",
                table: "AgentMonitoringStates");
        }
    }
}
