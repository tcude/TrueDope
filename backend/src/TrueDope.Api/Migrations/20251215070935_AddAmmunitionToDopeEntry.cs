using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrueDope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAmmunitionToDopeEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmmoLotId",
                table: "DopeEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AmmunitionId",
                table: "DopeEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DopeEntries_AmmoLotId",
                table: "DopeEntries",
                column: "AmmoLotId");

            migrationBuilder.CreateIndex(
                name: "IX_DopeEntries_AmmunitionId",
                table: "DopeEntries",
                column: "AmmunitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DopeEntries_AmmoLots_AmmoLotId",
                table: "DopeEntries",
                column: "AmmoLotId",
                principalTable: "AmmoLots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DopeEntries_Ammunition_AmmunitionId",
                table: "DopeEntries",
                column: "AmmunitionId",
                principalTable: "Ammunition",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DopeEntries_AmmoLots_AmmoLotId",
                table: "DopeEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_DopeEntries_Ammunition_AmmunitionId",
                table: "DopeEntries");

            migrationBuilder.DropIndex(
                name: "IX_DopeEntries_AmmoLotId",
                table: "DopeEntries");

            migrationBuilder.DropIndex(
                name: "IX_DopeEntries_AmmunitionId",
                table: "DopeEntries");

            migrationBuilder.DropColumn(
                name: "AmmoLotId",
                table: "DopeEntries");

            migrationBuilder.DropColumn(
                name: "AmmunitionId",
                table: "DopeEntries");
        }
    }
}
