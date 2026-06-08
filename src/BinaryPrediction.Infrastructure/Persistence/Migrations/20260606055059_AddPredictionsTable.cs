using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "predictions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    predicted_outcome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    reasoning_summary = table.Column<string>(type: "text", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_predictions", x => x.id);
                    table.ForeignKey(
                        name: "fk_predictions_ai_analyses_analysis_id",
                        column: x => x.analysis_id,
                        principalTable: "ai_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_predictions_markets_market_id",
                        column: x => x.market_id,
                        principalTable: "markets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_predictions_analysis_id",
                table: "predictions",
                column: "analysis_id");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_created_at_utc",
                table: "predictions",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_is_active",
                table: "predictions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_market_id",
                table: "predictions",
                column: "market_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "predictions");
        }
    }
}
