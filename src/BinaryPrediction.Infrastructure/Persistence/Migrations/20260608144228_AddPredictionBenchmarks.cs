using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionBenchmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prediction_benchmark_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    benchmark_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    total_predictions = table.Column<int>(type: "integer", nullable: false),
                    correct_predictions = table.Column<int>(type: "integer", nullable: false),
                    accuracy_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    average_brier_score = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    calculated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prediction_benchmark_results", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_prediction_benchmark_results_benchmark_type",
                table: "prediction_benchmark_results",
                column: "benchmark_type");

            migrationBuilder.CreateIndex(
                name: "ix_prediction_benchmark_results_calculated_at_utc",
                table: "prediction_benchmark_results",
                column: "calculated_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prediction_benchmark_results");
        }
    }
}
