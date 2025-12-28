using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrueDope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMeasurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupEntryId = table.Column<int>(type: "integer", nullable: false),
                    HolePositionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    BulletDiameter = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    ExtremeSpread = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    MeanRadius = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    HorizontalSpread = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    VerticalSpread = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    RadialStdDev = table.Column<decimal>(type: "numeric(6,5)", nullable: true),
                    HorizontalStdDev = table.Column<decimal>(type: "numeric(6,5)", nullable: true),
                    VerticalStdDev = table.Column<decimal>(type: "numeric(6,5)", nullable: true),
                    Cep50 = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    PoiOffsetX = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    PoiOffsetY = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    CalibrationMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MeasurementConfidence = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    OriginalImageId = table.Column<int>(type: "integer", nullable: true),
                    AnnotatedImageId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMeasurements_GroupEntries_GroupEntryId",
                        column: x => x.GroupEntryId,
                        principalTable: "GroupEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMeasurements_Images_AnnotatedImageId",
                        column: x => x.AnnotatedImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GroupMeasurements_Images_OriginalImageId",
                        column: x => x.OriginalImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMeasurements_AnnotatedImageId",
                table: "GroupMeasurements",
                column: "AnnotatedImageId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMeasurements_GroupEntryId",
                table: "GroupMeasurements",
                column: "GroupEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMeasurements_OriginalImageId",
                table: "GroupMeasurements",
                column: "OriginalImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupMeasurements");
        }
    }
}
