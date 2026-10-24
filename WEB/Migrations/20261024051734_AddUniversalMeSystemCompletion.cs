using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversalMeSystemCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                table: "Questions",
                type: "decimal(20,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinValue",
                table: "Questions",
                type: "decimal(20,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationRegex",
                table: "Questions",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Projects",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentPhase",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonorName",
                table: "Projects",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrantCode",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level1Label",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level2Label",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level3Label",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level4Label",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TargetDirectBeneficiaries",
                table: "Projects",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TargetIndirectBeneficiaries",
                table: "Projects",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBudget",
                table: "Projects",
                type: "decimal(20,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowMode",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DisaggregationOptionId",
                table: "IndicatorTargets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalculationFormulaNarrative",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataQualityProtocol",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DenominatorIndicatorId",
                table: "Indicators",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NumeratorIndicatorId",
                table: "Indicators",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalDefinition",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurposeRationale",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportingSchedule",
                table: "Indicators",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BeneficiaryFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateReceivedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    ContactDetails = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeneficiaryFeedbacks_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BeneficiaryFeedbacks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogFrameRowBudgetLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogFrameRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogFrameRowBudgetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogFrameRowBudgetLines_BudgetLines_BudgetLineId",
                        column: x => x.BudgetLineId,
                        principalTable: "BudgetLines",
                        principalColumn: "BudgetLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogFrameRowBudgetLines_LogFrameRows_LogFrameRowId",
                        column: x => x.LogFrameRowId,
                        principalTable: "LogFrameRows",
                        principalColumn: "LogFrameRowId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogFrameRowRiskAssumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogFrameRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiskAssumptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogFrameRowRiskAssumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogFrameRowRiskAssumptions_LogFrameRows_LogFrameRowId",
                        column: x => x.LogFrameRowId,
                        principalTable: "LogFrameRows",
                        principalColumn: "LogFrameRowId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogFrameRowRiskAssumptions_RiskAssumptions_RiskAssumptionId",
                        column: x => x.RiskAssumptionId,
                        principalTable: "RiskAssumptions",
                        principalColumn: "RiskAssumptionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogFrameRowTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogFrameRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogFrameRowTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogFrameRowTasks_LogFrameRows_LogFrameRowId",
                        column: x => x.LogFrameRowId,
                        principalTable: "LogFrameRows",
                        principalColumn: "LogFrameRowId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogFrameRowTasks_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorTargets_DisaggregationOptionId",
                table: "IndicatorTargets",
                column: "DisaggregationOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorTargets_Indicator_Date_Entity_Option",
                table: "IndicatorTargets",
                columns: new[] { "IndicatorId", "DateId", "EntityId", "DisaggregationOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_DenominatorIndicatorId",
                table: "Indicators",
                column: "DenominatorIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_NumeratorIndicatorId",
                table: "Indicators",
                column: "NumeratorIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryFeedbacks_AssignedToUserId",
                table: "BeneficiaryFeedbacks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryFeedbacks_Project_Status",
                table: "BeneficiaryFeedbacks",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_BeneficiaryFeedbacks_TrackingCode",
                table: "BeneficiaryFeedbacks",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogFrameRowBudgetLines_BudgetLineId",
                table: "LogFrameRowBudgetLines",
                column: "BudgetLineId");

            migrationBuilder.CreateIndex(
                name: "UX_LogFrameRowBudgetLines_Row_BudgetLine",
                table: "LogFrameRowBudgetLines",
                columns: new[] { "LogFrameRowId", "BudgetLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogFrameRowRiskAssumptions_RiskAssumptionId",
                table: "LogFrameRowRiskAssumptions",
                column: "RiskAssumptionId");

            migrationBuilder.CreateIndex(
                name: "UX_LogFrameRowRiskAssumptions_Row_Risk",
                table: "LogFrameRowRiskAssumptions",
                columns: new[] { "LogFrameRowId", "RiskAssumptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogFrameRowTasks_TaskId",
                table: "LogFrameRowTasks",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "UX_LogFrameRowTasks_Row_Task",
                table: "LogFrameRowTasks",
                columns: new[] { "LogFrameRowId", "TaskId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Indicators_Indicators_DenominatorIndicatorId",
                table: "Indicators",
                column: "DenominatorIndicatorId",
                principalTable: "Indicators",
                principalColumn: "IndicatorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Indicators_Indicators_NumeratorIndicatorId",
                table: "Indicators",
                column: "NumeratorIndicatorId",
                principalTable: "Indicators",
                principalColumn: "IndicatorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IndicatorTargets_DisaggregationOptions_DisaggregationOptionId",
                table: "IndicatorTargets",
                column: "DisaggregationOptionId",
                principalTable: "DisaggregationOptions",
                principalColumn: "DisaggregationOptionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Indicators_Indicators_DenominatorIndicatorId",
                table: "Indicators");

            migrationBuilder.DropForeignKey(
                name: "FK_Indicators_Indicators_NumeratorIndicatorId",
                table: "Indicators");

            migrationBuilder.DropForeignKey(
                name: "FK_IndicatorTargets_DisaggregationOptions_DisaggregationOptionId",
                table: "IndicatorTargets");

            migrationBuilder.DropTable(
                name: "BeneficiaryFeedbacks");

            migrationBuilder.DropTable(
                name: "LogFrameRowBudgetLines");

            migrationBuilder.DropTable(
                name: "LogFrameRowRiskAssumptions");

            migrationBuilder.DropTable(
                name: "LogFrameRowTasks");

            migrationBuilder.DropIndex(
                name: "IX_IndicatorTargets_DisaggregationOptionId",
                table: "IndicatorTargets");

            migrationBuilder.DropIndex(
                name: "IX_IndicatorTargets_Indicator_Date_Entity_Option",
                table: "IndicatorTargets");

            migrationBuilder.DropIndex(
                name: "IX_Indicators_DenominatorIndicatorId",
                table: "Indicators");

            migrationBuilder.DropIndex(
                name: "IX_Indicators_NumeratorIndicatorId",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ValidationRegex",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CurrentPhase",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DonorName",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "GrantCode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Level1Label",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Level2Label",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Level3Label",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Level4Label",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TargetDirectBeneficiaries",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TargetIndirectBeneficiaries",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TotalBudget",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "WorkflowMode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DisaggregationOptionId",
                table: "IndicatorTargets");

            migrationBuilder.DropColumn(
                name: "CalculationFormulaNarrative",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "DataQualityProtocol",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "DenominatorIndicatorId",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "NumeratorIndicatorId",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "OperationalDefinition",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "PurposeRationale",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "ReportingSchedule",
                table: "Indicators");
        }
    }
}
