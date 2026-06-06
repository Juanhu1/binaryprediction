using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateActiveQueueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_market_analysis_queue_market_id_pending_unique",
                table: "market_analysis_queue");

            migrationBuilder.CreateIndex(
                name: "ux_market_analysis_queue_active",
                table: "market_analysis_queue",
                column: "market_id",
                unique: true,
                filter: "status IN ('Pending', 'Processing')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_market_analysis_queue_active",
                table: "market_analysis_queue");

            migrationBuilder.CreateIndex(
                name: "ix_market_analysis_queue_market_id_pending_unique",
                table: "market_analysis_queue",
                column: "market_id",
                unique: true,
                filter: "status = 'Pending'");
        }
    }
}
