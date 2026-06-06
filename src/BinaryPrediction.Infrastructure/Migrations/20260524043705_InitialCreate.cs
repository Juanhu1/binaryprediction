using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "markets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    closed = table.Column<bool>(type: "boolean", nullable: false),
                    liquidity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    volume = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    probability = table.Column<decimal>(type: "numeric", nullable: false),
                    end_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_markets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_probability = table.Column<decimal>(type: "numeric", nullable: false),
                    estimated_probability = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    edge = table.Column<decimal>(type: "numeric", nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    key_reasons_json = table.Column<string>(type: "text", nullable: false),
                    risk_factors_json = table.Column<string>(type: "text", nullable: false),
                    model_name = table.Column<string>(type: "text", nullable: false),
                    prompt_version = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_analyses", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_analyses_markets_market_id",
                        column: x => x.market_id,
                        principalTable: "markets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_alerts_markets_market_id",
                        column: x => x.market_id,
                        principalTable: "markets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "market_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    market_id = table.Column<Guid>(type: "uuid", nullable: false),
                    probability = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    liquidity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_market_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_market_snapshots_markets_market_id",
                        column: x => x.market_id,
                        principalTable: "markets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_analyses_market_id_created_at_utc",
                table: "ai_analyses",
                columns: new[] { "market_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_market_id_created_at_utc",
                table: "alerts",
                columns: new[] { "market_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_market_snapshots_market_id_created_at_utc",
                table: "market_snapshots",
                columns: new[] { "market_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_markets_active_closed",
                table: "markets",
                columns: new[] { "active", "closed" });

            migrationBuilder.CreateIndex(
                name: "ix_markets_slug",
                table: "markets",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_analyses");

            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "market_snapshots");

            migrationBuilder.DropTable(
                name: "markets");
        }
    }
}
