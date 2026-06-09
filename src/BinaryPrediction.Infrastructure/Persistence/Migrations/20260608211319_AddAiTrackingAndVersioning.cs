using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTrackingAndVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "prompt_version_used",
                table: "predictions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ai_usage_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: false),
                    completion_tokens = table.Column<int>(type: "integer", nullable: false),
                    total_tokens = table.Column<int>(type: "integer", nullable: false),
                    estimated_cost_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_usage_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prompt_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    prompt_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_template = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prompt_versions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_records_created_at_utc",
                table: "ai_usage_records",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_records_model",
                table: "ai_usage_records",
                column: "model");

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_records_operation_type",
                table: "ai_usage_records",
                column: "operation_type");

            migrationBuilder.CreateIndex(
                name: "ix_prompt_versions_prompt_name",
                table: "prompt_versions",
                column: "prompt_name");

            migrationBuilder.CreateIndex(
                name: "ix_prompt_versions_version",
                table: "prompt_versions",
                column: "version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usage_records");

            migrationBuilder.DropTable(
                name: "prompt_versions");

            migrationBuilder.DropColumn(
                name: "prompt_version_used",
                table: "predictions");
        }
    }
}
