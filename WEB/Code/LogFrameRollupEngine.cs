using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    public static class LogFrameRollupEngine
    {
        public static LogFrameRollupDTO ComputeRollup(LogFrame logFrame)
        {
            if (logFrame == null)
            {
                throw new ArgumentNullException(nameof(logFrame));
            }

            var rollup = new LogFrameRollupDTO
            {
                LogFrameId = logFrame.LogFrameId,
                LogFrameName = logFrame.Name ?? string.Empty,
                TotalRows = logFrame.LogFrameRows?.Count ?? 0
            };

            if (logFrame.LogFrameRows == null || logFrame.LogFrameRows.Count == 0)
            {
                return rollup;
            }

            // Results Framework hierarchy: Goals (top) -> Outcomes -> Outputs -> Activities (bottom)
            var sortedRows = logFrame.LogFrameRows
                .OrderByDescending(r => r.RowType)
                .ThenBy(r => r.RowNumber)
                .ToList();

            var allIndicatorRollups = new List<LogFrameIndicatorRollupDTO>();

            foreach (var row in sortedRows)
            {
                var rowRollup = new LogFrameRowRollupDTO
                {
                    LogFrameRowId = row.LogFrameRowId,
                    LogFrameId = row.LogFrameId,
                    RowNumber = row.RowNumber,
                    RowType = row.RowType,
                    RowTypeName = GetRowTypeName(row.RowType),
                    Description = row.Description ?? string.Empty,
                    MeansOfVerification = row.MeansOfVerification ?? string.Empty,
                    RisksAndAssumptions = row.RisksAndAssumptions ?? string.Empty,
                    TotalIndicators = 0
                };

                if (row.LogFrameRowIndicators != null && row.LogFrameRowIndicators.Count > 0)
                {
                    foreach (var rowIndicator in row.LogFrameRowIndicators)
                    {
                        var indicator = rowIndicator.Indicator;
                        if (indicator == null) continue;

                        var indRollup = ComputeIndicatorRollup(indicator);
                        indRollup.Weight = rowIndicator.Weight > 0 ? rowIndicator.Weight : 1.0m;
                        rowRollup.Indicators.Add(indRollup);
                        allIndicatorRollups.Add(indRollup);

                        // Accumulate row status counts
                        switch (indRollup.StatusBadge)
                        {
                            case VarianceEngine.StatusOnTrack:
                                rowRollup.TotalOnTrack++;
                                break;
                            case VarianceEngine.StatusUnderperforming:
                                rowRollup.TotalUnderperforming++;
                                break;
                            case VarianceEngine.StatusExceeding:
                                rowRollup.TotalExceeding++;
                                break;
                            default:
                                rowRollup.TotalNoData++;
                                break;
                        }
                    }

                    rowRollup.TotalIndicators = rowRollup.Indicators.Count;

                    // Compute row-level weighted average achievement
                    var rowMeasurable = rowRollup.Indicators.Where(i => i.PercentageAchievement.HasValue).ToList();
                    if (rowMeasurable.Count > 0)
                    {
                        decimal totalWeight = rowMeasurable.Sum(i => i.Weight > 0 ? i.Weight : 1.0m);
                        decimal weightedSum = rowMeasurable.Sum(i => i.PercentageAchievement.Value * (i.Weight > 0 ? i.Weight : 1.0m));
                        rowRollup.AverageAchievement = Math.Round(weightedSum / totalWeight, 2);
                        rowRollup.RowStatus = ClassifyStatus(rowRollup.AverageAchievement.Value);
                    }
                    else
                    {
                        rowRollup.AverageAchievement = null;
                        rowRollup.RowStatus = VarianceEngine.StatusNotApplicable;
                    }
                }

                rollup.Rows.Add(rowRollup);
            }

            // Project-wide roll-up: deduplicate by IndicatorId to ensure indicators shared across objectives are not double counted in executive KPIs
            var distinctIndicators = allIndicatorRollups
                .GroupBy(i => i.IndicatorId)
                .Select(g => g.First())
                .ToList();

            rollup.TotalIndicators = distinctIndicators.Count;
            rollup.TotalOnTrack = distinctIndicators.Count(i => i.StatusBadge == VarianceEngine.StatusOnTrack);
            rollup.TotalUnderperforming = distinctIndicators.Count(i => i.StatusBadge == VarianceEngine.StatusUnderperforming);
            rollup.TotalExceeding = distinctIndicators.Count(i => i.StatusBadge == VarianceEngine.StatusExceeding);
            rollup.TotalNoData = distinctIndicators.Count(i => i.StatusBadge == VarianceEngine.StatusNotApplicable);

            var projectMeasurable = distinctIndicators.Where(i => i.PercentageAchievement.HasValue).ToList();
            if (projectMeasurable.Count > 0)
            {
                decimal totalWeight = projectMeasurable.Sum(i => i.Weight > 0 ? i.Weight : 1.0m);
                decimal weightedSum = projectMeasurable.Sum(i => i.PercentageAchievement.Value * (i.Weight > 0 ? i.Weight : 1.0m));
                rollup.OverallAchievement = Math.Round(weightedSum / totalWeight, 2);
                rollup.OverallStatus = ClassifyStatus(rollup.OverallAchievement.Value);
            }
            else
            {
                rollup.OverallAchievement = null;
                rollup.OverallStatus = VarianceEngine.StatusNotApplicable;
            }

            return rollup;
        }

        public static LogFrameIndicatorRollupDTO ComputeIndicatorRollup(Indicator indicator)
        {
            if (indicator == null) throw new ArgumentNullException(nameof(indicator));

            // Baseline from indicator or the chronologically earliest target with a baseline
            decimal? baseline = indicator.Baseline ?? indicator.IndicatorTargets?
                .Where(t => t.BaselineValue.HasValue)
                .OrderBy(t => t.Date?.SortOrder ?? 0)
                .ThenBy(t => t.Date?.OpenFrom ?? DateTime.MinValue)
                .Select(t => t.BaselineValue)
                .FirstOrDefault();

            decimal? target = null;
            decimal? actual = null;

            var validData = indicator.Data?.Where(d => d.Value.HasValue).ToList();

            if (indicator.DateAggregationType == AggregationType.MostRecent)
            {
                // Rate/Percentage/Prevalence indicators: use most recent period's observation and target (do NOT sum across periods)
                if (validData != null && validData.Count > 0)
                {
                    // Filter out intermediate SQL roll-ups if unaggregated data is present
                    var unaggregated = validData.Where(d => !d.Aggregated).ToList();
                    var dataPool = unaggregated.Count > 0 ? unaggregated : validData;

                    var mostRecentDatum = dataPool
                        .OrderByDescending(d => d.Date?.SortOrder ?? 0)
                        .ThenByDescending(d => d.Date?.OpenTo ?? DateTime.MinValue)
                        .ThenByDescending(d => d.LastSavedDateUtc)
                        .FirstOrDefault();

                    actual = mostRecentDatum?.Value;

                    if (indicator.IndicatorTargets != null && indicator.IndicatorTargets.Count > 0)
                    {
                        if (mostRecentDatum?.DateId != null && mostRecentDatum.DateId != Guid.Empty)
                        {
                            var matchingTarget = indicator.IndicatorTargets.FirstOrDefault(t => t.DateId == mostRecentDatum.DateId);
                            if (matchingTarget != null)
                            {
                                target = matchingTarget.TargetValue;
                            }
                        }

                        if (!target.HasValue)
                        {
                            var latestTarget = indicator.IndicatorTargets
                                .OrderByDescending(t => t.Date?.SortOrder ?? 0)
                                .ThenByDescending(t => t.Date?.OpenTo ?? DateTime.MinValue)
                                .FirstOrDefault();
                            target = latestTarget?.TargetValue;
                        }
                    }
                }
                else
                {
                    actual = null;
                    if (indicator.IndicatorTargets != null && indicator.IndicatorTargets.Count > 0)
                    {
                        var latestTarget = indicator.IndicatorTargets
                            .OrderByDescending(t => t.Date?.SortOrder ?? 0)
                            .ThenByDescending(t => t.Date?.OpenTo ?? DateTime.MinValue)
                            .FirstOrDefault();
                        target = latestTarget?.TargetValue;
                    }
                }
            }
            else
            {
                // Sum indicators (cumulative events, counts): sum unaggregated raw observations
                if (validData != null && validData.Count > 0)
                {
                    var unaggregated = validData.Where(d => !d.Aggregated).ToList();
                    var dataPool = unaggregated.Count > 0 ? unaggregated : validData;
                    actual = dataPool.Sum(d => d.Value.Value);
                }
                else
                {
                    actual = null;
                }

                if (indicator.IndicatorTargets != null && indicator.IndicatorTargets.Count > 0)
                {
                    target = indicator.IndicatorTargets.Sum(t => t.TargetValue);
                }
            }

            var variance = VarianceEngine.CalculateVariance(actual, target, indicator.Direction, baseline);

            return new LogFrameIndicatorRollupDTO
            {
                IndicatorId = indicator.IndicatorId,
                Code = indicator.Code ?? string.Empty,
                Name = indicator.Name ?? string.Empty,
                Units = indicator.Units ?? string.Empty,
                Direction = indicator.Direction,
                Baseline = baseline,
                Target = target,
                Actual = actual,
                PercentageAchievement = variance.PercentageAchievement,
                StatusBadge = variance.StatusBadge,
                AbsoluteVariance = variance.AbsoluteVariance,
                Weight = 1.0m
            };
        }

        public static string ClassifyStatus(decimal achievement)
        {
            if (achievement < 75m) return VarianceEngine.StatusUnderperforming;
            if (achievement <= 110m) return VarianceEngine.StatusOnTrack;
            return VarianceEngine.StatusExceeding;
        }

        public static string GetRowTypeName(LogFrameRowType rowType)
        {
            return rowType switch
            {
                LogFrameRowType.Goals => "Goal",
                LogFrameRowType.Outcomes => "Outcome",
                LogFrameRowType.Outputs => "Output",
                LogFrameRowType.Activities => "Activity",
                _ => rowType.ToString()
            };
        }

        public static byte[] GenerateDonorCsv(LogFrameRollupDTO rollup)
        {
            if (rollup == null) throw new ArgumentNullException(nameof(rollup));

            var records = new List<DonorLogFrameRowCsvRecord>();

            foreach (var row in rollup.Rows)
            {
                if (row.Indicators == null || row.Indicators.Count == 0)
                {
                    records.Add(new DonorLogFrameRowCsvRecord
                    {
                        ObjectiveLevel = row.RowTypeName,
                        RowNumber = row.RowNumber,
                        Description = row.Description,
                        IndicatorCode = string.Empty,
                        IndicatorName = string.Empty,
                        Units = string.Empty,
                        Baseline = null,
                        Target = null,
                        Actual = null,
                        AbsoluteVariance = null,
                        PercentageAchievement = null,
                        StatusBadge = row.RowStatus,
                        MeansOfVerification = row.MeansOfVerification,
                        RisksAndAssumptions = row.RisksAndAssumptions
                    });
                }
                else
                {
                    foreach (var ind in row.Indicators)
                    {
                        records.Add(new DonorLogFrameRowCsvRecord
                        {
                            ObjectiveLevel = row.RowTypeName,
                            RowNumber = row.RowNumber,
                            Description = row.Description,
                            IndicatorCode = ind.Code,
                            IndicatorName = ind.Name,
                            Units = ind.Units,
                            Baseline = ind.Baseline,
                            Target = ind.Target,
                            Actual = ind.Actual,
                            AbsoluteVariance = ind.AbsoluteVariance,
                            PercentageAchievement = ind.PercentageAchievement,
                            StatusBadge = ind.StatusBadge,
                            MeansOfVerification = row.MeansOfVerification,
                            RisksAndAssumptions = row.RisksAndAssumptions
                        });
                    }
                }
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                TrimOptions = TrimOptions.Trim
            };

            using var memoryStream = new MemoryStream();
            // Emit UTF-8 BOM so Microsoft Excel correctly parses international donor characters
            using (var streamWriter = new StreamWriter(memoryStream, new System.Text.UTF8Encoding(true)))
            using (var csv = new CsvWriter(streamWriter, config))
            {
                csv.WriteRecords(records);
                csv.Flush();
                streamWriter.Flush();
            }

            return memoryStream.ToArray();
        }

        public class DonorLogFrameRowCsvRecord
        {
            public string ObjectiveLevel { get; set; }
            public int RowNumber { get; set; }
            public string Description { get; set; }
            public string IndicatorCode { get; set; }
            public string IndicatorName { get; set; }
            public string Units { get; set; }
            public decimal? Baseline { get; set; }
            public decimal? Target { get; set; }
            public decimal? Actual { get; set; }
            public decimal? AbsoluteVariance { get; set; }
            public decimal? PercentageAchievement { get; set; }
            public string StatusBadge { get; set; }
            public string MeansOfVerification { get; set; }
            public string RisksAndAssumptions { get; set; }
        }
    }
}
