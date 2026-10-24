using System;
using System.Collections.Generic;
using System.IO;
using Monic.Web.Code;
using Monic.Web.Models;
using OfficeOpenXml;
using Xunit;

namespace Monic.Web.Tests
{
    public class MePlanDocumentBuilderTests
    {
        private Project CreateTestProject()
        {
            return new Project
            {
                ProjectId = Guid.NewGuid(),
                Name = "Tanzania Community Health & WASH Program",
                Sector = "Health / WASH",
                DonorName = "USAID / Global Fund",
                GrantCode = "TZ-HLTH-2026-001",
                TotalBudget = 1500000.00m,
                Currency = "USD",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2028, 12, 31),
                TargetDirectBeneficiaries = 45000,
                TargetIndirectBeneficiaries = 120000,
                CurrentPhase = "Implementation",
                Level4Label = "Goal (Impact)",
                Level3Label = "Outcome",
                Level2Label = "Output",
                Level1Label = "Activity"
            };
        }

        private LogFrame CreateTestLogFrame(Project project)
        {
            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "Core WASH Results Framework",
                ProjectId = project.ProjectId,
                TheoryOfChange = new TheoryOfChange
                {
                    Name = "Safe Water & Community Resilience Pathway"
                }
            };

            var rowGoal = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                LogFrameId = logFrame.LogFrameId,
                RowType = LogFrameRowType.Goals,
                RowNumber = 1,
                Description = "Reduced incidence of water-borne diseases among under-5 children",
                MeansOfVerification = "District Health Information System (DHIS2)",
                RisksAndAssumptions = "Macroeconomic stability and regional climate conditions remain stable."
            };

            var indGoal = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "IND-G01",
                Name = "Under-5 Diarrhea Incidence Rate per 1,000",
                DataType = DataType.Percent,
                LowerIsBetter = true,
                Baseline = 12.8m,
                OperationalDefinition = "Percentage of under-5 children diagnosed with acute diarrhea during annual health survey.",
                PurposeRationale = "Primary morbidity outcome indicator.",
                Frequency = DateType.Year,
                CalculationFormulaNarrative = "(Cases / Total U5 Population) * 100",
                DataQualityProtocol = "DHIS2 spot-check reconciliation with recount verification factor < 5%."
            };

            indGoal.IndicatorTargets.Add(new IndicatorTarget
            {
                IndicatorTargetId = Guid.NewGuid(),
                IndicatorId = indGoal.IndicatorId,
                TargetValue = 3.5m,
                Date = new Date { DateId = Guid.NewGuid(), SortOrder = 1, Code = "2026" }
            });

            rowGoal.LogFrameRowIndicators.Add(new LogFrameRowIndicator
            {
                LogFrameRowId = rowGoal.LogFrameRowId,
                IndicatorId = indGoal.IndicatorId,
                Indicator = indGoal
            });

            var rowOutput = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                LogFrameId = logFrame.LogFrameId,
                RowType = LogFrameRowType.Outputs,
                RowNumber = 2,
                Description = "Solar-powered community water points rehabilitated and functional",
                MeansOfVerification = "Engineering handover certificates & GPS mobile surveys",
                RisksAndAssumptions = "Supply chains for replacement solar inverters remain uninterrupted."
            };

            var indOutput = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "IND-OUT01",
                Name = "Number of solar-powered water points rehabilitated",
                DataType = DataType.Number,
                LowerIsBetter = false,
                Baseline = 0m,
                OperationalDefinition = "Total count of solar borehole installations fully rehabilitated and tested for potable water standard.",
                Frequency = DateType.Quarter,
                DataQualityProtocol = "100% engineering inspection checklist"
            };

            indOutput.IndicatorTargets.Add(new IndicatorTarget
            {
                IndicatorTargetId = Guid.NewGuid(),
                IndicatorId = indOutput.IndicatorId,
                TargetValue = 50m,
                Date = new Date { DateId = Guid.NewGuid(), SortOrder = 1, Code = "2026" }
            });

            rowOutput.LogFrameRowIndicators.Add(new LogFrameRowIndicator
            {
                LogFrameRowId = rowOutput.LogFrameRowId,
                IndicatorId = indOutput.IndicatorId,
                Indicator = indOutput
            });

            logFrame.LogFrameRows.Add(rowGoal);
            logFrame.LogFrameRows.Add(rowOutput);

            return logFrame;
        }

        [Fact]
        public void GeneratePdf_ReturnsValidPdfBytesWithHeader()
        {
            var project = CreateTestProject();
            var logFrame = CreateTestLogFrame(project);
            var indicators = new List<Indicator>();
            foreach (var r in logFrame.LogFrameRows)
            {
                foreach (var ri in r.LogFrameRowIndicators) indicators.Add(ri.Indicator);
            }

            var caps = new List<MonitoringAction>
            {
                new MonitoringAction
                {
                    MonitoringActionId = Guid.NewGuid(),
                    ProjectId = project.ProjectId,
                    Description = "Recount variance > 10% during Q2 borehole survey",
                    Notes = "Retrain enumerators on GPS coordinate capture and recount protocol",
                    DueDate = DateTime.UtcNow.AddDays(7),
                    Status = MonitoringActionStatus.Open
                }
            };

            var feedbacks = new List<BeneficiaryFeedback>
            {
                new BeneficiaryFeedback
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.ProjectId,
                    TrackingCode = "CFM-2026-0001",
                    Channel = "Hotline",
                    Category = "Water Point Functionality",
                    Severity = FeedbackSeverity.Medium,
                    Status = FeedbackStatus.Investigating,
                    Description = "Water pump in Village B experienced valve pressure leak"
                }
            };

            var pdfBytes = MePlanDocumentBuilder.GeneratePdf(project, logFrame, indicators, caps, feedbacks);

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, "PDF document should contain substantial bytes");

            // Verify standard PDF header %PDF (0x25, 0x50, 0x44, 0x46)
            Assert.Equal(0x25, pdfBytes[0]);
            Assert.Equal(0x50, pdfBytes[1]);
            Assert.Equal(0x44, pdfBytes[2]);
            Assert.Equal(0x46, pdfBytes[3]);
        }

        [Fact]
        public void GenerateExcel_ReturnsMultiTabWorkbookWithExpectedSheets()
        {
            var project = CreateTestProject();
            var logFrame = CreateTestLogFrame(project);
            var indicators = new List<Indicator>();
            foreach (var r in logFrame.LogFrameRows)
            {
                foreach (var ri in r.LogFrameRowIndicators) indicators.Add(ri.Indicator);
            }

            var excelBytes = MePlanDocumentBuilder.GenerateExcel(project, logFrame, indicators);

            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 2000, "Excel document should contain valid spreadsheet bytes");

            using var ms = new MemoryStream(excelBytes);
            using var xls = new ExcelPackage(ms);

            Assert.NotNull(xls.Workbook.Worksheets["Project Overview"]);
            Assert.NotNull(xls.Workbook.Worksheets["4x4 LogFrame Matrix"]);
            Assert.NotNull(xls.Workbook.Worksheets["PIRS Catalog"]);
            Assert.NotNull(xls.Workbook.Worksheets["DQA & CAPs Tracker"]);
            Assert.NotNull(xls.Workbook.Worksheets["Beneficiary Feedback Register"]);

            var wsOverview = xls.Workbook.Worksheets["Project Overview"];
            Assert.Equal("Project Name", wsOverview.Cells[3, 1].Value?.ToString());
            Assert.Equal(project.Name, wsOverview.Cells[3, 2].Value?.ToString());
        }

        [Fact]
        public void GeneratePdf_HandlesNullProjectMetadataGracefully()
        {
            var emptyProject = new Project { Name = "Empty Test Project" };
            var emptyLogFrame = new LogFrame { Name = "Empty Frame" };

            var pdfBytes = MePlanDocumentBuilder.GeneratePdf(emptyProject, emptyLogFrame, new List<Indicator>());

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 500);
            Assert.Equal(0x25, pdfBytes[0]); // %PDF
        }
    }
}
