using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLogFrameV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogFrameLevels",
                columns: table => new
                {
                    LogFrameLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogFrameId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PluralName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    IsIndicatorLevel = table.Column<bool>(type: "bit", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    RequiredFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogFrameLevels", x => x.LogFrameLevelId);
                    table.ForeignKey(
                        name: "FK_LogFrameLevels_LogFrames_LogFrameId",
                        column: x => x.LogFrameId,
                        principalTable: "LogFrames",
                        principalColumn: "LogFrameId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogFrameNodes",
                columns: table => new
                {
                    LogFrameNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogFrameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LogFrameLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MeansOfVerification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RisksAndAssumptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CustomFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacyLogFrameRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InheritedFromNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogFrameNodes", x => x.LogFrameNodeId);
                    table.ForeignKey(
                        name: "FK_LogFrameNodes_LogFrameLevels_LogFrameLevelId",
                        column: x => x.LogFrameLevelId,
                        principalTable: "LogFrameLevels",
                        principalColumn: "LogFrameLevelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogFrameNodes_LogFrameNodes_ParentNodeId",
                        column: x => x.ParentNodeId,
                        principalTable: "LogFrameNodes",
                        principalColumn: "LogFrameNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogFrameNodes_LogFrames_LogFrameId",
                        column: x => x.LogFrameId,
                        principalTable: "LogFrames",
                        principalColumn: "LogFrameId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogFrameNodeIndicators",
                columns: table => new
                {
                    LogFrameNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogFrameNodeIndicators", x => new { x.LogFrameNodeId, x.IndicatorId });
                    table.ForeignKey(
                        name: "FK_LogFrameNodeIndicators_Indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "Indicators",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogFrameNodeIndicators_LogFrameNodes_LogFrameNodeId",
                        column: x => x.LogFrameNodeId,
                        principalTable: "LogFrameNodes",
                        principalColumn: "LogFrameNodeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_LogFrameLevels_LogFrame_Rank",
                table: "LogFrameLevels",
                columns: new[] { "LogFrameId", "Rank" },
                unique: true,
                filter: "[LogFrameId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_LogFrameLevels_Org_Rank",
                table: "LogFrameLevels",
                columns: new[] { "OrganisationId", "Rank" },
                unique: true,
                filter: "[OrganisationId] IS NOT NULL AND [LogFrameId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogFrameNodeIndicators_Indicator",
                table: "LogFrameNodeIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_LogFrameNodes_LegacyRow",
                table: "LogFrameNodes",
                column: "LegacyLogFrameRowId");

            migrationBuilder.CreateIndex(
                name: "IX_LogFrameNodes_LogFrameLevelId",
                table: "LogFrameNodes",
                column: "LogFrameLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_LogFrameNodes_ParentNodeId",
                table: "LogFrameNodes",
                column: "ParentNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LogFrameNodes_Tree",
                table: "LogFrameNodes",
                columns: new[] { "LogFrameId", "ParentNodeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_LogFrameNodes_Code",
                table: "LogFrameNodes",
                columns: new[] { "LogFrameId", "Code" },
                unique: true,
                filter: "[Code] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogFrameNodeIndicators");

            migrationBuilder.DropTable(
                name: "LogFrameNodes");

            migrationBuilder.DropTable(
                name: "LogFrameLevels");
        }
    }
}
