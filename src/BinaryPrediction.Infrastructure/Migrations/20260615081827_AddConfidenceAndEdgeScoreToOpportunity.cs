using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfidenceAndEdgeScoreToOpportunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "confidence_percentage",
                table: "prediction_opportunities",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "edge_score",
                table: "prediction_opportunities",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_prediction_opportunities_confidence_percentage",
                table: "prediction_opportunities",
                column: "confidence_percentage");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_opportunities_edge_score",
                table: "prediction_opportunities",
                column: "edge_score");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_prediction_opportunities_confidence_percentage",
                table: "prediction_opportunities");

            migrationBuilder.DropIndex(
                name: "ix_prediction_opportunities_edge_score",
                table: "prediction_opportunities");

            migrationBuilder.DropColumn(
                name: "confidence_percentage",
                table: "prediction_opportunities");

            migrationBuilder.DropColumn(
                name: "edge_score",
                table: "prediction_opportunities");
        }
    }
}
