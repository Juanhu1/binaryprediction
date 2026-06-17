using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKalshiSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_markets_slug",
                table: "markets");

            migrationBuilder.AddColumn<string>(
                name: "external_event_id",
                table: "markets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_market_id",
                table: "markets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "market_source",
                table: "markets",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "source_url",
                table: "markets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_markets_market_source_external_market_id",
                table: "markets",
                columns: new[] { "market_source", "external_market_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_markets_market_source_slug",
                table: "markets",
                columns: new[] { "market_source", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_markets_market_source_external_market_id",
                table: "markets");

            migrationBuilder.DropIndex(
                name: "ix_markets_market_source_slug",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "external_event_id",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "external_market_id",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "market_source",
                table: "markets");

            migrationBuilder.DropColumn(
                name: "source_url",
                table: "markets");

            migrationBuilder.CreateIndex(
                name: "ix_markets_slug",
                table: "markets",
                column: "slug",
                unique: true);
        }
    }
}
