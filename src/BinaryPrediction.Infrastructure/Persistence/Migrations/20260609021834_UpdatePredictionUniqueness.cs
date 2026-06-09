using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePredictionUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_predictions_analysis_id",
                table: "predictions");

            migrationBuilder.DropIndex(
                name: "ix_predictions_market_id",
                table: "predictions");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_analysis_id",
                table: "predictions",
                column: "analysis_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_predictions_market_id",
                table: "predictions",
                column: "market_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_predictions_analysis_id",
                table: "predictions");

            migrationBuilder.DropIndex(
                name: "ix_predictions_market_id",
                table: "predictions");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_analysis_id",
                table: "predictions",
                column: "analysis_id");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_market_id",
                table: "predictions",
                column: "market_id",
                unique: true);
        }
    }
}
