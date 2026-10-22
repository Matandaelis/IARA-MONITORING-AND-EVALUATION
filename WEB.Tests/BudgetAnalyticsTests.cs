using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class BudgetAnalyticsTests : IDisposable
    {
        private readonly ApplicationDbContext db;
        private readonly Guid projectId = Guid.NewGuid();
        private readonly Guid categoryId = Guid.NewGuid();

        public BudgetAnalyticsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"budget-analytics-{Guid.NewGuid()}")
                .Options;

            db = new ApplicationDbContext(options);

            db.Projects.Add(new Project { ProjectId = projectId, Name = "Water for Senegal" });
            db.BudgetCategories.Add(new BudgetCategory { BudgetCategoryId = categoryId, Name = "Staff" });
            db.SaveChanges();
        }

        private BudgetLine AddLine(string currency, decimal planned, decimal committed, decimal disbursed, decimal expended, Guid? dateId = null, bool isDeleted = false)
        {
            var line = new BudgetLine
            {
                ProjectId = projectId,
                BudgetCategoryId = categoryId,
                Currency = currency,
                PlannedAmount = planned,
                CommittedAmount = committed,
                DisbursedAmount = disbursed,
                ActualAmount = expended,
                DateId = dateId,
                IsDeleted = isDeleted
            };
            db.BudgetLines.Add(line);
            db.SaveChanges();
            return line;
        }

        [Theory]
        [InlineData(null, "USD")]
        [InlineData("", "USD")]
        [InlineData("EUR", "EUR")]
        public void ResolveBaseCurrency_EmptyOrSet_ReturnsExpected(string baseCurrency, string expected)
        {
            Assert.Equal(expected, BudgetAnalytics.ResolveBaseCurrency(new Monic.Web.Models.Settings { BaseCurrency = baseCurrency }));
        }

        [Fact]
        public async System.Threading.Tasks.Task GetRateAsync_SameCurrency_ReturnsOne()
        {
            var rate = await BudgetAnalytics.GetRateAsync(db, "USD", "USD", DateTime.UtcNow);
            Assert.Equal(1m, rate);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetRateAsync_DirectRate_ReturnsLatestEffectiveNotFuture()
        {
            var asOf = new DateTime(2026, 6, 15);

            db.FxRates.Add(new FxRate { FromCurrency = "EUR", ToCurrency = "USD", Rate = 1.05m, EffectiveFrom = new DateTime(2026, 1, 1) });
            db.FxRates.Add(new FxRate { FromCurrency = "EUR", ToCurrency = "USD", Rate = 1.10m, EffectiveFrom = new DateTime(2026, 6, 1) });
            db.FxRates.Add(new FxRate { FromCurrency = "EUR", ToCurrency = "USD", Rate = 1.20m, EffectiveFrom = new DateTime(2026, 8, 1) }); // future
            db.SaveChanges();

            var rate = await BudgetAnalytics.GetRateAsync(db, "EUR", "USD", asOf);

            Assert.Equal(1.10m, rate);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetRateAsync_MissingDirect_FallsBackToInverse()
        {
            db.FxRates.Add(new FxRate { FromCurrency = "USD", ToCurrency = "GBP", Rate = 0.8m, EffectiveFrom = new DateTime(2026, 1, 1) });
            db.SaveChanges();

            var rate = await BudgetAnalytics.GetRateAsync(db, "GBP", "USD", new DateTime(2026, 6, 1));

            Assert.Equal(1.25m, rate); // 1 / 0.8
        }

        [Fact]
        public async System.Threading.Tasks.Task GetRateAsync_NoRateAtAll_ReturnsNull()
        {
            var rate = await BudgetAnalytics.GetRateAsync(db, "XXX", "USD", DateTime.UtcNow);
            Assert.Null(rate);
        }

        [Fact]
        public async System.Threading.Tasks.Task ComputeSummaryAsync_MixedCurrencies_ConvertsKnownAndFlagsUnknown()
        {
            // EUR line with a known rate: 100 planned × 1.10 = 110 USD
            db.FxRates.Add(new FxRate { FromCurrency = "EUR", ToCurrency = "USD", Rate = 1.10m, EffectiveFrom = new DateTime(2026, 1, 1) });
            AddLine("USD", 200m, 50m, 30m, 20m);
            AddLine("EUR", 100m, 10m, 5m, 2m);
            AddLine("JPY", 1000m, 0m, 0m, 0m); // no JPY rate → excluded + flagged
            db.SaveChanges();

            var summary = await BudgetAnalytics.ComputeSummaryAsync(db, projectId, "USD", inBaseCurrency: true);

            Assert.Equal("USD", summary.BaseCurrency);
            Assert.Contains("JPY", summary.MissingRates);

            var row = Assert.Single(summary.Rows);
            Assert.Equal(310m, row.Planned);  // 200 + 110 (JPY excluded)
            Assert.Equal(61m, row.Committed); // 50 + 11
            Assert.Equal(35.5m, row.Disbursed); // 30 + 5×1.10
            Assert.Equal(22.2m, row.Actual);    // 20 + 2×1.10
        }

        [Fact]
        public async System.Threading.Tasks.Task ComputeSummaryAsync_VarianceAndUtilization_Computed()
        {
            AddLine("USD", 200m, 0m, 0m, 100m);
            db.SaveChanges();

            var summary = await BudgetAnalytics.ComputeSummaryAsync(db, projectId, "USD", inBaseCurrency: true);
            var row = Assert.Single(summary.Rows);

            Assert.Equal(100m, row.Variance);
            Assert.Equal(50.0, row.Utilization);
        }

        [Fact]
        public async System.Threading.Tasks.Task ComputeSummaryAsync_SoftDeletedLines_Excluded()
        {
            AddLine("USD", 100m, 0m, 0m, 0m);
            AddLine("USD", 999m, 0m, 0m, 0m, isDeleted: true);
            db.SaveChanges();

            var summary = await BudgetAnalytics.ComputeSummaryAsync(db, projectId, "USD", inBaseCurrency: true);
            var row = Assert.Single(summary.Rows);

            Assert.Equal(100m, row.Planned);
        }

        [Fact]
        public async System.Threading.Tasks.Task ComputeBurnSeriesAsync_CumulativeExpended_AccumulatesInPeriodOrder()
        {
            var january = new Date { DateId = Guid.NewGuid(), Name = "Jan 2026", Code = "J26", OpenFrom = new DateTime(2026, 1, 1), OpenTo = new DateTime(2026, 1, 31), SortOrder = 1 };
            var february = new Date { DateId = Guid.NewGuid(), Name = "Feb 2026", Code = "F26", OpenFrom = new DateTime(2026, 2, 1), OpenTo = new DateTime(2026, 2, 28), SortOrder = 2 };
            var march = new Date { DateId = Guid.NewGuid(), Name = "Mar 2026", Code = "M26", OpenFrom = new DateTime(2026, 3, 1), OpenTo = new DateTime(2026, 3, 31), SortOrder = 3 };
            db.Dates.AddRange(january, february, march);

            AddLine("USD", 100m, 0m, 0m, 30m, dateId: january.DateId);
            AddLine("USD", 100m, 0m, 0m, 50m, dateId: february.DateId);
            AddLine("USD", 100m, 0m, 0m, 40m, dateId: march.DateId);
            db.SaveChanges();

            var points = await BudgetAnalytics.ComputeBurnSeriesAsync(db, projectId, "USD");

            Assert.Equal(3, points.Count);
            Assert.Equal(30m, points[0].CumulativeExpended);
            Assert.Equal(80m, points[1].CumulativeExpended);
            Assert.Equal(120m, points[2].CumulativeExpended);
            Assert.All(points, p => Assert.Equal(100m, p.Planned));
        }

        [Fact]
        public async System.Threading.Tasks.Task ComputeBurnSeriesAsync_LinesWithoutPeriod_Excluded()
        {
            AddLine("USD", 100m, 0m, 0m, 10m, dateId: null);
            db.SaveChanges();

            var points = await BudgetAnalytics.ComputeBurnSeriesAsync(db, projectId, "USD");

            Assert.Empty(points);
        }

        [Fact]
        public async System.Threading.Tasks.Task ComputeCostPerOutcomeAsync_ComputesExpendedDividedByLatestApprovedDatum()
        {
            var indicatorId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var dateId = Guid.NewGuid();

            db.Indicators.Add(new Indicator { IndicatorId = indicatorId, Code = "IND-01", Name = "People trained", Units = "People", Prefix = "", Suffix = "", EntityTypeId = Guid.NewGuid() });
            AddLine("USD", 1000m, 0m, 0m, 400m);

            var unapproved = new Datum { IndicatorId = indicatorId, EntityId = Guid.NewGuid(), DateId = dateId, Value = 100m, Aggregated = false, LastSavedDateUtc = DateTime.UtcNow.AddDays(-2), LastSavedById = Guid.NewGuid() };
            var approved = new Datum { IndicatorId = indicatorId, EntityId = Guid.NewGuid(), DateId = dateId, Value = 80m, Aggregated = false, LastSavedDateUtc = DateTime.UtcNow.AddDays(-1), LastSavedById = Guid.NewGuid() };
            db.Data.AddRange(unapproved, approved);
            // Approved is a SQL computed column with a private setter — set it via the change tracker for InMemory
            db.Entry(approved).Property("Approved").CurrentValue = true;
            db.SaveChanges();

            var result = await BudgetAnalytics.ComputeCostPerOutcomeAsync(db, projectId, indicatorId, "USD");

            Assert.Equal(400m, result.Expended);
            Assert.Equal(80m, result.OutcomeValue); // latest APPROVED datum, not the unapproved 100
            Assert.Equal(5m, result.CostPerOutcome);
        }

        public void Dispose()
        {
            db.Database.EnsureDeleted();
            db.Dispose();
        }
    }
}
