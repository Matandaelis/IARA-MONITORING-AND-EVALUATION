using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTypedAnswersAndResponseSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Responses_QuestionnaireId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientDeviceId",
                table: "Responses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientGeneratedId",
                table: "Responses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Responses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAtUtc",
                table: "Responses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Responses",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ValueBoolean",
                table: "Answers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValueDate",
                table: "Answers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValueNumber",
                table: "Answers",
                type: "decimal(20,8)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Responses_ClientGeneratedId",
                table: "Responses",
                columns: new[] { "QuestionnaireId", "ClientGeneratedId" },
                unique: true,
                filter: "[ClientGeneratedId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_Question_Number",
                table: "Answers",
                columns: new[] { "QuestionId", "ValueNumber" },
                filter: "[ValueNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedAtUtc" });

            // One-time backfill of typed answer projections for existing rows, mirroring
            // AnswerValueProjector.Apply: numeric -> ValueNumber, yes/no/true/false ->
            // ValueBoolean, ISO dates -> ValueDate. A value populates at most one column.
            migrationBuilder.Sql(@"
UPDATE Answers
SET ValueNumber = TRY_CAST(Value AS decimal(20,8))
WHERE ValueNumber IS NULL
  AND TRY_CAST(Value AS decimal(20,8)) IS NOT NULL;

UPDATE Answers
SET ValueBoolean = CASE WHEN LOWER(Value) IN ('true','yes') THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
WHERE ValueBoolean IS NULL
  AND LOWER(Value) IN ('true','false','yes','no');

UPDATE Answers
SET ValueDate = TRY_CONVERT(date, Value)
WHERE ValueDate IS NULL
  AND TRY_CONVERT(date, Value) IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "UX_Responses_ClientGeneratedId",
                table: "Responses");

            migrationBuilder.DropIndex(
                name: "IX_Answers_Question_Number",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "ClientDeviceId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "ClientGeneratedId",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "ValueBoolean",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "ValueDate",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "ValueNumber",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_Responses_QuestionnaireId",
                table: "Responses",
                column: "QuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                column: "QuestionId");
        }
    }
}
