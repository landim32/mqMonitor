using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MqMonitor.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_logs",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    process_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_logs", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "process_executions",
                columns: table => new
                {
                    process_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    worker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    current_stage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    saga_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_executions", x => x.process_id);
                });

            migrationBuilder.CreateTable(
                name: "saga_steps",
                columns: table => new
                {
                    step_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    process_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    stage_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    worker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    step_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saga_steps", x => x.step_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_logs_process_id",
                table: "event_logs",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_logs_timestamp",
                table: "event_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_event_logs_type",
                table: "event_logs",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_process_executions_current_stage",
                table: "process_executions",
                column: "current_stage");

            migrationBuilder.CreateIndex(
                name: "IX_process_executions_priority",
                table: "process_executions",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "IX_process_executions_saga_status",
                table: "process_executions",
                column: "saga_status");

            migrationBuilder.CreateIndex(
                name: "IX_process_executions_status",
                table: "process_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_process_executions_updated_at",
                table: "process_executions",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "IX_saga_steps_process_id",
                table: "saga_steps",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "IX_saga_steps_process_id_step_order",
                table: "saga_steps",
                columns: new[] { "process_id", "step_order" });

            migrationBuilder.CreateIndex(
                name: "IX_saga_steps_stage_name",
                table: "saga_steps",
                column: "stage_name");

            migrationBuilder.CreateIndex(
                name: "IX_saga_steps_status",
                table: "saga_steps",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_logs");

            migrationBuilder.DropTable(
                name: "process_executions");

            migrationBuilder.DropTable(
                name: "saga_steps");
        }
    }
}
