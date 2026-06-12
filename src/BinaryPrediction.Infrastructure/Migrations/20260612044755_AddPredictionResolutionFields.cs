using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionResolutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "actual_outcome_probability",
                table: "predictions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution_source",
                table: "predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "resolved_at_utc",
                table: "predictions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "actual_outcome_probability",
                table: "predictions");

            migrationBuilder.DropColumn(
                name: "resolution_source",
                table: "predictions");

            migrationBuilder.DropColumn(
                name: "resolved_at_utc",
                table: "predictions");
        }
    }
}
