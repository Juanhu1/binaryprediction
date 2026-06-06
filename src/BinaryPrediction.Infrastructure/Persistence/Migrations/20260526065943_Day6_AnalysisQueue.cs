using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Day6_AnalysisQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "market_analysis_queue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_market_analysis_queue", x => x.id);
                    table.ForeignKey(
                        name: "fk_market_analysis_queue_markets_market_id",
                        column: x => x.market_id,
                        principalTable: "markets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_market_analysis_queue_created_at_utc",
                table: "market_analysis_queue",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_market_analysis_queue_market_id_pending_unique",
                table: "market_analysis_queue",
                column: "market_id",
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_market_analysis_queue_priority",
                table: "market_analysis_queue",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_market_analysis_queue_status",
                table: "market_analysis_queue",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "market_analysis_queue");
        }
    }
}
