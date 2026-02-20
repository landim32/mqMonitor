using Microsoft.EntityFrameworkCore.Migrations;

namespace Test.Infrastructure.Database.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "test_executions",
            columns: table => new
            {
                test_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                worker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_test_executions", x => x.test_id);
            });

        migrationBuilder.CreateTable(
            name: "event_logs",
            columns: table => new
            {
                event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_event_logs", x => x.event_id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_test_executions_status",
            table: "test_executions",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "IX_test_executions_updated_at",
            table: "test_executions",
            column: "updated_at");

        migrationBuilder.CreateIndex(
            name: "IX_event_logs_type",
            table: "event_logs",
            column: "type");

        migrationBuilder.CreateIndex(
            name: "IX_event_logs_timestamp",
            table: "event_logs",
            column: "timestamp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "test_executions");
        migrationBuilder.DropTable(name: "event_logs");
    }
}
