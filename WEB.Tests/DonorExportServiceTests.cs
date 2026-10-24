using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Monic.Web.Code;
using Monic.Web.Models;
using OfficeOpenXml;
using Xunit;

namespace Monic.Web.Tests
{
    public class DonorExportServiceTests
    {
        private (Project project, LogFrame logFrame, List<Indicator> indicators) CreateTestContext()
        {
            var project = new Project
            {
                ProjectId = Guid.NewGuid(),
                Name = "National Malaria Reduction Initiative",
                Sector = "Health",
                DonorName = "Global Fund / USAID",
                GrantCode = "GF-MAL-2026",
                TotalBudget = 3200000m,
                Currency = "USD",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2027, 12, 31)
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "Malaria Results Framework",
                ProjectId = project.ProjectId
            };

            var rowOutcome = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                LogFrameId = logFrame.LogFrameId,
                RowType = LogFrameRowType.Outcomes,
                RowNumber = 1,
                Description = "Increased coverage of Long-Lasting Insecticidal Nets (LLINs) in target districts",
                RisksAndAssumptions = "Routine mass distribution campaigns execute according to national schedule."
            };

            var ind = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "MAL-OUT-01",
                Name = "Percentage of households with at least one LLIN for every two persons",
                DataType = DataType.Percent,
                LowerIsBetter = false,
                Baseline = 42m,
                OperationalDefinition = "Universal coverage threshold defined by WHO guidelines."
            };

            ind.IndicatorTargets.Add(new IndicatorTarget
            {
                IndicatorTargetId = Guid.NewGuid(),
                IndicatorId = ind.IndicatorId,
                TargetValue = 85m,
                Date = new Date { DateId = Guid.NewGuid(), SortOrder = 1, Code = "2026" }
            });

            ind.Data.Add(new Datum
            {
                IndicatorId = ind.IndicatorId,
                EntityId = Guid.NewGuid(),
                DateId = Guid.NewGuid(),
                Value = 78.5m,
                LastSavedDateUtc = DateTime.UtcNow
            });

            rowOutcome.LogFrameRowIndicators.Add(new LogFrameRowIndicator
            {
                LogFrameRowId = rowOutcome.LogFrameRowId,
                IndicatorId = ind.IndicatorId,
                Indicator = ind
            });

            logFrame.LogFrameRows.Add(rowOutcome);

            return (project, logFrame, new List<Indicator> { ind });
        }

        [Fact]
        public void GenerateIatiXml_ProducesConformantIatiV203Xml()
        {
            var (project, logFrame, indicators) = CreateTestContext();

            var xmlString = DonorExportService.GenerateIatiXml(project, logFrame, indicators);

            Assert.NotNull(xmlString);
            Assert.Contains("version=\"2.03\"", xmlString);
            Assert.Contains("<iati-activities", xmlString);
            Assert.Contains("<iati-activity", xmlString);
            Assert.Contains("<iati-identifier>", xmlString);
            Assert.Contains("GF-MAL-2026", xmlString);
            Assert.Contains("<reporting-org", xmlString);
            Assert.Contains("<result type=\"2\"", xmlString); // 2 = Outcome
            Assert.Contains("<indicator measure=\"1\"", xmlString);
            Assert.Contains("MAL-OUT-01", xmlString);
            Assert.Contains("<period-start", xmlString);
            Assert.Contains("<period-end", xmlString);

            // Verify parseable by System.Xml.Linq.XDocument
            var doc = XDocument.Parse(xmlString);
            Assert.Equal("2.03", doc.Root?.Attribute("version")?.Value);
        }

        [Fact]
        public void GenerateUsaidQprExcel_ProducesIpttSheetWithCalculatedDeviations()
        {
            var (project, logFrame, indicators) = CreateTestContext();

            var excelBytes = DonorExportService.GenerateUsaidQprExcel(project, logFrame, indicators);

            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 2000);

            using var ms = new MemoryStream(excelBytes);
            using var xls = new ExcelPackage(ms);

            var ws = xls.Workbook.Worksheets["USAID IPTT"];
            Assert.NotNull(ws);

            Assert.Equal("Award / Grant No:", ws.Cells[2, 1].Value?.ToString());
            Assert.Equal("GF-MAL-2026", ws.Cells[2, 2].Value?.ToString());
            Assert.Equal("Indicator No", ws.Cells[5, 1].Value?.ToString());
            Assert.Equal("MAL-OUT-01", ws.Cells[6, 1].Value?.ToString());
            Assert.Equal(85m, Convert.ToDecimal(ws.Cells[6, 9].Value)); // LOP Target
        }

        [Fact]
        public void GenerateEuRomExport_ProducesRomMatrixAndQualityCriteria()
        {
            var (project, logFrame, indicators) = CreateTestContext();

            var excelBytes = DonorExportService.GenerateEuRomExport(project, logFrame, indicators);

            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 2000);

            using var ms = new MemoryStream(excelBytes);
            using var xls = new ExcelPackage(ms);

            var wsMatrix = xls.Workbook.Worksheets["EU ROM LogFrame Matrix"];
            var wsCriteria = xls.Workbook.Worksheets["ROM Quality Criteria"];

            Assert.NotNull(wsMatrix);
            Assert.NotNull(wsCriteria);

            Assert.Contains("EUROPEAN COMMISSION RESULT-ORIENTED MONITORING", wsMatrix.Cells[1, 1].Value?.ToString());
            Assert.Equal("EU ROM FIVE QUALITY CRITERIA ASSESSMENT", wsCriteria.Cells[1, 1].Value?.ToString());
            Assert.Equal("1. Relevance & Quality of Design", wsCriteria.Cells[4, 1].Value?.ToString());
            Assert.Equal("A (Very Good)", wsCriteria.Cells[4, 3].Value?.ToString());
        }
    }
}
