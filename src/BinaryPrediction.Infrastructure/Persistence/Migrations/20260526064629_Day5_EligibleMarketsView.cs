using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Day5_EligibleMarketsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIEW eligible_markets_view AS
                SELECT 
                    id, question, category, quality_score, probability, 
                    liquidity, volume, end_date, estimated_resolution_date_utc, 
                    created_at_utc
                FROM markets
                WHERE eligible_for_analysis = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW eligible_markets_view;");
        }
    }
}
