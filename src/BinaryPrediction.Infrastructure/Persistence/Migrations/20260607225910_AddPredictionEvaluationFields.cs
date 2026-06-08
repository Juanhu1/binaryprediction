using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionEvaluationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "actual_outcome",
                table: "predictions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "brier_score",
                table: "predictions",
                type: "numeric(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "evaluated_at_utc",
                table: "predictions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "was_correct",
                table: "predictions",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_predictions_confidence_score",
                table: "predictions",
                column: "confidence_score");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_evaluated_at_utc",
                table: "predictions",
                column: "evaluated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_predictions_was_correct",
                table: "predictions",
                column: "was_correct");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_predictions_confidence_score",
                table: "predictions");

            migrationBuilder.DropIndex(
                name: "ix_predictions_evaluated_at_utc",
                table: "predictions");

            migrationBuilder.DropIndex(
                name: "ix_predictions_was_correct",
                table: "predictions");

            migrationBuilder.DropColumn(
                name: "actual_outcome",
                table: "predictions");

            migrationBuilder.DropColumn(
                name: "brier_score",
                table: "predictions");

            migrationBuilder.DropColumn(
                name: "evaluated_at_utc",
                table: "predictions");

            migrationBuilder.DropColumn(
                name: "was_correct",
                table: "predictions");
        }
    }
}
