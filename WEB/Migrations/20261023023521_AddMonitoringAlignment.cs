using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoringAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Data_EntityId",
                table: "Data");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "TheoriesOfChange",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "LogFrames",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TheoryOfChangeId",
                table: "LogFrames",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RollupMethod",
                table: "LogFrameLevels",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "AmberThreshold",
                table: "Indicators",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenThreshold",
                table: "Indicators",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonitoringType",
                table: "Indicators",
                type: "int",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleUserId",
                table: "Indicators",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "Data",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "Data",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "IndicatorId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LogFrameNodeId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LessonsLearned",
                columns: table => new
                {
                    LessonLearnedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LogFrameNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MonitoringType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonsLearned", x => x.LessonLearnedId);
                    table.ForeignKey(
                        name: "FK_LessonsLearned_AspNetUsers_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonsLearned_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "ComponentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonsLearned_LogFrameNodes_LogFrameNodeId",
                        column: x => x.LogFrameNodeId,
                        principalTable: "LogFrameNodes",
                        principalColumn: "LogFrameNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LessonsLearned_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonitoringChecks",
                columns: table => new
                {
                    MonitoringCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RequirementSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "Date", nullable: true),
                    PeriodDateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EvidenceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringChecks", x => x.MonitoringCheckId);
                    table.ForeignKey(
                        name: "FK_MonitoringChecks_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringChecks_Dates_PeriodDateId",
                        column: x => x.PeriodDateId,
                        principalTable: "Dates",
                        principalColumn: "DateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringChecks_Documents_EvidenceDocumentId",
                        column: x => x.EvidenceDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringChecks_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "EntityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringChecks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonitoringEvents",
                columns: table => new
                {
                    MonitoringEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EventDate = table.Column<DateTime>(type: "Date", nullable: false),
                    PeriodDateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConductedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Participants = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MeReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringEvents", x => x.MonitoringEventId);
                    table.ForeignKey(
                        name: "FK_MonitoringEvents_AspNetUsers_ConductedById",
                        column: x => x.ConductedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringEvents_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringEvents_Dates_PeriodDateId",
                        column: x => x.PeriodDateId,
                        principalTable: "Dates",
                        principalColumn: "DateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringEvents_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringEvents_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "EntityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringEvents_MeReviews_MeReviewId",
                        column: x => x.MeReviewId,
                        principalTable: "MeReviews",
                        principalColumn: "MeReviewId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringEvents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonitoringPlans",
                columns: table => new
                {
                    MonitoringPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromDateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToDateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringPlans", x => x.MonitoringPlanId);
                    table.ForeignKey(
                        name: "FK_MonitoringPlans_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringPlans_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringPlans_Dates_FromDateId",
                        column: x => x.FromDateId,
                        principalTable: "Dates",
                        principalColumn: "DateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringPlans_Dates_ToDateId",
                        column: x => x.ToDateId,
                        principalTable: "Dates",
                        principalColumn: "DateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringPlans_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectIndicators",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectIndicators", x => new { x.ProjectId, x.IndicatorId });
                    table.ForeignKey(
                        name: "FK_ProjectIndicators_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectIndicators_Indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "Indicators",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectIndicators_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssumptions",
                columns: table => new
                {
                    RiskAssumptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogFrameNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Likelihood = table.Column<int>(type: "int", nullable: false),
                    Impact = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Mitigation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "Date", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssumptions", x => x.RiskAssumptionId);
                    table.ForeignKey(
                        name: "FK_RiskAssumptions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssumptions_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssumptions_LogFrameNodes_LogFrameNodeId",
                        column: x => x.LogFrameNodeId,
                        principalTable: "LogFrameNodes",
                        principalColumn: "LogFrameNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssumptions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonitoringPlanItems",
                columns: table => new
                {
                    MonitoringPlanItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoringPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoringType = table.Column<int>(type: "int", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CollectionMethod = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    ResponsibleUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibleEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MeansOfVerification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringPlanItems", x => x.MonitoringPlanItemId);
                    table.ForeignKey(
                        name: "FK_MonitoringPlanItems_AspNetUsers_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringPlanItems_Entities_ResponsibleEntityId",
                        column: x => x.ResponsibleEntityId,
                        principalTable: "Entities",
                        principalColumn: "EntityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringPlanItems_Indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "Indicators",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringPlanItems_MonitoringPlans_MonitoringPlanId",
                        column: x => x.MonitoringPlanId,
                        principalTable: "MonitoringPlans",
                        principalColumn: "MonitoringPlanId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonitoringActions",
                columns: table => new
                {
                    MonitoringActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonitoringEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiskAssumptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateTime>(type: "Date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "Date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringActions", x => x.MonitoringActionId);
                    table.ForeignKey(
                        name: "FK_MonitoringActions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringActions_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringActions_MonitoringEvents_MonitoringEventId",
                        column: x => x.MonitoringEventId,
                        principalTable: "MonitoringEvents",
                        principalColumn: "MonitoringEventId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringActions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringActions_RiskAssumptions_RiskAssumptionId",
                        column: x => x.RiskAssumptionId,
                        principalTable: "RiskAssumptions",
                        principalColumn: "RiskAssumptionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringActions_Tasks_LinkedTaskId",
                        column: x => x.LinkedTaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssumptionUpdates",
                columns: table => new
                {
                    RiskAssumptionUpdateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiskAssumptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Likelihood = table.Column<int>(type: "int", nullable: false),
                    Impact = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssumptionUpdates", x => x.RiskAssumptionUpdateId);
                    table.ForeignKey(
                        name: "FK_RiskAssumptionUpdates_AspNetUsers_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssumptionUpdates_Dates_DateId",
                        column: x => x.DateId,
                        principalTable: "Dates",
                        principalColumn: "DateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssumptionUpdates_Documents_EvidenceDocumentId",
                        column: x => x.EvidenceDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAssumptionUpdates_RiskAssumptions_RiskAssumptionId",
                        column: x => x.RiskAssumptionId,
                        principalTable: "RiskAssumptions",
                        principalColumn: "RiskAssumptionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TheoriesOfChange_ProjectId",
                table: "TheoriesOfChange",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LogFrames_ProjectId",
                table: "LogFrames",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LogFrames_TheoryOfChangeId",
                table: "LogFrames",
                column: "TheoryOfChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_MonitoringType",
                table: "Indicators",
                column: "MonitoringType");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_ResponsibleUserId",
                table: "Indicators",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Data_Entity_Date",
                table: "Data",
                columns: new[] { "EntityId", "DateId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_IndicatorId",
                table: "BudgetLines",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_LogFrameNodeId",
                table: "BudgetLines",
                column: "LogFrameNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonsLearned_ComponentId",
                table: "LessonsLearned",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonsLearned_LogFrameNodeId",
                table: "LessonsLearned",
                column: "LogFrameNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonsLearned_MonitoringType",
                table: "LessonsLearned",
                column: "MonitoringType");

            migrationBuilder.CreateIndex(
                name: "IX_LessonsLearned_ProjectId",
                table: "LessonsLearned",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonsLearned_RecordedById",
                table: "LessonsLearned",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringActions_CreatedById",
                table: "MonitoringActions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringActions_EventId",
                table: "MonitoringActions",
                column: "MonitoringEventId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringActions_LinkedTaskId",
                table: "MonitoringActions",
                column: "LinkedTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringActions_OwnerUserId",
                table: "MonitoringActions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringActions_ProjectId",
                table: "MonitoringActions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringActions_RiskId",
                table: "MonitoringActions",
                column: "RiskAssumptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringActions_Status",
                table: "MonitoringActions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringChecks_CreatedById",
                table: "MonitoringChecks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringChecks_EntityId",
                table: "MonitoringChecks",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringChecks_EvidenceDocumentId",
                table: "MonitoringChecks",
                column: "EvidenceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringChecks_PeriodDateId",
                table: "MonitoringChecks",
                column: "PeriodDateId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringChecks_ProjectId",
                table: "MonitoringChecks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringChecks_Status",
                table: "MonitoringChecks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringChecks_Type",
                table: "MonitoringChecks",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringEvents_ConductedById",
                table: "MonitoringEvents",
                column: "ConductedById");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringEvents_CreatedById",
                table: "MonitoringEvents",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringEvents_DocumentId",
                table: "MonitoringEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringEvents_EntityId",
                table: "MonitoringEvents",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringEvents_MeReviewId",
                table: "MonitoringEvents",
                column: "MeReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringEvents_PeriodDateId",
                table: "MonitoringEvents",
                column: "PeriodDateId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringEvents_Project_Date",
                table: "MonitoringEvents",
                columns: new[] { "ProjectId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringEvents_Type",
                table: "MonitoringEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlanItems_IndicatorId",
                table: "MonitoringPlanItems",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlanItems_PlanId",
                table: "MonitoringPlanItems",
                column: "MonitoringPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlanItems_ResponsibleEntityId",
                table: "MonitoringPlanItems",
                column: "ResponsibleEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlanItems_ResponsibleUserId",
                table: "MonitoringPlanItems",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "UX_MonitoringPlanItems_Plan_Indicator",
                table: "MonitoringPlanItems",
                columns: new[] { "MonitoringPlanId", "IndicatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlans_CreatedById",
                table: "MonitoringPlans",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlans_FromDateId",
                table: "MonitoringPlans",
                column: "FromDateId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlans_OwnerUserId",
                table: "MonitoringPlans",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlans_ProjectId",
                table: "MonitoringPlans",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlans_Status",
                table: "MonitoringPlans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringPlans_ToDateId",
                table: "MonitoringPlans",
                column: "ToDateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIndicators_CreatedById",
                table: "ProjectIndicators",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIndicators_IndicatorId",
                table: "ProjectIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptions_CreatedById",
                table: "RiskAssumptions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptions_LogFrameNodeId",
                table: "RiskAssumptions",
                column: "LogFrameNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptions_OwnerUserId",
                table: "RiskAssumptions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptions_ProjectId",
                table: "RiskAssumptions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptions_Status",
                table: "RiskAssumptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptionUpdates_DateId",
                table: "RiskAssumptionUpdates",
                column: "DateId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptionUpdates_EvidenceDocumentId",
                table: "RiskAssumptionUpdates",
                column: "EvidenceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptionUpdates_RecordedById",
                table: "RiskAssumptionUpdates",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssumptionUpdates_RiskId",
                table: "RiskAssumptionUpdates",
                column: "RiskAssumptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLines_Indicators_IndicatorId",
                table: "BudgetLines",
                column: "IndicatorId",
                principalTable: "Indicators",
                principalColumn: "IndicatorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLines_LogFrameNodes_LogFrameNodeId",
                table: "BudgetLines",
                column: "LogFrameNodeId",
                principalTable: "LogFrameNodes",
                principalColumn: "LogFrameNodeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Indicators_AspNetUsers_ResponsibleUserId",
                table: "Indicators",
                column: "ResponsibleUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LogFrames_Projects_ProjectId",
                table: "LogFrames",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LogFrames_TheoriesOfChange_TheoryOfChangeId",
                table: "LogFrames",
                column: "TheoryOfChangeId",
                principalTable: "TheoriesOfChange",
                principalColumn: "TheoryOfChangeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TheoriesOfChange_Projects_ProjectId",
                table: "TheoriesOfChange",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Restrict);

            // Default existing logframe levels to weighted-average roll-up.
            migrationBuilder.Sql(@"
                UPDATE [LogFrameLevels] SET [RollupMethod] = 2 WHERE [RollupMethod] = 0;
            ");

            // Classify existing indicators from the component type they are attached to:
            // Input/Activity → Process, Output/Outcome → Results, Impact → Impact.
            migrationBuilder.Sql(@"
                UPDATE i
                SET i.[MonitoringType] =
                    CASE
                        WHEN x.[MaxComponentType] >= 4 THEN 9
                        WHEN x.[MaxComponentType] >= 2 THEN 7
                        ELSE 0
                    END
                FROM [Indicators] i
                INNER JOIN (
                    SELECT ci.[IndicatorId], MAX(c.[ComponentType]) AS [MaxComponentType]
                    FROM [ComponentIndicators] ci
                    INNER JOIN [Components] c ON c.[ComponentId] = ci.[ComponentId]
                    GROUP BY ci.[IndicatorId]
                ) x ON x.[IndicatorId] = i.[IndicatorId];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetLines_Indicators_IndicatorId",
                table: "BudgetLines");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetLines_LogFrameNodes_LogFrameNodeId",
                table: "BudgetLines");

            migrationBuilder.DropForeignKey(
                name: "FK_Indicators_AspNetUsers_ResponsibleUserId",
                table: "Indicators");

            migrationBuilder.DropForeignKey(
                name: "FK_LogFrames_Projects_ProjectId",
                table: "LogFrames");

            migrationBuilder.DropForeignKey(
                name: "FK_LogFrames_TheoriesOfChange_TheoryOfChangeId",
                table: "LogFrames");

            migrationBuilder.DropForeignKey(
                name: "FK_TheoriesOfChange_Projects_ProjectId",
                table: "TheoriesOfChange");

            migrationBuilder.DropTable(
                name: "LessonsLearned");

            migrationBuilder.DropTable(
                name: "MonitoringActions");

            migrationBuilder.DropTable(
                name: "MonitoringChecks");

            migrationBuilder.DropTable(
                name: "MonitoringPlanItems");

            migrationBuilder.DropTable(
                name: "ProjectIndicators");

            migrationBuilder.DropTable(
                name: "RiskAssumptionUpdates");

            migrationBuilder.DropTable(
                name: "MonitoringEvents");

            migrationBuilder.DropTable(
                name: "MonitoringPlans");

            migrationBuilder.DropTable(
                name: "RiskAssumptions");

            migrationBuilder.DropIndex(
                name: "IX_TheoriesOfChange_ProjectId",
                table: "TheoriesOfChange");

            migrationBuilder.DropIndex(
                name: "IX_LogFrames_ProjectId",
                table: "LogFrames");

            migrationBuilder.DropIndex(
                name: "IX_LogFrames_TheoryOfChangeId",
                table: "LogFrames");

            migrationBuilder.DropIndex(
                name: "IX_Indicators_MonitoringType",
                table: "Indicators");

            migrationBuilder.DropIndex(
                name: "IX_Indicators_ResponsibleUserId",
                table: "Indicators");

            migrationBuilder.DropIndex(
                name: "IX_Data_Entity_Date",
                table: "Data");

            migrationBuilder.DropIndex(
                name: "IX_BudgetLines_IndicatorId",
                table: "BudgetLines");

            migrationBuilder.DropIndex(
                name: "IX_BudgetLines_LogFrameNodeId",
                table: "BudgetLines");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TheoriesOfChange");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "LogFrames");

            migrationBuilder.DropColumn(
                name: "TheoryOfChangeId",
                table: "LogFrames");

            migrationBuilder.DropColumn(
                name: "RollupMethod",
                table: "LogFrameLevels");

            migrationBuilder.DropColumn(
                name: "AmberThreshold",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "GreenThreshold",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "MonitoringType",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "ResponsibleUserId",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Data");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Data");

            migrationBuilder.DropColumn(
                name: "IndicatorId",
                table: "BudgetLines");

            migrationBuilder.DropColumn(
                name: "LogFrameNodeId",
                table: "BudgetLines");

            migrationBuilder.CreateIndex(
                name: "IX_Data_EntityId",
                table: "Data",
                column: "EntityId");
        }
    }
}
