using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsSnapshots2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_markets_prediction_categories_prediction_category_id",
                table: "markets");

            migrationBuilder.AlterColumn<Guid>(
                name: "prediction_category_id",
                table: "markets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "calibration_trend_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    confidence_range = table.Column<string>(type: "text", nullable: false),
                    snapshot_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    prediction_count = table.Column<int>(type: "integer", nullable: false),
                    actual_accuracy_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    expected_accuracy_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    calibration_error = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calibration_trend_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category_performance_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    prediction_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    prediction_count = table.Column<int>(type: "integer", nullable: false),
                    correct_predictions = table.Column<int>(type: "integer", nullable: false),
                    accuracy_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    average_brier_score = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_performance_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_performance_snapshots_prediction_categories_predic",
                        column: x => x.prediction_category_id,
                        principalTable: "prediction_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prediction_performance_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_predictions = table.Column<int>(type: "integer", nullable: false),
                    correct_predictions = table.Column<int>(type: "integer", nullable: false),
                    accuracy_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    average_brier_score = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prediction_performance_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_calibration_trend_snapshots_confidence_range_snapshot_date_",
                table: "calibration_trend_snapshots",
                columns: new[] { "confidence_range", "snapshot_date_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_performance_snapshots_prediction_category_id_snaps",
                table: "category_performance_snapshots",
                columns: new[] { "prediction_category_id", "snapshot_date_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prediction_performance_snapshots_snapshot_date_utc",
                table: "prediction_performance_snapshots",
                column: "snapshot_date_utc",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_markets_prediction_categories_prediction_category_id",
                table: "markets",
                column: "prediction_category_id",
                principalTable: "prediction_categories",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_markets_prediction_categories_prediction_category_id",
                table: "markets");

            migrationBuilder.DropTable(
                name: "calibration_trend_snapshots");

            migrationBuilder.DropTable(
                name: "category_performance_snapshots");

            migrationBuilder.DropTable(
                name: "prediction_performance_snapshots");

            migrationBuilder.AlterColumn<Guid>(
                name: "prediction_category_id",
                table: "markets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_markets_prediction_categories_prediction_category_id",
                table: "markets",
                column: "prediction_category_id",
                principalTable: "prediction_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
