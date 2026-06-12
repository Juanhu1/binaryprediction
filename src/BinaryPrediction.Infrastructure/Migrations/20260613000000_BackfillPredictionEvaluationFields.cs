using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    public partial class BackfillPredictionEvaluationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill ActualOutcome, WasCorrect, and EvaluatedAtUtc for predictions that have been evaluated via resolution history
            migrationBuilder.Sql(@"
                UPDATE p
                SET ActualOutcome = rh.ActualOutcome,
                    WasCorrect = CASE WHEN p.PredictedOutcome = rh.ActualOutcome THEN TRUE ELSE FALSE END,
                    EvaluatedAtUtc = rh.ResolvedAtUtc,
                    PredictionError = CASE
                        WHEN p.ConfidencePercentage >= 50 THEN ABS(p.ConfidencePercentage/100.0 - CASE WHEN rh.ActualOutcome = 'Yes' THEN 1 ELSE 0 END)
                        ELSE ABS((1 - p.ConfidencePercentage/100.0) - CASE WHEN rh.ActualOutcome = 'Yes' THEN 1 ELSE 0 END)
                    END
                FROM prediction p
                INNER JOIN prediction_resolution_histories rh ON p.Id = rh.PredictionId
                WHERE p.EvaluatedAtUtc IS NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No automatic rollback; manually clear fields if needed
            migrationBuilder.Sql(@"
                UPDATE p
                SET ActualOutcome = NULL,
                    WasCorrect = NULL,
                    EvaluatedAtUtc = NULL
                FROM prediction p
                WHERE p.EvaluatedAtUtc IS NOT NULL;
            ");
        }
    }
}
