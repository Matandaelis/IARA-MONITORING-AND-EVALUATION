using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;
using OfficeOpenXml;
using Xunit;

namespace Monic.Web.Tests
{
    public class EndToEndMeLifecycleTests : IDisposable
    {
        private readonly ApplicationDbContext db;

        public EndToEndMeLifecycleTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"me-e2e-lifecycle-{Guid.NewGuid()}")
                .Options;

            db = new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            db.Database.EnsureDeleted();
            db.Dispose();
        }

        [Fact]
        public async Task UniversalMeSystem_EndToEndLifecycle_ExecutesSuccessfullyAcrossAllLayers()
        {
            // =========================================================================
            // LAYER 1: MULTI-SECTOR PROJECT DEFINITION & THEORY OF CHANGE
            // =========================================================================
            var project = new Project
            {
                ProjectId = Guid.NewGuid(),
                Name = "Integrated Resilient Agriculture & WASH Initiative (IRAWI)",
                Sector = "Multi-Sector (Agriculture, WASH, Climate)",
                DonorName = "USAID & European Commission",
                GrantCode = "AGR-WASH-2026-X",
                TotalBudget = 4500000m,
                Currency = "USD",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2028, 12, 31),
                TargetDirectBeneficiaries = 35000,
                TargetIndirectBeneficiaries = 120000,
                Level4Label = "Impact (Goal)",
                Level3Label = "Outcomes",
                Level2Label = "Outputs",
                Level1Label = "Activities"
            };
            db.Projects.Add(project);

            var toc = new TheoryOfChange
            {
                TheoryOfChangeId = Guid.NewGuid(),
                ProjectId = project.ProjectId,
                Name = "IRAWI Theory of Change: Climate Resilient Agriculture & WASH"
            };
            db.TheoriesOfChange.Add(toc);

            // =========================================================================
            // LAYER 1 & 3: 4x4 RESULTS LOGICAL FRAMEWORK (EC / USAID STANDARDS)
            // =========================================================================
            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                ProjectId = project.ProjectId,
                Name = "IRAWI Comprehensive LogFrame Matrix",
                TheoryOfChange = toc
            };
            db.LogFrames.Add(logFrame);

            var rowGoal = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                LogFrameId = logFrame.LogFrameId,
                RowType = LogFrameRowType.Goals,
                RowNumber = 1,
                Description = "Sustainably improve household resilience, food security, and health outcomes in target communities.",
                Indicators = "Household dietary diversity score (HDDS), Under-5 stunting prevalence",
                MeansOfVerification = "National demographic and health survey, baseline/endline household surveys",
                RisksAndAssumptions = "Macroeconomic stability and no catastrophic extreme climate events surpassing historical 50-year thresholds."
            };

            var rowOutcome = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                LogFrameId = logFrame.LogFrameId,
                RowType = LogFrameRowType.Outcomes,
                RowNumber = 1,
                Description = "Increased adoption of climate-resilient farming techniques and daily use of safe community water points.",
                Indicators = "Adoption rate of CSA practices, Percentage of households accessing >= 20L/person/day safe water",
                MeansOfVerification = "Quarterly post-distribution surveys, water point telemetry sensor logs",
                RisksAndAssumptions = "Communities maintain active water governance committees and participate in demonstration days."
            };

            var rowOutput = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                LogFrameId = logFrame.LogFrameId,
                RowType = LogFrameRowType.Outputs,
                RowNumber = 1,
                Description = "Smallholder farmers trained on regenerative agriculture and 30 solar boreholes constructed and operational.",
                Indicators = "Number of farmers completing FFS training, Number of solar boreholes commissioned",
                MeansOfVerification = "Training attendance registers with photo verification, engineering sign-off certificates",
                RisksAndAssumptions = "Groundwater aquifers yield sufficient volume; supply chains for solar pumping equipment remain intact."
            };

            var rowActivity = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                LogFrameId = logFrame.LogFrameId,
                RowType = LogFrameRowType.Activities,
                RowNumber = 1,
                Description = "Conduct 50 farmer field school modules and drill/install 30 high-capacity solar pumping systems.",
                Indicators = "Training sessions held, Drilling contracts awarded",
                MeansOfVerification = "Module curriculum records, procurement audit reports",
                RisksAndAssumptions = "Local municipal permits obtained on schedule."
            };

            logFrame.LogFrameRows.Add(rowGoal);
            logFrame.LogFrameRows.Add(rowOutcome);
            logFrame.LogFrameRows.Add(rowOutput);
            logFrame.LogFrameRows.Add(rowActivity);
            db.LogFrameRows.AddRange(rowGoal, rowOutcome, rowOutput, rowActivity);

            // Setup Entity & Reporting Date
            var entityType = new EntityType { EntityTypeId = Guid.NewGuid(), Name = "District Hub", Plural = "District Hubs" };
            var entity = new Entity { EntityId = Guid.NewGuid(), EntityTypeId = entityType.EntityTypeId, Code = "DIST-KILOSA", Name = "Kilosa District", ShortName = "Kilosa" };
            var dateQ1 = new Date { DateId = Guid.NewGuid(), Code = "2026-Q1", Name = "Quarter 1 2026", SortOrder = 1, DateType = DateType.Quarter, OpenFrom = DateTime.UtcNow, OpenTo = DateTime.UtcNow.AddYears(1) };
            db.EntityTypes.Add(entityType);
            db.Entities.Add(entity);
            db.Dates.Add(dateQ1);

            // =========================================================================
            // LAYER 1 & 3: INDICATOR & DISAGGREGATED TARGET ARCHITECTURE
            // =========================================================================
            var indicator = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "IRAWI-OUT-01",
                Name = "Number of smallholder farmers adopting climate-smart farming techniques",
                OperationalDefinition = "A farmer who has applied at least 2 soil conservation or drip-irrigation practices on >= 0.5 acres during the cropping season.",
                PurposeRationale = "Measures core project adoption fidelity and behavioural transformation towards climate resilience.",
                DataQualityProtocol = "Semi-annual field spot-checks verifying farmer logs against demonstration plots using GPS-tagged mobile surveys.",
                ReportingSchedule = "Quarterly",
                CalculationFormulaNarrative = "Sum of uniquely verified adopting farmers across all project intervention clusters.",
                Baseline = 250m,
                Units = "Farmers",
                Prefix = "",
                Suffix = "",
                SubcategoryId = Guid.NewGuid(),
                EntityTypeId = entityType.EntityTypeId,
                CreatedById = Guid.NewGuid(),
                LastSavedById = Guid.NewGuid(),
                CreatedDateUtc = DateTime.UtcNow,
                LastSavedDateUtc = DateTime.UtcNow,
                Frequency = DateType.Quarter
            };
            db.Indicators.Add(indicator);

            // Link Indicator to Output LogFrame row via LogFrameRowIndicator
            var rowInd = new LogFrameRowIndicator
            {
                LogFrameRowId = rowOutput.LogFrameRowId,
                IndicatorId = indicator.IndicatorId,
                Indicator = indicator
            };
            rowOutput.LogFrameRowIndicators.Add(rowInd);

            // Add Disaggregation Group and Options
            var genderGroup = new DisaggregationGroup
            {
                DisaggregationGroupId = Guid.NewGuid(),
                Name = "Gender"
            };
            var optFemale = new DisaggregationOption
            {
                DisaggregationOptionId = Guid.NewGuid(),
                DisaggregationGroupId = genderGroup.DisaggregationGroupId,
                Name = "Female",
                Code = "F"
            };
            var optMale = new DisaggregationOption
            {
                DisaggregationOptionId = Guid.NewGuid(),
                DisaggregationGroupId = genderGroup.DisaggregationGroupId,
                Name = "Male",
                Code = "M"
            };
            genderGroup.Options.Add(optFemale);
            genderGroup.Options.Add(optMale);
            db.DisaggregationGroups.Add(genderGroup);
            db.DisaggregationOptions.AddRange(optFemale, optMale);

            // Add Disaggregated Targets
            var targetQ1Female = new IndicatorTarget
            {
                IndicatorTargetId = Guid.NewGuid(),
                IndicatorId = indicator.IndicatorId,
                TargetValue = 600m,
                Date = dateQ1,
                DateId = dateQ1.DateId,
                Entity = entity,
                EntityId = entity.EntityId,
                DisaggregationOption = optFemale,
                DisaggregationOptionId = optFemale.DisaggregationOptionId
            };
            var targetQ1Male = new IndicatorTarget
            {
                IndicatorTargetId = Guid.NewGuid(),
                IndicatorId = indicator.IndicatorId,
                TargetValue = 600m,
                Date = dateQ1,
                DateId = dateQ1.DateId,
                Entity = entity,
                EntityId = entity.EntityId,
                DisaggregationOption = optMale,
                DisaggregationOptionId = optMale.DisaggregationOptionId
            };
            indicator.IndicatorTargets.Add(targetQ1Female);
            indicator.IndicatorTargets.Add(targetQ1Male);
            db.IndicatorTargets.AddRange(targetQ1Female, targetQ1Male);

            // =========================================================================
            // LAYER 2: FIELD SURVEY CONFIGURATION & FORM-INDICATOR MAPPING
            // =========================================================================
            var questionnaire = new Questionnaire
            {
                QuestionnaireId = Guid.NewGuid(),
                Name = "Farmer Field Adoption Verification Tool",
                EntityTypeId = entityType.EntityTypeId,
                CompletionText = "Thank you for completing this survey."
            };
            var section = new Section
            {
                SectionId = Guid.NewGuid(),
                QuestionnaireId = questionnaire.QuestionnaireId,
                Name = "Farming Practices"
            };
            var qAdopt = new Question
            {
                QuestionId = Guid.NewGuid(),
                SectionId = section.SectionId,
                Code = "Q_ADOPTED",
                Text = "Has the farmer applied at least 2 climate-smart practices?",
                QuestionType = QuestionType.Text
            };
            var qAcreage = new Question
            {
                QuestionId = Guid.NewGuid(),
                SectionId = section.SectionId,
                Code = "Q_ACRES",
                Text = "Total acreage under climate-smart management",
                QuestionType = QuestionType.Number
            };
            section.Questions.Add(qAdopt);
            section.Questions.Add(qAcreage);
            questionnaire.Sections.Add(section);
            db.Questionnaires.Add(questionnaire);

            var mapping = new FormIndicatorMapping
            {
                FormIndicatorMappingId = Guid.NewGuid(),
                QuestionnaireId = questionnaire.QuestionnaireId,
                IndicatorId = indicator.IndicatorId,
                Aggregation = MappingAggregation.Count,
                CountOnlySubmitted = false,
                IsActive = true
            };
            mapping.QuestionLinks.Add(new QuestionIndicatorLink
            {
                QuestionIndicatorLinkId = Guid.NewGuid(),
                FormIndicatorMappingId = mapping.FormIndicatorMappingId,
                QuestionId = qAdopt.QuestionId,
                Role = QuestionIndicatorRole.Value,
                ValueMapJson = "{\"Yes\": 1.0, \"No\": 0.0}"
            });
            db.FormIndicatorMappings.Add(mapping);

            await db.SaveChangesAsync();

            // =========================================================================
            // LAYER 2: SURVEY SUBMISSION & RUNTIME CONTRIBUTION ENGINE ROLL-UP
            // =========================================================================
            var response1 = new Response
            {
                ResponseId = Guid.NewGuid(),
                QuestionnaireId = questionnaire.QuestionnaireId,
                EntityId = entity.EntityId,
                DateId = dateQ1.DateId,
                CreatedOnUtc = DateTime.UtcNow,
                SubmittedOnUtc = DateTime.UtcNow,
                TotalQuestions = 2,
                CompletedQuestions = 2
            };
            response1.Answers.Add(new Answer
            {
                AnswerId = Guid.NewGuid(),
                ResponseId = response1.ResponseId,
                QuestionId = qAdopt.QuestionId,
                Value = "Yes",
                ValueBoolean = true
            });
            response1.Answers.Add(new Answer
            {
                AnswerId = Guid.NewGuid(),
                ResponseId = response1.ResponseId,
                QuestionId = qAcreage.QuestionId,
                Value = "2.5",
                ValueNumber = 2.5m
            });
            db.Responses.Add(response1);
            await db.SaveChangesAsync();

            // Process Contributions through the engine
            var contributions = await FormIndicatorContributionEngine.ProcessResponseContributionsAsync(db, response1.ResponseId);
            Assert.Single(contributions);
            Assert.Equal(1m, contributions[0].Value);
            Assert.Equal(indicator.IndicatorId, contributions[0].IndicatorId);

            // Verify live Datum updated in database
            var liveDatum = await db.Data.FirstOrDefaultAsync(d => d.IndicatorId == indicator.IndicatorId && d.EntityId == entity.EntityId && d.DateId == dateQ1.DateId);
            Assert.NotNull(liveDatum);
            Assert.Equal(1m, liveDatum.Value);

            // =========================================================================
            // LAYER 2: ROUTINE DATA QUALITY AUDIT (RDQA) & CORRECTIVE ACTION PLANS (CAP)
            // =========================================================================
            // Spot check: Reported = 1200, Recounted = 980 (Discrepancy = 18.33%, Grade C)
            var dqaResult = DataQualityAuditService.AssessSpotCheck(1200m, 980m);
            Assert.Equal("C", dqaResult.Grade);
            Assert.True(dqaResult.TriggerCap);
            Assert.True(dqaResult.DiscrepancyPercent > 10m);

            // Audit Summary with CAP Generation
            var dqaSummary = await DataQualityAuditService.PerformDqaAndTriggerCapAsync(
                db,
                project.ProjectId,
                indicator.IndicatorId,
                1200m,
                980m,
                siteName: "Kilosa Central Hub",
                auditorUserId: null
            );
            Assert.NotNull(dqaSummary.GeneratedCap);
            Assert.Equal(MonitoringActionStatus.Open, dqaSummary.GeneratedCap.Status);
            Assert.True(dqaSummary.GeneratedCap.DueDate <= DateTime.UtcNow.Date.AddDays(12));

            // =========================================================================
            // LAYER 4: COMPREHENSIVE M&E PLAN EXPORT (PDF)
            // =========================================================================
            var caps = new List<MonitoringAction> { dqaSummary.GeneratedCap };
            var feedbacks = new List<BeneficiaryFeedback>
            {
                new BeneficiaryFeedback
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.ProjectId,
                    TrackingCode = "CFM-2026-IRAWI-01",
                    Channel = "Mobile SMS",
                    Category = "Agriculture Demonstration",
                    Severity = FeedbackSeverity.Low,
                    Status = FeedbackStatus.Resolved,
                    Description = "Request for earlier seed distribution ahead of the short rains"
                }
            };
            var indicatorList = new List<Indicator> { indicator };

            var pdfBytes = MePlanDocumentBuilder.GeneratePdf(project, logFrame, indicatorList, caps, feedbacks);
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, "Generated M&E Plan PDF must be non-empty and formatted");
            Assert.Equal(0x25, pdfBytes[0]); // %PDF header

            // =========================================================================
            // LAYER 4: DONOR EXPORTS (IATI XML, USAID QPR, EU ROM WORKBOOK)
            // =========================================================================
            var iatiXml = DonorExportService.GenerateIatiXml(project, logFrame, indicatorList);
            Assert.False(string.IsNullOrWhiteSpace(iatiXml));
            var xDoc = XDocument.Parse(iatiXml);
            Assert.NotNull(xDoc.Root);
            Assert.Equal("iati-activities", xDoc.Root.Name.LocalName);
            Assert.Contains(indicator.Code, iatiXml);
            Assert.Contains("IRAWI", iatiXml);
            Assert.Equal(project.Name, xDoc.Descendants("title").FirstOrDefault()?.Value);

            // USAID QPR IPTT Excel
            var usaidBytes = DonorExportService.GenerateUsaidQprExcel(project, logFrame, indicatorList);
            Assert.NotNull(usaidBytes);
            Assert.True(usaidBytes.Length > 1000, "USAID QPR Excel workbook must be non-empty");

            using (var ms = new MemoryStream(usaidBytes))
            using (var package = new ExcelPackage(ms))
            {
                var sheet = package.Workbook.Worksheets["USAID IPTT"];
                Assert.NotNull(sheet);
                Assert.Equal("Award / Grant No:", sheet.Cells[2, 1].Value?.ToString());
                Assert.Equal("AGR-WASH-2026-X", sheet.Cells[2, 2].Value?.ToString());
            }

            // European Commission ROM Workbook
            var euBytes = DonorExportService.GenerateEuRomExport(project, logFrame, indicatorList);
            Assert.NotNull(euBytes);
            Assert.True(euBytes.Length > 1000, "EU ROM Excel workbook must be non-empty");

            using (var ms = new MemoryStream(euBytes))
            using (var package = new ExcelPackage(ms))
            {
                var logFrameSheet = package.Workbook.Worksheets["EU ROM LogFrame Matrix"];
                var romSheet = package.Workbook.Worksheets["ROM Quality Criteria"];
                Assert.NotNull(logFrameSheet);
                Assert.NotNull(romSheet);
                Assert.Equal("EUROPEAN COMMISSION RESULT-ORIENTED MONITORING (ROM) LOGICAL FRAMEWORK", logFrameSheet.Cells[1, 1].Value?.ToString());
            }
        }
    }
}
