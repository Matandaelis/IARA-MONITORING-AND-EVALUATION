using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Monic.Web.Tests
{
    /// <summary>
    /// End-to-end numeric-accuracy tests: seeds the full demo project, enters 2 years of
    /// facility data through the REAL pipeline (Calculation.SaveAsync -> SaveData ->
    /// Aggregate -> RunCalculations stored procedures), then verifies that every
    /// aggregated & calculated value in the database exactly matches independently
    /// computed expectations.
    /// </summary>
    public class DemoProjectAccuracyTests : IClassFixture<MonicSqlServerFixture>
    {
        private static readonly decimal Tolerance = 0.0005m;

        private readonly MonicSqlServerFixture fixture;

        public DemoProjectAccuracyTests(MonicSqlServerFixture fixture)
        {
            this.fixture = fixture;
        }

        private sealed class DataRowProjection
        {
            public Guid IndicatorId { get; set; }
            public Guid EntityId { get; set; }
            public Guid DateId { get; set; }
            public decimal? Value { get; set; }
            public string Note { get; set; }
            public bool Aggregated { get; set; }
        }

        private sealed class LevelSpec
        {
            public string EntityCode { get; set; }
            public List<int> Facilities { get; set; }
            public bool IsFacility { get; set; }
        }

        [Fact]
        public async Task DemoProject_FullPipeline_ProducesAccurateAggregatesAndCalculations()
        {
            // ---------------------------------------------------------------- seed & enter data
            DemoDataSeedResult seed;
            await using (var db = fixture.CreateContext())
            {
                seed = await DemoDataSeeder.SeedAsync(db, fixture.AdminUserId);
            }

            // ---------------------------------------------------------------- load every value
            await using var verifyDb = fixture.CreateContext();

            var demoIndicatorIds = seed.IndicatorIdsByCode.Values.ToList();
            var demoEntityIds = seed.EntityIdsByCode.Values.ToList();
            var demoDateIds = seed.DateIdsByCode.Values.ToList();

            var rows = await verifyDb.Data
                .Where(o => demoIndicatorIds.Contains(o.IndicatorId)
                         && demoEntityIds.Contains(o.EntityId)
                         && demoDateIds.Contains(o.DateId))
                .Select(o => new DataRowProjection
                {
                    IndicatorId = o.IndicatorId,
                    EntityId = o.EntityId,
                    DateId = o.DateId,
                    Value = o.Value,
                    Note = o.Note,
                    Aggregated = o.Aggregated
                })
                .ToListAsync();

            var rowMap = rows.ToDictionary(
                o => (o.IndicatorId, o.EntityId, o.DateId));

            var errors = new List<string>();

            // ---------------------------------------------------------------- row-count integrity
            // 8 indicators x 15 entities x 34 dates = 4080 rows, all populated.
            var expectedRowCount = 8 * 15 * 34;
            Assert.True(rows.Count == expectedRowCount,
                $"Expected {expectedRowCount} demo data rows but found {rows.Count}");

            // ---------------------------------------------------------------- full grid verification
            var levels = BuildLevels();
            var dates = AllDates().ToList();

            foreach (var level in levels)
            {
                foreach (var collectedCode in DemoDataSeeder.IndicatorCodesCollected)
                {
                    // BED uses AVERAGE for entity roll-ups; everything else sums.
                    var entityAverage = !level.IsFacility && collectedCode == "BED";

                    foreach (var (monthIndex, dateLevel) in dates)
                    {
                        var dateCode = DateCodeFor(dateLevel, monthIndex);
                        var expected = DemoExpected.CollectedEntityAtDate(
                            collectedCode, level.Facilities, entityAverage, dateLevel, monthIndex);

                        // facility/month rows are raw data; everything else is aggregated
                        var expectedAggregated = !level.IsFacility || dateLevel != 2;

                        CheckValue(errors, seed, rowMap, collectedCode, level.EntityCode, dateCode,
                            $"collected {collectedCode} @ {level.EntityCode}/{dateCode}",
                            expected, expectedAggregated);
                    }
                }

                foreach (var calcCode in DemoDataSeeder.IndicatorCodesCalculated)
                {
                    foreach (var (monthIndex, dateLevel) in dates)
                    {
                        var dateCode = DateCodeFor(dateLevel, monthIndex);
                        var expected = DemoExpected.CalculatedAtKey(
                            calcCode, level.Facilities, false, dateLevel, monthIndex);

                        CheckValue(errors, seed, rowMap, calcCode, level.EntityCode, dateCode,
                            $"calculated {calcCode} @ {level.EntityCode}/{dateCode}",
                            expected, null);
                    }
                }
            }

            // ---------------------------------------------------------------- null handling
            // KIT is seeded null at facility F008 / month 2025-M12. TOT & CPS must propagate
            // the null (ConvertNullToZero=false) with the SQL note intact.
            var nullCases = new[]
            {
                new { Code = "KIT", Note = (string)null },
                new { Code = "TOT", Note = "Null value in data" },
                new { Code = "CPS", Note = "Null value in data" }
            };

            foreach (var nullCase in nullCases)
            {
                var key = (
                    seed.IndicatorIdsByCode[nullCase.Code],
                    seed.EntityIdsByCode["F008"],
                    seed.DateIdsByCode[DemoExpected.MonthCode(24)]);

                if (!rowMap.TryGetValue(key, out var row))
                {
                    errors.Add($"null case: {nullCase.Code} @ F008/2025-M12 row is missing");
                    continue;
                }

                if (row.Value != null)
                    errors.Add($"null case: {nullCase.Code} @ F008/2025-M12 expected null value, got {row.Value}");

                if (row.Note != nullCase.Note)
                    errors.Add($"null case: {nullCase.Code} @ F008/2025-M12 note expected '{nullCase.Note}', got '{row.Note}'");
            }

            // sanity: the null facility datum must NOT distort the country roll-up
            var countryKit = DemoExpected.CollectedEntityAtDate("KIT",
                DemoExpected.AllFacilityIndexes(), false, 2, 24);
            CheckValue(errors, seed, rowMap, "KIT", DemoExpected.CountryCode,
                DemoExpected.MonthCode(24),
                "KIT @ CTRY/2025-M12 (null exclusion check)", countryKit, true);

            Assert.True(errors.Count == 0,
                $"Accuracy errors ({errors.Count}):\n{string.Join("\n", errors.Take(25))}");
        }

        [Fact]
        public async Task DemoProject_Seeder_IsIdempotent()
        {
            DemoDataSeedResult first;
            DemoDataSeedResult second;

            await using (var db = fixture.CreateContext())
            {
                first = await DemoDataSeeder.SeedAsync(db, fixture.AdminUserId);
            }

            await using (var recheckDb = fixture.CreateContext())
            {
                second = await DemoDataSeeder.SeedAsync(recheckDb, fixture.AdminUserId);
            }

            Assert.True(second.DataAlreadySeeded,
                "Second seeding run should recognise the existing demo data");
            Assert.Equal(first.EnteredDatumCount, second.EnteredDatumCount);
            Assert.Equal(first.EntityIdsByCode.OrderBy(o => o.Key), second.EntityIdsByCode.OrderBy(o => o.Key));
            Assert.Equal(first.DateIdsByCode.OrderBy(o => o.Key), second.DateIdsByCode.OrderBy(o => o.Key));
            Assert.Equal(first.IndicatorIdsByCode.OrderBy(o => o.Key), second.IndicatorIdsByCode.OrderBy(o => o.Key));
        }

        // ------------------------------------------------------------------ helpers

        private List<LevelSpec> BuildLevels()
        {
            var levels = new List<LevelSpec>();

            for (var f = 1; f <= DemoExpected.FacilityCount; f++)
            {
                levels.Add(new LevelSpec
                {
                    EntityCode = $"F00{f}",
                    Facilities = new List<int> { f },
                    IsFacility = true
                });
            }

            for (var d = 0; d < DemoExpected.DistrictCodes.Length; d++)
            {
                levels.Add(new LevelSpec
                {
                    EntityCode = DemoExpected.DistrictCode(d),
                    Facilities = DemoExpected.FacilitiesOfDistrict(d).ToList(),
                    IsFacility = false
                });
            }

            for (var r = 0; r < DemoExpected.RegionCodes.Length; r++)
            {
                levels.Add(new LevelSpec
                {
                    EntityCode = DemoExpected.RegionCode(r),
                    Facilities = DemoExpected.FacilitiesOfRegion(r).ToList(),
                    IsFacility = false
                });
            }

            levels.Add(new LevelSpec
            {
                EntityCode = DemoExpected.CountryCode,
                Facilities = DemoExpected.AllFacilityIndexes().ToList(),
                IsFacility = false
            });

            return levels;
        }

        /// <summary></summary>
        /// dateLevel: 2 = month, 1 = quarter, 0 = year. Quarters are represented by their last month.
        private static IEnumerable<(int MonthIndex, int DateLevel)> AllDates()
        {
            for (var m = 1; m <= DemoExpected.MonthCount; m++)
                yield return (m, 2);

            var quarterLastMonths = new[] { 3, 6, 9, 12, 15, 18, 21, 24 };
            foreach (var q in quarterLastMonths)
                yield return (q, 1);

            yield return (12, 0);
            yield return (24, 0);
        }

        private static string DateCodeFor(int dateLevel, int monthIndex)
        {
            switch (dateLevel)
            {
                case 2: return DemoExpected.MonthCode(monthIndex);
                case 1: return DemoExpected.QuarterCode(monthIndex);
                case 0: return DemoExpected.YearCode(monthIndex);
                default: throw new ArgumentException("dateLevel");
            }
        }

        private static void CheckValue(
            List<string> errors,
            DemoDataSeedResult seed,
            Dictionary<(Guid IndicatorId, Guid EntityId, Guid DateId), DataRowProjection> rows,
            string indicatorCode,
            string entityCode,
            string dateCode,
            string context,
            decimal? expected,
            bool? expectedAggregated)
        {
            var key = (
                seed.IndicatorIdsByCode[indicatorCode],
                seed.EntityIdsByCode[entityCode],
                seed.DateIdsByCode[dateCode]);

            if (!rows.TryGetValue(key, out var row))
            {
                if (expected.HasValue)
                    errors.Add($"{context}: no data row found, expected {expected.Value}");
                return;
            }

            if (expected.HasValue)
            {
                if (row.Value == null || Math.Abs(row.Value.Value - expected.Value) > Tolerance)
                    errors.Add($"{context}: expected {expected.Value}, got {row.Value}");
            }
            else if (row.Value != null)
            {
                errors.Add($"{context}: expected no value, got {row.Value}");
            }

            if (expectedAggregated.HasValue && row.Aggregated != expectedAggregated.Value)
                errors.Add($"{context}: Aggregated flag expected {expectedAggregated.Value}, got {row.Aggregated}");
        }
    }
}
