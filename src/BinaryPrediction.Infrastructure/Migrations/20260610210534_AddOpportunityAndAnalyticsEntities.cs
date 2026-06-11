using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    public partial class AddOpportunityAndAnalyticsEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PredictionOpportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    PredictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketProbability = table.Column<decimal>(type: "numeric", nullable: false),
                    AiProbability = table.Column<decimal>(type: "numeric", nullable: false),
                    ProbabilityGap = table.Column<decimal>(type: "numeric", nullable: false),
                    EdgeThresholdPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    HasEdge = table.Column<bool>(type: "boolean", nullable: false),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionOpportunities", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "gap_direction",
                table: "PredictionOpportunities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OpportunityAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    OpportunityCount = table.Column<int>(type: "integer", nullable: false),
                    AverageGapPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    LargestGapPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityAnalyticsSnapshots", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PredictionOpportunities");
            migrationBuilder.DropTable(name: "OpportunityAnalyticsSnapshots");
        }
    }
}
