using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2CollaborationBudgetEtl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseCurrency",
                table: "Settings",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCommentId",
                table: "Comments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommittedAmount",
                table: "BudgetLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "DateId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DisbursedAmount",
                table: "BudgetLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BudgetLines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                table: "BudgetLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnboardingCompleted",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FxRates",
                columns: table => new
                {
                    FxRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ToCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "Date", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FxRates", x => x.FxRateId);
                    table.ForeignKey(
                        name: "FK_FxRates_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    ImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MappingConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationReportJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ValidRows = table.Column<int>(type: "int", nullable: false),
                    ErrorRows = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommittedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommittedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.ImportJobId);
                    table.ForeignKey(
                        name: "FK_ImportJobs_AspNetUsers_CommittedById",
                        column: x => x.CommittedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportJobs_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndicatorTags",
                columns: table => new
                {
                    IndicatorTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorTags", x => x.IndicatorTagId);
                    table.ForeignKey(
                        name: "FK_IndicatorTags_Indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "Indicators",
                        principalColumn: "IndicatorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndicatorTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    NotificationPreferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InApp = table.Column<bool>(type: "bit", nullable: false),
                    Email = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.NotificationPreferenceId);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobContents",
                columns: table => new
                {
                    ImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileContents = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobContents", x => x.ImportJobId);
                    table.ForeignKey(
                        name: "FK_ImportJobContents_ImportJobs_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJobs",
                        principalColumn: "ImportJobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_EntityId",
                table: "Projects",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_DateId",
                table: "BudgetLines",
                column: "DateId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_IsDeleted",
                table: "BudgetLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TaskId",
                table: "BudgetLines",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_CreatedById",
                table: "FxRates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_From_To_EffectiveFrom",
                table: "FxRates",
                columns: new[] { "FromCurrency", "ToCurrency", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CommittedById",
                table: "ImportJobs",
                column: "CommittedById");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatedById",
                table: "ImportJobs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatedOnUtc",
                table: "ImportJobs",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ImportType",
                table: "ImportJobs",
                column: "ImportType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status",
                table: "ImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorTags_IndicatorId",
                table: "IndicatorTags",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorTags_IndicatorId_TagId",
                table: "IndicatorTags",
                columns: new[] { "IndicatorId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorTags_TagId",
                table: "IndicatorTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_Type",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "Type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLines_Dates_DateId",
                table: "BudgetLines",
                column: "DateId",
                principalTable: "Dates",
                principalColumn: "DateId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLines_Tasks_TaskId",
                table: "BudgetLines",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId",
                principalTable: "Comments",
                principalColumn: "CommentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Entities_EntityId",
                table: "Projects",
                column: "EntityId",
                principalTable: "Entities",
                principalColumn: "EntityId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetLines_Dates_DateId",
                table: "BudgetLines");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetLines_Tasks_TaskId",
                table: "BudgetLines");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Comments_ParentCommentId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Entities_EntityId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "FxRates");

            migrationBuilder.DropTable(
                name: "ImportJobContents");

            migrationBuilder.DropTable(
                name: "IndicatorTags");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropIndex(
                name: "IX_Projects_EntityId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_BudgetLines_DateId",
                table: "BudgetLines");

            migrationBuilder.DropIndex(
                name: "IX_BudgetLines_IsDeleted",
                table: "BudgetLines");

            migrationBuilder.DropIndex(
                name: "IX_BudgetLines_TaskId",
                table: "BudgetLines");

            migrationBuilder.DropColumn(
                name: "BaseCurrency",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "CommittedAmount",
                table: "BudgetLines");

            migrationBuilder.DropColumn(
                name: "DateId",
                table: "BudgetLines");

            migrationBuilder.DropColumn(
                name: "DisbursedAmount",
                table: "BudgetLines");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BudgetLines");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "BudgetLines");

            migrationBuilder.DropColumn(
                name: "OnboardingCompleted",
                table: "AspNetUsers");
        }
    }
}
