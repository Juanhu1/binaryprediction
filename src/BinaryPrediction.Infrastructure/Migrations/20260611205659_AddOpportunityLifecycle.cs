using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "prediction_opportunities",
                newName: "last_status_changed_at_utc");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expired_at_utc",
                table: "prediction_opportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ignored_at_utc",
                table: "prediction_opportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "resolved_at_utc",
                table: "prediction_opportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "prediction_opportunities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "opportunity_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: false),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opportunity_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_opportunity_status_history_prediction_opportunities_opportu",
                        column: x => x.opportunity_id,
                        principalTable: "prediction_opportunities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_opportunity_status_history_opportunity_id",
                table: "opportunity_status_history",
                column: "opportunity_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opportunity_status_history");

            migrationBuilder.DropColumn(
                name: "expired_at_utc",
                table: "prediction_opportunities");

            migrationBuilder.DropColumn(
                name: "ignored_at_utc",
                table: "prediction_opportunities");

            migrationBuilder.DropColumn(
                name: "resolved_at_utc",
                table: "prediction_opportunities");

            migrationBuilder.DropColumn(
                name: "status",
                table: "prediction_opportunities");

            migrationBuilder.RenameColumn(
                name: "last_status_changed_at_utc",
                table: "prediction_opportunities",
                newName: "updated_at_utc");
        }
    }
}
