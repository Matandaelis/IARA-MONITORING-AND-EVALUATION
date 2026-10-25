using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrectiveActionTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeviationPercent",
                table: "MonitoringActions",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IndicatorId",
                table: "MonitoringActions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerSource",
                table: "MonitoringActions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TriggeredOnUtc",
                table: "MonitoringActions",
                type: "Date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringActions_IndicatorId",
                table: "MonitoringActions",
                column: "IndicatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonitoringActions_Indicators_IndicatorId",
                table: "MonitoringActions",
                column: "IndicatorId",
                principalTable: "Indicators",
                principalColumn: "IndicatorId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonitoringActions_Indicators_IndicatorId",
                table: "MonitoringActions");

            migrationBuilder.DropIndex(
                name: "IX_MonitoringActions_IndicatorId",
                table: "MonitoringActions");

            migrationBuilder.DropColumn(
                name: "DeviationPercent",
                table: "MonitoringActions");

            migrationBuilder.DropColumn(
                name: "IndicatorId",
                table: "MonitoringActions");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "MonitoringActions");

            migrationBuilder.DropColumn(
                name: "TriggeredOnUtc",
                table: "MonitoringActions");
        }
    }
}
