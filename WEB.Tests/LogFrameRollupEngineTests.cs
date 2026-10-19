using System;
using System.Collections.Generic;
using System.Text;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class LogFrameRollupEngineTests
    {
        [Fact]
        public void ComputeRollup_NullLogFrame_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LogFrameRollupEngine.ComputeRollup(null));
        }

        [Fact]
        public void ComputeRollup_EmptyLogFrame_ReturnsZeroedRollup()
        {
            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "Project Alpha",
                LogFrameRows = new List<LogFrameRow>()
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            Assert.Equal(logFrame.LogFrameId, rollup.LogFrameId);
            Assert.Equal("Project Alpha", rollup.LogFrameName);
            Assert.Equal(0, rollup.TotalRows);
            Assert.Equal(0, rollup.TotalIndicators);
            Assert.Null(rollup.OverallAchievement);
            Assert.Equal(VarianceEngine.StatusNotApplicable, rollup.OverallStatus);
        }

        [Fact]
        public void ComputeRollup_SingleRow_StandardIndicator_CalculatesCorrectVarianceAndStatus()
        {
            var indId = Guid.NewGuid();
            var indicator = new Indicator
            {
                IndicatorId = indId,
                Code = "IND-01",
                Name = "Beneficiaries Trained",
                Units = "People",
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget>
                {
                    new IndicatorTarget { TargetValue = 1000m, BaselineValue = 100m }
                },
                Data = new List<Datum>
                {
                    new Datum { Value = 850m }
                }
            };

            var row = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outcomes,
                Description = "Outcome 1: Enhanced Capacity",
                MeansOfVerification = "Training registers",
                RisksAndAssumptions = "Stable security",
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = indId, Indicator = indicator }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "Health Project",
                LogFrameRows = new List<LogFrameRow> { row }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            Assert.Equal(1, rollup.TotalRows);
            Assert.Equal(1, rollup.TotalIndicators);
            Assert.Equal(85.0m, rollup.OverallAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, rollup.OverallStatus);
            Assert.Equal(1, rollup.TotalOnTrack);
            Assert.Equal(0, rollup.TotalUnderperforming);

            var rowResult = rollup.Rows[0];
            Assert.Equal("Outcome", rowResult.RowTypeName);
            Assert.Equal(85.0m, rowResult.AverageAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, rowResult.RowStatus);

            var indResult = rowResult.Indicators[0];
            Assert.Equal("IND-01", indResult.Code);
            Assert.Equal(100m, indResult.Baseline);
            Assert.Equal(1000m, indResult.Target);
            Assert.Equal(850m, indResult.Actual);
            Assert.Equal(85.0m, indResult.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, indResult.StatusBadge);
            Assert.Equal(-150m, indResult.AbsoluteVariance);
        }

        [Fact]
        public void ComputeRollup_InvertedIndicator_HandlesLowerIsBetter()
        {
            var indId = Guid.NewGuid();
            var indicator = new Indicator
            {
                IndicatorId = indId,
                Code = "IND-02",
                Name = "Child Stunting Rate",
                Units = "%",
                Direction = IndicatorDirection.LowerIsBetter,
                IndicatorTargets = new List<IndicatorTarget>
                {
                    new IndicatorTarget { TargetValue = 20m, BaselineValue = 35m }
                },
                Data = new List<Datum>
                {
                    new Datum { Value = 15m } // lower than target -> exceeding!
                }
            };

            var row = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Goals,
                Description = "Goal: Reduce Malnutrition",
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = indId, Indicator = indicator }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "Nutrition Program",
                LogFrameRows = new List<LogFrameRow> { row }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            // 100 + ((20 - 15) / 20) * 100 = 125% -> Exceeding
            Assert.Equal(125.0m, rollup.OverallAchievement);
            Assert.Equal(VarianceEngine.StatusExceeding, rollup.OverallStatus);
            Assert.Equal(1, rollup.TotalExceeding);
        }

        [Fact]
        public void ComputeRollup_MultipleIndicators_CalculatesAverageAchievementAndCounts()
        {
            var ind1 = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "IND-A",
                Name = "Metric A",
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget> { new IndicatorTarget { TargetValue = 100m } },
                Data = new List<Datum> { new Datum { Value = 50m } } // 50% -> Underperforming
            };

            var ind2 = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "IND-B",
                Name = "Metric B",
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget> { new IndicatorTarget { TargetValue = 100m } },
                Data = new List<Datum> { new Datum { Value = 100m } } // 100% -> OnTrack
            };

            var row = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outputs,
                Description = "Output 1: Deliveries",
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = ind1.IndicatorId, Indicator = ind1 },
                    new LogFrameRowIndicator { IndicatorId = ind2.IndicatorId, Indicator = ind2 }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "Multi-Indicator Test",
                LogFrameRows = new List<LogFrameRow> { row }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            // Average of 50% and 100% = 75% -> OnTrack (75% <= 75% <= 110%)
            Assert.Equal(75.0m, rollup.OverallAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, rollup.OverallStatus);
            Assert.Equal(1, rollup.TotalOnTrack);
            Assert.Equal(1, rollup.TotalUnderperforming);
            Assert.Equal(0, rollup.TotalExceeding);
            Assert.Equal(0, rollup.TotalNoData);
        }

        [Fact]
        public void ComputeRollup_MultiLevelLogFrame_CalculatesProjectWideAggregates()
        {
            var goalInd = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "GOAL-1",
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget> { new IndicatorTarget { TargetValue = 200m } },
                Data = new List<Datum> { new Datum { Value = 240m } } // 120% -> Exceeding
            };

            var activityInd = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "ACT-1",
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget> { new IndicatorTarget { TargetValue = 100m } },
                Data = new List<Datum> { new Datum { Value = 60m } } // 60% -> Underperforming
            };

            var goalRow = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Goals,
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = goalInd.IndicatorId, Indicator = goalInd }
                }
            };

            var activityRow = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Activities,
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = activityInd.IndicatorId, Indicator = activityInd }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "Full Hierarchy Program",
                LogFrameRows = new List<LogFrameRow> { goalRow, activityRow }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            Assert.Equal(2, rollup.TotalRows);
            Assert.Equal(2, rollup.TotalIndicators);
            // Average of 120% and 60% = 90% -> OnTrack
            Assert.Equal(90.0m, rollup.OverallAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, rollup.OverallStatus);
            Assert.Equal(1, rollup.TotalExceeding);
            Assert.Equal(1, rollup.TotalUnderperforming);
        }

        [Fact]
        public void ComputeRollup_ZeroTargetBoundary_SafeguardsCorrectly()
        {
            var indZero = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "ZERO-01",
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget> { new IndicatorTarget { TargetValue = 0m } },
                Data = new List<Datum> { new Datum { Value = 0m } }
            };

            var row = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outcomes,
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = indZero.IndicatorId, Indicator = indZero }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                LogFrameRows = new List<LogFrameRow> { row }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            Assert.Equal(100m, rollup.OverallAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, rollup.OverallStatus);
        }

        [Fact]
        public void ComputeRollup_MissingDataOrTarget_MarksNotApplicable()
        {
            var indNoActual = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "NO-ACTUAL",
                IndicatorTargets = new List<IndicatorTarget> { new IndicatorTarget { TargetValue = 500m } },
                Data = new List<Datum>()
            };

            var indNoTarget = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "NO-TARGET",
                IndicatorTargets = new List<IndicatorTarget>(),
                Data = new List<Datum> { new Datum { Value = 150m } }
            };

            var row = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outputs,
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = indNoActual.IndicatorId, Indicator = indNoActual },
                    new LogFrameRowIndicator { IndicatorId = indNoTarget.IndicatorId, Indicator = indNoTarget }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                LogFrameRows = new List<LogFrameRow> { row }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            Assert.Equal(2, rollup.TotalIndicators);
            Assert.Equal(2, rollup.TotalNoData);
            Assert.Null(rollup.OverallAchievement);
            Assert.Equal(VarianceEngine.StatusNotApplicable, rollup.OverallStatus);
        }

        [Fact]
        public void GenerateDonorCsv_ValidRollup_ProducesValidCsvMatrixWithHeadersAndRows()
        {
            var ind = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "KPI-01",
                Name = "Water Points Rehabilitated",
                Units = "Wells",
                IndicatorTargets = new List<IndicatorTarget> { new IndicatorTarget { TargetValue = 50m, BaselineValue = 10m } },
                Data = new List<Datum> { new Datum { Value = 45m } }
            };

            var row = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outputs,
                Description = "Rehabilitated community infrastructure",
                MeansOfVerification = "Site inspection reports",
                RisksAndAssumptions = "Material availability",
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = ind.IndicatorId, Indicator = ind }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "WASH Program 2026",
                LogFrameRows = new List<LogFrameRow> { row }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);
            var csvBytes = LogFrameRollupEngine.GenerateDonorCsv(rollup);

            Assert.NotNull(csvBytes);
            Assert.True(csvBytes.Length > 3);

            // Assert UTF-8 BOM (0xEF, 0xBB, 0xBF)
            Assert.Equal(0xEF, csvBytes[0]);
            Assert.Equal(0xBB, csvBytes[1]);
            Assert.Equal(0xBF, csvBytes[2]);

            var csvText = Encoding.UTF8.GetString(csvBytes);

            // Assert headers
            Assert.Contains("ObjectiveLevel", csvText);
            Assert.Contains("RowNumber", csvText);
            Assert.Contains("Description", csvText);
            Assert.Contains("IndicatorCode", csvText);
            Assert.Contains("IndicatorName", csvText);
            Assert.Contains("Units", csvText);
            Assert.Contains("Baseline", csvText);
            Assert.Contains("Target", csvText);
            Assert.Contains("Actual", csvText);
            Assert.Contains("AbsoluteVariance", csvText);
            Assert.Contains("PercentageAchievement", csvText);
            Assert.Contains("StatusBadge", csvText);
            Assert.Contains("MeansOfVerification", csvText);
            Assert.Contains("RisksAndAssumptions", csvText);

            // Assert row content
            Assert.Contains("Output", csvText);
            Assert.Contains("KPI-01", csvText);
            Assert.Contains("Water Points Rehabilitated", csvText);
            Assert.Contains("Wells", csvText);
            Assert.Contains("50", csvText);
            Assert.Contains("45", csvText);
            Assert.Contains("-5", csvText);
            Assert.Contains("90", csvText);
            Assert.Contains("OnTrack", csvText);
        }

        [Fact]
        public void ComputeRollup_MostRecentIndicator_DoesNotSumAcrossPeriods()
        {
            var dateQ1 = new Date { DateId = Guid.NewGuid(), Name = "2026-Q1", SortOrder = 1 };
            var dateQ2 = new Date { DateId = Guid.NewGuid(), Name = "2026-Q2", SortOrder = 2 };

            var ind = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "RATE-01",
                Name = "Access to Sanitation Rate",
                Units = "%",
                DateAggregationType = AggregationType.MostRecent,
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget>
                {
                    new IndicatorTarget { DateId = dateQ1.DateId, Date = dateQ1, TargetValue = 60m, BaselineValue = 40m },
                    new IndicatorTarget { DateId = dateQ2.DateId, Date = dateQ2, TargetValue = 80m, BaselineValue = 40m }
                },
                Data = new List<Datum>
                {
                    new Datum { DateId = dateQ1.DateId, Date = dateQ1, Value = 55m },
                    new Datum { DateId = dateQ2.DateId, Date = dateQ2, Value = 76m }
                }
            };

            var row = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outcomes,
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = ind.IndicatorId, Indicator = ind }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                Name = "WASH Outcome",
                LogFrameRows = new List<LogFrameRow> { row }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            // For MostRecent, Q2 target (80) and Q2 actual (76) should be used, NOT the sums (140 target, 131 actual)
            var indResult = rollup.Rows[0].Indicators[0];
            Assert.Equal(80m, indResult.Target);
            Assert.Equal(76m, indResult.Actual);
            Assert.Equal(95.0m, indResult.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, indResult.StatusBadge);
            Assert.Equal(-4m, indResult.AbsoluteVariance);
            Assert.Equal(40m, indResult.Baseline);
        }

        [Fact]
        public void ComputeRollup_PreventsDoubleCountingOfAggregatedData()
        {
            var ind = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "SUM-01",
                Name = "People Reached",
                DateAggregationType = AggregationType.Sum,
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget>
                {
                    new IndicatorTarget { TargetValue = 100m }
                },
                Data = new List<Datum>
                {
                    // Raw leaf observations
                    new Datum { Value = 40m, Aggregated = false },
                    new Datum { Value = 50m, Aggregated = false },
                    // Stored-procedure precalculated roll-up row (should be ignored to prevent double-counting)
                    new Datum { Value = 90m, Aggregated = true }
                }
            };

            var row = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outputs,
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = ind.IndicatorId, Indicator = ind }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                LogFrameRows = new List<LogFrameRow> { row }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            var indResult = rollup.Rows[0].Indicators[0];
            // Actual must be 40 + 50 = 90, NOT 40 + 50 + 90 = 180
            Assert.Equal(90m, indResult.Actual);
            Assert.Equal(90.0m, indResult.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, indResult.StatusBadge);
        }

        [Fact]
        public void ComputeRollup_DeduplicatesSharedIndicatorsInProjectTotals()
        {
            var sharedInd = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "SHARED-01",
                Direction = IndicatorDirection.HigherIsBetter,
                IndicatorTargets = new List<IndicatorTarget> { new IndicatorTarget { TargetValue = 100m } },
                Data = new List<Datum> { new Datum { Value = 80m } } // 80% -> OnTrack
            };

            // Same indicator linked to both an Outcome row and an Output row
            var outcomeRow = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outcomes,
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = sharedInd.IndicatorId, Indicator = sharedInd }
                }
            };

            var outputRow = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outputs,
                LogFrameRowIndicators = new List<LogFrameRowIndicator>
                {
                    new LogFrameRowIndicator { IndicatorId = sharedInd.IndicatorId, Indicator = sharedInd }
                }
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                LogFrameRows = new List<LogFrameRow> { outcomeRow, outputRow }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            // Both rows have the indicator for their respective objective matrices
            Assert.Single(rollup.Rows[0].Indicators);
            Assert.Single(rollup.Rows[1].Indicators);

            // But project-wide summary KPI deduplicates by IndicatorId
            Assert.Equal(1, rollup.TotalIndicators);
            Assert.Equal(1, rollup.TotalOnTrack);
            Assert.Equal(80.0m, rollup.OverallAchievement);
        }

        [Fact]
        public void ComputeRollup_HierarchyOrder_GoalsBeforeActivities()
        {
            var goalRow = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Goals,
                Description = "High-level Goal"
            };

            var activityRow = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Activities,
                Description = "Ground-level Activity"
            };

            var outputRow = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outputs,
                Description = "Intermediate Output"
            };

            var outcomeRow = new LogFrameRow
            {
                LogFrameRowId = Guid.NewGuid(),
                RowNumber = 1,
                RowType = LogFrameRowType.Outcomes,
                Description = "Medium-term Outcome"
            };

            var logFrame = new LogFrame
            {
                LogFrameId = Guid.NewGuid(),
                // Added out of order
                LogFrameRows = new List<LogFrameRow> { activityRow, outputRow, goalRow, outcomeRow }
            };

            var rollup = LogFrameRollupEngine.ComputeRollup(logFrame);

            // Order must be: Goals -> Outcomes -> Outputs -> Activities
            Assert.Equal(LogFrameRowType.Goals, rollup.Rows[0].RowType);
            Assert.Equal(LogFrameRowType.Outcomes, rollup.Rows[1].RowType);
            Assert.Equal(LogFrameRowType.Outputs, rollup.Rows[2].RowType);
            Assert.Equal(LogFrameRowType.Activities, rollup.Rows[3].RowType);
        }
    }
}
