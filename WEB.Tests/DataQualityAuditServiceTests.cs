using System;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class DataQualityAuditServiceTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public void AssessSpotCheck_GradeA_WhenDiscrepancyUnder5Percent()
        {
            var result = DataQualityAuditService.AssessSpotCheck(reportedValue: 100m, recountedValue: 102m);

            Assert.Equal("A", result.Grade);
            Assert.Equal(2.0m, result.DiscrepancyPercent);
            Assert.Equal(1.02m, result.VerificationFactor);
            Assert.False(result.TriggerCap);
            Assert.Contains("Acceptable", result.Interpretation);
        }

        [Fact]
        public void AssessSpotCheck_GradeB_WhenDiscrepancyBetween5And10Percent()
        {
            var result = DataQualityAuditService.AssessSpotCheck(reportedValue: 100m, recountedValue: 93m);

            Assert.Equal("B", result.Grade);
            Assert.Equal(7.0m, result.DiscrepancyPercent);
            Assert.Equal(0.93m, result.VerificationFactor);
            Assert.False(result.TriggerCap);
            Assert.Contains("Minor Discrepancy", result.Interpretation);
        }

        [Fact]
        public void AssessSpotCheck_GradeC_WhenDiscrepancyExceeds10Percent()
        {
            var result = DataQualityAuditService.AssessSpotCheck(reportedValue: 100m, recountedValue: 80m);

            Assert.Equal("C", result.Grade);
            Assert.Equal(20.0m, result.DiscrepancyPercent);
            Assert.Equal(0.8m, result.VerificationFactor);
            Assert.True(result.TriggerCap);
            Assert.Contains("Significant Discrepancy", result.Interpretation);
        }

        [Fact]
        public async Task PerformDqaAndTriggerCapAsync_CreatesMonitoringAction_WhenGradeC()
        {
            using var db = CreateInMemoryDbContext();

            var projectId = Guid.NewGuid();
            var indicatorId = Guid.NewGuid();
            var auditorId = Guid.NewGuid();

            var indicator = new Indicator
            {
                IndicatorId = indicatorId,
                Code = "WASH_01",
                Name = "Water Points Rehabilitated",
                Prefix = "",
                Suffix = "",
                Units = "Points"
            };
            db.Indicators.Add(indicator);
            await db.SaveChangesAsync();

            var summary = await DataQualityAuditService.PerformDqaAndTriggerCapAsync(
                db, projectId, indicatorId, reportedValue: 50m, recountedValue: 35m, siteName: "District Hospital", auditorUserId: auditorId);

            Assert.NotNull(summary);
            Assert.Equal("C", summary.Result.Grade);
            Assert.True(summary.Result.TriggerCap);
            Assert.NotNull(summary.GeneratedCap);

            var savedCap = await db.MonitoringActions.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.IndicatorId == indicatorId);
            Assert.NotNull(savedCap);
            Assert.Equal("DataQualityAudit", savedCap.TriggerSource);
            Assert.Equal(MonitoringActionStatus.Open, savedCap.Status);
            Assert.Equal(30.0m, savedCap.DeviationPercent); // (50 - 35) / 50 = 30%
            Assert.Contains("District Hospital", savedCap.Description);
            Assert.NotNull(savedCap.DueDate);
        }
    }
}
