using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrueDope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ammunition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Caliber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Grain = table.Column<decimal>(type: "numeric(6,1)", nullable: false),
                    BulletType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CostPerRound = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    BallisticCoefficient = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    DragModel = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ammunition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ammunition_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RifleSetups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Caliber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BarrelLength = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    TwistRate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ScopeMake = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ScopeModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ScopeHeight = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    ZeroDistance = table.Column<int>(type: "integer", nullable: false),
                    ZeroElevationClicks = table.Column<decimal>(type: "numeric", nullable: true),
                    ZeroWindageClicks = table.Column<decimal>(type: "numeric", nullable: true),
                    MuzzleVelocity = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    BallisticCoefficient = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    DragModel = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RifleSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RifleSetups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(10,8)", nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(11,8)", nullable: false),
                    Altitude = table.Column<decimal>(type: "numeric(7,1)", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedLocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmmoLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AmmunitionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitialQuantity = table.Column<int>(type: "integer", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmmoLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmmoLots_Ammunition_AmmunitionId",
                        column: x => x.AmmunitionId,
                        principalTable: "Ammunition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmmoLots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RangeSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SessionTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    RifleSetupId = table.Column<int>(type: "integer", nullable: false),
                    SavedLocationId = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    LocationName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Temperature = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Humidity = table.Column<int>(type: "integer", nullable: true),
                    WindSpeed = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    WindDirection = table.Column<int>(type: "integer", nullable: true),
                    Pressure = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DensityAltitude = table.Column<decimal>(type: "numeric(7,1)", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RangeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RangeSessions_RifleSetups_RifleSetupId",
                        column: x => x.RifleSetupId,
                        principalTable: "RifleSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RangeSessions_SavedLocations_SavedLocationId",
                        column: x => x.SavedLocationId,
                        principalTable: "SavedLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RangeSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChronoSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RangeSessionId = table.Column<int>(type: "integer", nullable: false),
                    AmmunitionId = table.Column<int>(type: "integer", nullable: false),
                    AmmoLotId = table.Column<int>(type: "integer", nullable: true),
                    BarrelTemperature = table.Column<decimal>(type: "numeric(5,1)", nullable: true),
                    NumberOfRounds = table.Column<int>(type: "integer", nullable: false),
                    AverageVelocity = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    HighVelocity = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    LowVelocity = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    StandardDeviation = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    ExtremeSpread = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronoSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronoSessions_AmmoLots_AmmoLotId",
                        column: x => x.AmmoLotId,
                        principalTable: "AmmoLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChronoSessions_Ammunition_AmmunitionId",
                        column: x => x.AmmunitionId,
                        principalTable: "Ammunition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChronoSessions_RangeSessions_RangeSessionId",
                        column: x => x.RangeSessionId,
                        principalTable: "RangeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DopeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RangeSessionId = table.Column<int>(type: "integer", nullable: false),
                    Distance = table.Column<int>(type: "integer", nullable: false),
                    ElevationMils = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    WindageMils = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DopeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DopeEntries_RangeSessions_RangeSessionId",
                        column: x => x.RangeSessionId,
                        principalTable: "RangeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RangeSessionId = table.Column<int>(type: "integer", nullable: false),
                    AmmunitionId = table.Column<int>(type: "integer", nullable: true),
                    AmmoLotId = table.Column<int>(type: "integer", nullable: true),
                    GroupNumber = table.Column<int>(type: "integer", nullable: false),
                    Distance = table.Column<int>(type: "integer", nullable: false),
                    NumberOfShots = table.Column<int>(type: "integer", nullable: false),
                    GroupSizeMoa = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    MeanRadiusMoa = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupEntries_AmmoLots_AmmoLotId",
                        column: x => x.AmmoLotId,
                        principalTable: "AmmoLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GroupEntries_Ammunition_AmmunitionId",
                        column: x => x.AmmunitionId,
                        principalTable: "Ammunition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GroupEntries_RangeSessions_RangeSessionId",
                        column: x => x.RangeSessionId,
                        principalTable: "RangeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VelocityReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChronoSessionId = table.Column<int>(type: "integer", nullable: false),
                    ShotNumber = table.Column<int>(type: "integer", nullable: false),
                    Velocity = table.Column<decimal>(type: "numeric(6,1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VelocityReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VelocityReadings_ChronoSessions_ChronoSessionId",
                        column: x => x.ChronoSessionId,
                        principalTable: "ChronoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ThumbnailFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    RifleSetupId = table.Column<int>(type: "integer", nullable: true),
                    RangeSessionId = table.Column<int>(type: "integer", nullable: true),
                    GroupEntryId = table.Column<int>(type: "integer", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_GroupEntries_GroupEntryId",
                        column: x => x.GroupEntryId,
                        principalTable: "GroupEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Images_RangeSessions_RangeSessionId",
                        column: x => x.RangeSessionId,
                        principalTable: "RangeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Images_RifleSetups_RifleSetupId",
                        column: x => x.RifleSetupId,
                        principalTable: "RifleSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Images_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmmoLots_AmmunitionId_LotNumber",
                table: "AmmoLots",
                columns: new[] { "AmmunitionId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmmoLots_UserId",
                table: "AmmoLots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ammunition_UserId",
                table: "Ammunition",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronoSessions_AmmoLotId",
                table: "ChronoSessions",
                column: "AmmoLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronoSessions_AmmunitionId",
                table: "ChronoSessions",
                column: "AmmunitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronoSessions_RangeSessionId",
                table: "ChronoSessions",
                column: "RangeSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DopeEntries_RangeSessionId",
                table: "DopeEntries",
                column: "RangeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DopeEntries_RangeSessionId_Distance",
                table: "DopeEntries",
                columns: new[] { "RangeSessionId", "Distance" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupEntries_AmmoLotId",
                table: "GroupEntries",
                column: "AmmoLotId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupEntries_AmmunitionId",
                table: "GroupEntries",
                column: "AmmunitionId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupEntries_RangeSessionId",
                table: "GroupEntries",
                column: "RangeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_GroupEntryId",
                table: "Images",
                column: "GroupEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_RangeSessionId",
                table: "Images",
                column: "RangeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_RifleSetupId",
                table: "Images",
                column: "RifleSetupId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_UserId",
                table: "Images",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RangeSessions_RifleSetupId",
                table: "RangeSessions",
                column: "RifleSetupId");

            migrationBuilder.CreateIndex(
                name: "IX_RangeSessions_SavedLocationId",
                table: "RangeSessions",
                column: "SavedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RangeSessions_SessionDate",
                table: "RangeSessions",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_RangeSessions_UserId",
                table: "RangeSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RifleSetups_UserId",
                table: "RifleSetups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLocations_UserId",
                table: "SavedLocations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VelocityReadings_ChronoSessionId",
                table: "VelocityReadings",
                column: "ChronoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VelocityReadings_ChronoSessionId_ShotNumber",
                table: "VelocityReadings",
                columns: new[] { "ChronoSessionId", "ShotNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DopeEntries");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "VelocityReadings");

            migrationBuilder.DropTable(
                name: "GroupEntries");

            migrationBuilder.DropTable(
                name: "ChronoSessions");

            migrationBuilder.DropTable(
                name: "AmmoLots");

            migrationBuilder.DropTable(
                name: "RangeSessions");

            migrationBuilder.DropTable(
                name: "Ammunition");

            migrationBuilder.DropTable(
                name: "RifleSetups");

            migrationBuilder.DropTable(
                name: "SavedLocations");
        }
    }
}
