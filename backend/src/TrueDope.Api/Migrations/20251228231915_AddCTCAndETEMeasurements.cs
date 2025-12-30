using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueDope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCTCAndETEMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VerticalSpread",
                table: "GroupMeasurements",
                newName: "VerticalSpreadEte");

            migrationBuilder.RenameColumn(
                name: "HorizontalSpread",
                table: "GroupMeasurements",
                newName: "VerticalSpreadCtc");

            migrationBuilder.RenameColumn(
                name: "ExtremeSpread",
                table: "GroupMeasurements",
                newName: "HorizontalSpreadEte");

            migrationBuilder.AddColumn<decimal>(
                name: "ExtremeSpreadCtc",
                table: "GroupMeasurements",
                type: "numeric(6,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtremeSpreadEte",
                table: "GroupMeasurements",
                type: "numeric(6,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HorizontalSpreadCtc",
                table: "GroupMeasurements",
                type: "numeric(6,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtremeSpreadCtc",
                table: "GroupMeasurements");

            migrationBuilder.DropColumn(
                name: "ExtremeSpreadEte",
                table: "GroupMeasurements");

            migrationBuilder.DropColumn(
                name: "HorizontalSpreadCtc",
                table: "GroupMeasurements");

            migrationBuilder.RenameColumn(
                name: "VerticalSpreadEte",
                table: "GroupMeasurements",
                newName: "VerticalSpread");

            migrationBuilder.RenameColumn(
                name: "VerticalSpreadCtc",
                table: "GroupMeasurements",
                newName: "HorizontalSpread");

            migrationBuilder.RenameColumn(
                name: "HorizontalSpreadEte",
                table: "GroupMeasurements",
                newName: "ExtremeSpread");
        }
    }
}
