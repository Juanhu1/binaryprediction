using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionAnalyticsTablesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "prediction_category_id",
                table: "markets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "prediction_calibration_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confidence_range = table.Column<string>(type: "text", nullable: false),
                    prediction_count = table.Column<int>(type: "integer", nullable: false),
                    actual_accuracy_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    expected_accuracy_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    calibration_error = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prediction_calibration_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prediction_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prediction_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_markets_prediction_category_id",
                table: "markets",
                column: "prediction_category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_markets_prediction_categories_prediction_category_id",
                table: "markets",
                column: "prediction_category_id",
                principalTable: "prediction_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_markets_prediction_categories_prediction_category_id",
                table: "markets");

            migrationBuilder.DropTable(
                name: "prediction_calibration_snapshots");

            migrationBuilder.DropTable(
                name: "prediction_categories");

            migrationBuilder.DropIndex(
                name: "ix_markets_prediction_category_id",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "prediction_category_id",
                table: "markets");
        }
    }
}
