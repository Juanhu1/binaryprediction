using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameConfidenceScoreAndAddUniqueMarketIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_predictions_market_id",
                table: "predictions");

            migrationBuilder.RenameColumn(
                name: "confidence_score",
                table: "predictions",
                newName: "confidence_percentage");

            migrationBuilder.RenameIndex(
                name: "ix_predictions_confidence_score",
                table: "predictions",
                newName: "ix_predictions_confidence_percentage");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_market_id",
                table: "predictions",
                column: "market_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_predictions_market_id",
                table: "predictions");

            migrationBuilder.RenameColumn(
                name: "confidence_percentage",
                table: "predictions",
                newName: "confidence_score");

            migrationBuilder.RenameIndex(
                name: "ix_predictions_confidence_percentage",
                table: "predictions",
                newName: "ix_predictions_confidence_score");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_market_id",
                table: "predictions",
                column: "market_id");
        }
    }
}
