using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakePredictionCategoryIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing foreign key
            migrationBuilder.DropForeignKey(
                name: "fk_markets_prediction_categories_prediction_category_id",
                table: "markets");

            // Alter column to be nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "prediction_category_id",
                table: "markets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);

            // Re‑create foreign key with SetNull on delete
            migrationBuilder.AddForeignKey(
                name: "fk_markets_prediction_categories_prediction_category_id",
                table: "markets",
                column: "prediction_category_id",
                principalTable: "prediction_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
