using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageRecordSuccessAndLatency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "ai_usage_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_success",
                table: "ai_usage_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "latency_ms",
                table: "ai_usage_records",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error_message",
                table: "ai_usage_records");

            migrationBuilder.DropColumn(
                name: "is_success",
                table: "ai_usage_records");

            migrationBuilder.DropColumn(
                name: "latency_ms",
                table: "ai_usage_records");
        }
    }
}
