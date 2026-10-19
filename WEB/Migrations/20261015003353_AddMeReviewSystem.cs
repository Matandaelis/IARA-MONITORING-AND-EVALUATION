using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMeReviewSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeReviews",
                columns: table => new
                {
                    MeReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OverallScore = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentSource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeReviews", x => x.MeReviewId);
                    table.ForeignKey(
                        name: "FK_MeReviews_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeReviewSections",
                columns: table => new
                {
                    MeReviewSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeReviewSections", x => x.MeReviewSectionId);
                    table.ForeignKey(
                        name: "FK_MeReviewSections_MeReviews_MeReviewId",
                        column: x => x.MeReviewId,
                        principalTable: "MeReviews",
                        principalColumn: "MeReviewId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeReviewFindings",
                columns: table => new
                {
                    MeReviewFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeReviewSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Criterion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationRef = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeReviewFindings", x => x.MeReviewFindingId);
                    table.ForeignKey(
                        name: "FK_MeReviewFindings_MeReviewSections_MeReviewSectionId",
                        column: x => x.MeReviewSectionId,
                        principalTable: "MeReviewSections",
                        principalColumn: "MeReviewSectionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeReviewFindings_MeReviews_MeReviewId",
                        column: x => x.MeReviewId,
                        principalTable: "MeReviews",
                        principalColumn: "MeReviewId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeReviewRecommendations",
                columns: table => new
                {
                    MeReviewRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeReviewFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecommendationText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionSteps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsImplemented = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeReviewRecommendations", x => x.MeReviewRecommendationId);
                    table.ForeignKey(
                        name: "FK_MeReviewRecommendations_MeReviewFindings_MeReviewFindingId",
                        column: x => x.MeReviewFindingId,
                        principalTable: "MeReviewFindings",
                        principalColumn: "MeReviewFindingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeReviewRecommendations_MeReviews_MeReviewId",
                        column: x => x.MeReviewId,
                        principalTable: "MeReviews",
                        principalColumn: "MeReviewId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeReviewFindings_MeReviewId",
                table: "MeReviewFindings",
                column: "MeReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_MeReviewFindings_MeReviewSectionId",
                table: "MeReviewFindings",
                column: "MeReviewSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MeReviewRecommendations_MeReviewFindingId",
                table: "MeReviewRecommendations",
                column: "MeReviewFindingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeReviewRecommendations_MeReviewId",
                table: "MeReviewRecommendations",
                column: "MeReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_MeReviews_CreatedById",
                table: "MeReviews",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MeReviews_CreatedDateUtc",
                table: "MeReviews",
                column: "CreatedDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MeReviews_Domain",
                table: "MeReviews",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_MeReviews_TargetEntityId",
                table: "MeReviews",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MeReviewSections_MeReviewId",
                table: "MeReviewSections",
                column: "MeReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeReviewRecommendations");

            migrationBuilder.DropTable(
                name: "MeReviewFindings");

            migrationBuilder.DropTable(
                name: "MeReviewSections");

            migrationBuilder.DropTable(
                name: "MeReviews");
        }
    }
}
