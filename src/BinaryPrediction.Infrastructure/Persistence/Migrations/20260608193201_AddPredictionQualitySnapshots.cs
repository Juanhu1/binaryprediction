using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionQualitySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prediction_quality_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    total_predictions = table.Column<int>(type: "integer", nullable: false),
                    accuracy_percentage = table.Column<double>(type: "double precision", nullable: false),
                    average_brier_score = table.Column<double>(type: "double precision", nullable: false),
                    calibration_error = table.Column<double>(type: "double precision", nullable: false),
                    benchmark_advantage = table.Column<double>(type: "double precision", nullable: false),
                    improvement_trend = table.Column<double>(type: "double precision", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prediction_quality_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_prediction_quality_snapshots_created_at_utc",
                table: "prediction_quality_snapshots",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_quality_snapshots_snapshot_date_utc",
                table: "prediction_quality_snapshots",
                column: "snapshot_date_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prediction_quality_snapshots");
        }
    }
}
