using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolicyPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginHistoryEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "login_history_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    device_label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    device_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    os_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    os_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_history_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_login_history_entries_user_id_occurred_at",
                table: "login_history_entries",
                columns: new[] { "user_id", "occurred_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "login_history_entries");
        }
    }
}
