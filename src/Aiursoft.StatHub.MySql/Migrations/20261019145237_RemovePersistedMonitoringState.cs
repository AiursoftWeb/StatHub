using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.StatHub.MySql.Migrations
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
                    ClientId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentState = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Hostname = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastReportedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NextReportId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ObservationSessionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PendingRecoveryAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RedOrWorseSinceUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StateSinceUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    YellowOrWorseSinceUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMonitoringStates", x => x.ClientId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
