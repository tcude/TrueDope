using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrueDope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdminUserId = table.Column<string>(type: "text", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetUserId = table.Column<string>(type: "text", nullable: true),
                    TargetEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TargetEntityId = table.Column<int>(type: "integer", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_ActionType",
                table: "AdminAuditLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUserId",
                table: "AdminAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_TargetUserId",
                table: "AdminAuditLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_Timestamp",
                table: "AdminAuditLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");
        }
    }
}
