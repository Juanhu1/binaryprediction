using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BinaryPrediction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opportunity_lifecycle_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    open_count = table.Column<int>(type: "integer", nullable: false),
                    active_count = table.Column<int>(type: "integer", nullable: false),
                    ignored_count = table.Column<int>(type: "integer", nullable: false),
                    expired_count = table.Column<int>(type: "integer", nullable: false),
                    resolved_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opportunity_lifecycle_snapshots", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opportunity_lifecycle_snapshots");
        }
    }
}
