using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opportunity_analytics_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_date_utc = table.Column<DateOnly>(type: "date", nullable: false),
                    opportunity_count = table.Column<int>(type: "integer", nullable: false),
                    average_gap_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    largest_gap_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opportunity_analytics_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prediction_opportunities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prediction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_probability = table.Column<decimal>(type: "numeric", nullable: false),
                    ai_probability = table.Column<decimal>(type: "numeric", nullable: false),
                    probability_gap = table.Column<decimal>(type: "numeric", nullable: false),
                    gap_direction = table.Column<int>(type: "integer", nullable: false),
                    edge_threshold_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    has_edge = table.Column<bool>(type: "boolean", nullable: false),
                    detected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prediction_opportunities", x => x.id);
                    table.ForeignKey(
                        name: "fk_prediction_opportunities_markets_market_id",
                        column: x => x.market_id,
                        principalTable: "markets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_prediction_opportunities_predictions_prediction_id",
                        column: x => x.prediction_id,
                        principalTable: "predictions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_opportunity_analytics_snapshots_snapshot_date_utc",
                table: "opportunity_analytics_snapshots",
                column: "snapshot_date_utc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prediction_opportunities_detected_at_utc",
                table: "prediction_opportunities",
                column: "detected_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_opportunities_has_edge",
                table: "prediction_opportunities",
                column: "has_edge");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_opportunities_market_id",
                table: "prediction_opportunities",
                column: "market_id");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_opportunities_prediction_id",
                table: "prediction_opportunities",
                column: "prediction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prediction_opportunities_probability_gap",
                table: "prediction_opportunities",
                column: "probability_gap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opportunity_analytics_snapshots");

            migrationBuilder.DropTable(
                name: "prediction_opportunities");
        }
    }
}
