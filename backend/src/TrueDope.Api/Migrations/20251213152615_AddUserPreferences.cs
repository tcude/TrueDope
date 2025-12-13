using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueDope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DistanceUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdjustmentUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TemperatureUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PressureUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VelocityUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
