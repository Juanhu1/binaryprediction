using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Day5_MarketFiltering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "markets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "eligible_for_analysis",
                table: "markets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_quality_evaluation_utc",
                table: "markets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quality_score",
                table: "markets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "markets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "eligible_for_analysis",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "last_quality_evaluation_utc",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "quality_score",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "markets");
        }
    }
}
