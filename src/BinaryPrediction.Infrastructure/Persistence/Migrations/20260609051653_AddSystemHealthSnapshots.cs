using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemHealthSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_health_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_markets = table.Column<int>(type: "integer", nullable: false),
                    active_markets = table.Column<int>(type: "integer", nullable: false),
                    resolved_markets = table.Column<int>(type: "integer", nullable: false),
                    queued_markets = table.Column<int>(type: "integer", nullable: false),
                    processing_markets = table.Column<int>(type: "integer", nullable: false),
                    completed_markets = table.Column<int>(type: "integer", nullable: false),
                    failed_markets = table.Column<int>(type: "integer", nullable: false),
                    total_analyses = table.Column<int>(type: "integer", nullable: false),
                    total_predictions = table.Column<int>(type: "integer", nullable: false),
                    prediction_accuracy = table.Column<decimal>(type: "numeric", nullable: false),
                    average_brier_score = table.Column<decimal>(type: "numeric", nullable: false),
                    last_market_collection_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_analysis_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_prediction_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_evaluation_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_health_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_system_health_snapshots_created_at_utc",
                table: "system_health_snapshots",
                column: "created_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_health_snapshots");
        }
    }
}
