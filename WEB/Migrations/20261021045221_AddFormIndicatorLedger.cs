using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFormIndicatorLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormIndicatorMappings",
                columns: table => new
                {
                    FormIndicatorMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Aggregation = table.Column<int>(type: "int", nullable: false),
                    ValueExpressionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateBindingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityBindingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisaggregationGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountOnlySubmitted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormIndicatorMappings", x => x.FormIndicatorMappingId);
                    table.ForeignKey(
                        name: "FK_FormIndicatorMappings_Indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "Indicators",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormIndicatorMappings_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "QuestionnaireId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndicatorContributions",
                columns: table => new
                {
                    IndicatorContributionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormIndicatorMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisaggregationOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Value = table.Column<decimal>(type: "decimal(20,8)", nullable: false),
                    Numerator = table.Column<decimal>(type: "decimal(20,8)", nullable: true),
                    Denominator = table.Column<decimal>(type: "decimal(20,8)", nullable: true),
                    ComputedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorContributions", x => x.IndicatorContributionId);
                    table.ForeignKey(
                        name: "FK_IndicatorContributions_FormIndicatorMappings_FormIndicatorMappingId",
                        column: x => x.FormIndicatorMappingId,
                        principalTable: "FormIndicatorMappings",
                        principalColumn: "FormIndicatorMappingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndicatorContributions_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Responses",
                        principalColumn: "ResponseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionIndicatorLinks",
                columns: table => new
                {
                    QuestionIndicatorLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormIndicatorMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    DisaggregationOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValueMapJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionIndicatorLinks", x => x.QuestionIndicatorLinkId);
                    table.ForeignKey(
                        name: "FK_QuestionIndicatorLinks_FormIndicatorMappings_FormIndicatorMappingId",
                        column: x => x.FormIndicatorMappingId,
                        principalTable: "FormIndicatorMappings",
                        principalColumn: "FormIndicatorMappingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionIndicatorLinks_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormIndicatorMappings_IndicatorId",
                table: "FormIndicatorMappings",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "UX_FormIndicatorMappings_Form_Indicator",
                table: "FormIndicatorMappings",
                columns: new[] { "QuestionnaireId", "IndicatorId" },
                unique: true,
                filter: "[SectionId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_FormIndicatorMappings_Form_Section_Indicator",
                table: "FormIndicatorMappings",
                columns: new[] { "QuestionnaireId", "IndicatorId", "SectionId" },
                unique: true,
                filter: "[SectionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_Aggregate",
                table: "IndicatorContributions",
                columns: new[] { "IndicatorId", "EntityId", "DateId" });

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorContributions_ResponseId",
                table: "IndicatorContributions",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "UX_Contributions_Response_Option",
                table: "IndicatorContributions",
                columns: new[] { "FormIndicatorMappingId", "ResponseId", "DisaggregationOptionId" },
                unique: true,
                filter: "[DisaggregationOptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Contributions_Response_Total",
                table: "IndicatorContributions",
                columns: new[] { "FormIndicatorMappingId", "ResponseId" },
                unique: true,
                filter: "[DisaggregationOptionId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionIndicatorLinks_Mapping",
                table: "QuestionIndicatorLinks",
                columns: new[] { "FormIndicatorMappingId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionIndicatorLinks_QuestionId",
                table: "QuestionIndicatorLinks",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndicatorContributions");

            migrationBuilder.DropTable(
                name: "QuestionIndicatorLinks");

            migrationBuilder.DropTable(
                name: "FormIndicatorMappings");
        }
    }
}
