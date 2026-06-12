using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePredictionPerformanceSnapshotDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "prediction_error",
                table: "predictions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "snapshot_date_utc",
                table: "prediction_performance_snapshots",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<decimal>(
                name: "average_confidence",
                table: "prediction_performance_snapshots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "average_prediction_error",
                table: "prediction_performance_snapshots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "incorrect_predictions",
                table: "prediction_performance_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_prediction_performance_snapshots_created_at_utc",
                table: "prediction_performance_snapshots",
                column: "created_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_prediction_performance_snapshots_created_at_utc",
                table: "prediction_performance_snapshots");

            migrationBuilder.DropColumn(
                name: "prediction_error",
                table: "predictions");

            migrationBuilder.DropColumn(
                name: "average_confidence",
                table: "prediction_performance_snapshots");

            migrationBuilder.DropColumn(
                name: "average_prediction_error",
                table: "prediction_performance_snapshots");

            migrationBuilder.DropColumn(
                name: "incorrect_predictions",
                table: "prediction_performance_snapshots");

            migrationBuilder.AlterColumn<DateTime>(
                name: "snapshot_date_utc",
                table: "prediction_performance_snapshots",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");
        }
    }
}
