using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class IndicatorDisaggregationsAndTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "Indicators",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DisaggregationGroupId",
                table: "Indicators",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisaggregationGroups",
                columns: table => new
                {
                    DisaggregationGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisaggregationGroups", x => x.DisaggregationGroupId);
                });

            migrationBuilder.CreateTable(
                name: "IndicatorTargets",
                columns: table => new
                {
                    IndicatorTargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(20,8)", nullable: false),
                    BaselineValue = table.Column<decimal>(type: "decimal(20,8)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorTargets", x => x.IndicatorTargetId);
                    table.ForeignKey(
                        name: "FK_IndicatorTargets_Dates_DateId",
                        column: x => x.DateId,
                        principalTable: "Dates",
                        principalColumn: "DateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndicatorTargets_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "EntityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndicatorTargets_Indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "Indicators",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisaggregationOptions",
                columns: table => new
                {
                    DisaggregationOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisaggregationGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisaggregationOptions", x => x.DisaggregationOptionId);
                    table.ForeignKey(
                        name: "FK_DisaggregationOptions_DisaggregationGroups_DisaggregationGroupId",
                        column: x => x.DisaggregationGroupId,
                        principalTable: "DisaggregationGroups",
                        principalColumn: "DisaggregationGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisaggregationData",
                columns: table => new
                {
                    DisaggregationDatumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatumId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisaggregationOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(20,8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisaggregationData", x => x.DisaggregationDatumId);
                    table.ForeignKey(
                        name: "FK_DisaggregationData_Data_IndicatorId_EntityId_DateId",
                        columns: x => new { x.IndicatorId, x.EntityId, x.DateId },
                        principalTable: "Data",
                        principalColumns: new[] { "IndicatorId", "EntityId", "DateId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisaggregationData_DisaggregationOptions_DisaggregationOptionId",
                        column: x => x.DisaggregationOptionId,
                        principalTable: "DisaggregationOptions",
                        principalColumn: "DisaggregationOptionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_DisaggregationGroupId",
                table: "Indicators",
                column: "DisaggregationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DisaggregationData_Datum_OptionId",
                table: "DisaggregationData",
                columns: new[] { "IndicatorId", "EntityId", "DateId", "DisaggregationOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisaggregationData_DisaggregationOptionId",
                table: "DisaggregationData",
                column: "DisaggregationOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DisaggregationGroups_Name",
                table: "DisaggregationGroups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DisaggregationOptions_GroupId_Code",
                table: "DisaggregationOptions",
                columns: new[] { "DisaggregationGroupId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorTargets_DateId",
                table: "IndicatorTargets",
                column: "DateId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorTargets_EntityId",
                table: "IndicatorTargets",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorTargets_Indicator_Date_Entity",
                table: "IndicatorTargets",
                columns: new[] { "IndicatorId", "DateId", "EntityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Indicators_DisaggregationGroups_DisaggregationGroupId",
                table: "Indicators",
                column: "DisaggregationGroupId",
                principalTable: "DisaggregationGroups",
                principalColumn: "DisaggregationGroupId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Indicators_DisaggregationGroups_DisaggregationGroupId",
                table: "Indicators");

            migrationBuilder.DropTable(
                name: "DisaggregationData");

            migrationBuilder.DropTable(
                name: "IndicatorTargets");

            migrationBuilder.DropTable(
                name: "DisaggregationOptions");

            migrationBuilder.DropTable(
                name: "DisaggregationGroups");

            migrationBuilder.DropIndex(
                name: "IX_Indicators_DisaggregationGroupId",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "DisaggregationGroupId",
                table: "Indicators");
        }
    }
}
