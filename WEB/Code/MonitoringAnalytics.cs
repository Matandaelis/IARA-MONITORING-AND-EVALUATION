using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// Read-side monitoring computations. Everything is derived on demand from the existing data model:
    /// indicator targets/actuals, logframe v2 tree, budget ledgers, risks, workplan and approval state.
    /// No monitoring metrics are persisted, so results always reflect the latest source data.
    /// </summary>
    public static class MonitoringAnalytics
    {
        private record CoverageStats(
            int Expected,
            int Reported,
            int Approved,
            decimal? Coverage,
            decimal? Timeliness,
            decimal? ApprovalRate);

        // ───────────────────────────── project scope ─────────────────────────────

        /// <summary>
        /// Indicators in scope for a project: explicitly assigned, linked to the project's logframe,
        /// or linked to a component of the project's theory of change. Falls back to the enabled
        /// indicator library when no project links exist yet (legacy projects).
        /// </summary>
        public static async Task<List<Indicator>> LoadProjectIndicatorsAsync(ApplicationDbContext db, Guid projectId)
        {
            var direct = await db.ProjectIndicators.AsNoTracking()
                .Where(o => o.ProjectId == projectId)
                .Select(o => o.IndicatorId)
                .ToListAsync();

            var viaLogFrame = await db.LogFrameNodeIndicators.AsNoTracking()
                .Where(o => o.Node.LogFrame.ProjectId == projectId)
                .Select(o => o.IndicatorId)
                .ToListAsync();

            var viaComponent = await db.ComponentIndicators.AsNoTracking()
                .Where(o => o.Component.TheoryOfChangeComponents.Any(t => t.TheoryOfChange.ProjectId == projectId))
                .Select(o => o.IndicatorId)
                .ToListAsync();

            var ids = direct.Concat(viaLogFrame).Concat(viaComponent).Distinct().ToList();

            var query = db.Indicators.AsNoTracking()
                .Where(o => o.IndicatorStatus == IndicatorStatus.Enabled);

            if (ids.Count > 0)
                query = query.Where(o => ids.Contains(o.IndicatorId));

            return await query
                .Include(o => o.ResponsibleUser)
                .Include(o => o.Data).ThenInclude(o => o.Date)
                .Include(o => o.IndicatorTargets).ThenInclude(o => o.Date)
                .Include(o => o.IndicatorDates).ThenInclude(o => o.Date)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Name)
                .ToListAsync();
        }

        public static async Task<string> GetProjectNameAsync(ApplicationDbContext db, Guid projectId)
        {
            return await db.Projects.AsNoTracking()
                .Where(o => o.ProjectId == projectId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        // ───────────────────────────── indicator performance ─────────────────────────────

        public static MonitoringIndicatorPerformanceDTO ComputePerformance(
            Indicator indicator,
            Guid? dateId,
            IReadOnlyCollection<Date> allDates,
            DateTime? asOfUtc = null)
        {
            if (indicator == null) return null;

            var asOf = (asOfUtc ?? DateTime.UtcNow).Date;
            Date selectedDate = null;

            List<Datum> data;
            List<IndicatorTarget> targets;

            if (dateId.HasValue)
            {
                selectedDate = allDates?.FirstOrDefault(o => o.DateId == dateId.Value);
                data = indicator.Data.Where(o => o.DateId == dateId.Value && o.Value.HasValue).ToList();
                targets = indicator.IndicatorTargets.Where(o => o.DateId == dateId.Value).ToList();
            }
            else
            {
                var latestData = indicator.Data
                    .Where(o => o.Value.HasValue && o.Date != null)
                    .OrderByDescending(o => o.Date.SortOrder)
                    .ThenByDescending(o => o.Date.OpenTo)
                    .FirstOrDefault();

                if (latestData != null)
                {
                    selectedDate = latestData.Date;
                    data = indicator.Data.Where(o => o.DateId == latestData.DateId && o.Value.HasValue).ToList();
                    targets = indicator.IndicatorTargets.Where(o => o.DateId == latestData.DateId).ToList();
                }
                else
                {
                    data = new List<Datum>();
                    targets = new List<IndicatorTarget>();
                }

                if (targets.Count == 0)
                {
                    var latestTarget = indicator.IndicatorTargets
                        .Where(o => o.Date != null)
                        .OrderByDescending(o => o.Date.SortOrder)
                        .ThenByDescending(o => o.Date.OpenTo)
                        .FirstOrDefault();

                    if (latestTarget != null)
                    {
                        targets = new List<IndicatorTarget> { latestTarget };
                        selectedDate ??= latestTarget.Date;
                    }
                }
            }

            decimal? actual = null;
            var values = data.Select(o => o.Value!.Value).ToList();

            if (data.Count > 0)
            {
                if (indicator.DateAggregationType == AggregationType.MostRecent)
                {
                    actual = data.OrderByDescending(o => o.LastSavedDateUtc).First().Value;
                }
                else if (indicator.EntityAggregationType == EntityAggregationType.Average)
                {
                    actual = Math.Round(values.Average(), 4);
                }
                else if (indicator.EntityAggregationType == EntityAggregationType.None)
                {
                    actual = data.OrderByDescending(o => o.LastSavedDateUtc).First().Value;
                }
                else
                {
                    actual = values.Sum();
                }
            }

            decimal? target = null;
            if (targets.Count > 0)
            {
                if (indicator.EntityAggregationType == EntityAggregationType.Average)
                    target = Math.Round(targets.Average(o => o.TargetValue), 4);
                else if (indicator.DateAggregationType == AggregationType.MostRecent || indicator.EntityAggregationType == EntityAggregationType.None)
                    target = targets.OrderByDescending(o => o.UpdatedAt).First().TargetValue;
                else
                    target = targets.Sum(o => o.TargetValue);
            }

            var baseline = targets.Select(o => o.BaselineValue).FirstOrDefault(o => o.HasValue) ?? indicator.Baseline;

            var variance = VarianceEngine.CalculateVariance(actual, target, indicator.Direction, baseline);
            var rag = MonitoringMetrics.ClassifyRag(
                variance?.PercentageAchievement,
                indicator.GreenThreshold,
                indicator.AmberThreshold);

            var coverage = ComputeCoverageStats(indicator, allDates, asOf);

            return new MonitoringIndicatorPerformanceDTO
            {
                IndicatorId = indicator.IndicatorId,
                Code = indicator.Code,
                Name = indicator.Name,
                Units = indicator.Units,
                MonitoringType = indicator.MonitoringType,
                MonitoringTypeLabel = indicator.MonitoringType.Label(),
                Direction = indicator.Direction,
                IsCalculated = indicator.IndicatorType == IndicatorType.Calculated,
                Baseline = baseline,
                Target = target,
                Actual = actual,
                PercentageAchievement = variance?.PercentageAchievement,
                AbsoluteVariance = variance?.AbsoluteVariance,
                StatusBadge = variance?.StatusBadge ?? VarianceEngine.StatusNotApplicable,
                Rag = rag,
                GreenThreshold = indicator.GreenThreshold,
                AmberThreshold = indicator.AmberThreshold,
                DataPoints = data.Count,
                ApprovedDataPoints = data.Count(o => o.Approved),
                Coverage = coverage.Coverage,
                Timeliness = coverage.Timeliness,
                ApprovalRate = coverage.ApprovalRate,
                LatestDateId = selectedDate?.DateId,
                LatestPeriod = selectedDate?.Name,
                ResponsibleUserName = indicator.ResponsibleUser?.FullName
            };
        }

        private static CoverageStats ComputeCoverageStats(
            Indicator indicator,
            IReadOnlyCollection<Date> allDates,
            DateTime asOf)
        {
            var definedPeriods = indicator.IndicatorDates
                .Where(o => o.Date != null && o.Date.OpenFrom.Date <= asOf)
                .Select(o => o.Date)
                .ToList();

            int expected;
            if (definedPeriods.Count > 0)
            {
                expected = definedPeriods.Count;
            }
            else if (allDates != null)
            {
                // No explicit reporting calendar: assume the last 12 months at the indicator's frequency.
                var horizon = asOf.AddYears(-1);
                expected = allDates.Count(o =>
                    o.DateType == indicator.Frequency &&
                    o.OpenFrom.Date >= horizon &&
                    o.OpenFrom.Date <= asOf);
            }
            else
            {
                expected = 0;
            }

            var periodData = indicator.Data
                .Where(o => o.Value.HasValue && o.Date != null && o.Date.OpenFrom.Date <= asOf)
                .ToList();

            var reportedDateIds = periodData.Select(o => o.DateId).Distinct().ToList();
            var approvedDateIds = periodData.Where(o => o.Approved).Select(o => o.DateId).Distinct().ToList();
            var timelyDateIds = periodData
                .Where(o => o.LastSavedDateUtc.Date <= o.Date.OpenTo.Date)
                .Select(o => o.DateId)
                .Distinct()
                .ToList();

            decimal? coverage = expected > 0
                ? Math.Min(100m, Math.Round((decimal)reportedDateIds.Count / expected * 100m, 1))
                : (decimal?)null;

            return new CoverageStats(
                expected,
                reportedDateIds.Count,
                approvedDateIds.Count,
                coverage,
                MonitoringMetrics.Percent(timelyDateIds.Count, reportedDateIds.Count),
                MonitoringMetrics.Percent(approvedDateIds.Count, reportedDateIds.Count));
        }

        // ───────────────────────────── dashboard summary ─────────────────────────────

        public static async Task<MonitoringSummaryDTO> ComputeSummaryAsync(
            ApplicationDbContext db,
            Guid projectId,
            Guid? dateId = null)
        {
            var asOf = DateTime.UtcNow.Date;
            var indicators = await LoadProjectIndicatorsAsync(db, projectId);
            var allDates = await db.Dates.AsNoTracking().ToListAsync();
            var selectedDate = dateId.HasValue ? allDates.FirstOrDefault(o => o.DateId == dateId.Value) : null;

            var performance = indicators
                .Select(o => ComputePerformance(o, dateId, allDates, asOf))
                .Where(o => o != null)
                .ToList();

            var assessed = performance.Where(o => o.Rag != RagStatus.NotAssessed).ToList();
            var overallAchievement = MonitoringMetrics.Average(assessed.Select(o => o.PercentageAchievement));
            var overallRag = MonitoringMetrics.ClassifyRag(overallAchievement);
            var coverage = MonitoringMetrics.Average(performance.Select(o => o.Coverage));
            var timeliness = MonitoringMetrics.Average(performance.Select(o => o.Timeliness));
            var approvalRate = MonitoringMetrics.Average(performance.Select(o => o.ApprovalRate));

            var financial = await ComputeFinancialAsync(db, projectId, dateId);

            var workplan = await db.Tasks.AsNoTracking()
                .Where(o => o.Milestone.ProjectId == projectId)
                .Select(o => (decimal?)o.PercentCompleted)
                .ToListAsync();
            var workplanCompletion = MonitoringMetrics.Average(workplan);

            // No budget means there is no financial delivery signal; exclude that component
            // from the composite rather than scoring it as zero utilisation.
            var financialUtilization = financial.Planned > 0m ? financial.Utilization : (decimal?)null;

            var composite = MonitoringMetrics.ComputeComposite(
                overallAchievement,
                financialUtilization,
                workplanCompletion,
                approvalRate);

            var summary = new MonitoringSummaryDTO
            {
                ProjectId = projectId,
                ProjectName = await GetProjectNameAsync(db, projectId),
                DateId = dateId,
                PeriodLabel = selectedDate?.Name ?? "Latest reported period",
                GeneratedOnUtc = DateTime.UtcNow,
                TotalIndicators = performance.Count,
                AssessedIndicators = assessed.Count,
                Green = performance.Count(o => o.Rag == RagStatus.Green),
                Amber = performance.Count(o => o.Rag == RagStatus.Amber),
                Red = performance.Count(o => o.Rag == RagStatus.Red),
                NoData = performance.Count(o => o.Rag == RagStatus.NotAssessed),
                OverallAchievement = overallAchievement,
                OverallStatus = overallRag.Label(),
                OverallRag = overallRag,
                Coverage = coverage,
                Timeliness = timeliness,
                ApprovalRate = approvalRate,
                CompositeScore = composite.CompositeScore,
                ResultsScore = composite.ResultsScore,
                FinancialScore = composite.FinancialScore,
                WorkplanScore = composite.WorkplanScore,
                DataQualityScore = composite.DataQualityScore,
                Financial = financial,
                Underperforming = performance
                    .Where(o => o.Rag == RagStatus.Red || o.Rag == RagStatus.Amber)
                    .OrderBy(o => o.Rag == RagStatus.Red ? 0 : 1)
                    .ThenBy(o => o.PercentageAchievement ?? 0)
                    .Take(10)
                    .ToList()
            };

            foreach (var type in Enum.GetValues<MonitoringType>())
            {
                var rows = performance.Where(o => o.MonitoringType == type).ToList();
                summary.ByType.Add(new MonitoringTypeSummaryDTO
                {
                    MonitoringType = type,
                    Label = type.Label(),
                    Total = rows.Count,
                    Green = rows.Count(o => o.Rag == RagStatus.Green),
                    Amber = rows.Count(o => o.Rag == RagStatus.Amber),
                    Red = rows.Count(o => o.Rag == RagStatus.Red),
                    NoData = rows.Count(o => o.Rag == RagStatus.NotAssessed),
                    AverageAchievement = MonitoringMetrics.Average(rows.Select(o => o.PercentageAchievement))
                });
            }

            return summary;
        }

        // ───────────────────────────── monitoring matrix ─────────────────────────────

        public static async Task<MonitoringMatrixDTO> ComputeMatrixAsync(
            ApplicationDbContext db,
            Guid projectId,
            Guid? dateId = null)
        {
            var asOf = DateTime.UtcNow.Date;
            var indicators = await LoadProjectIndicatorsAsync(db, projectId);
            var allDates = await db.Dates.AsNoTracking().ToListAsync();
            var selectedDate = dateId.HasValue ? allDates.FirstOrDefault(o => o.DateId == dateId.Value) : null;

            // Periods that actually carry monitoring data or targets, most recent last.
            var periodIds = indicators
                .SelectMany(o => o.Data.Where(d => d.Value.HasValue).Select(d => d.DateId)
                    .Concat(o.IndicatorTargets.Select(t => t.DateId)))
                .Distinct()
                .ToList();

            var periods = allDates
                .Where(o => periodIds.Contains(o.DateId) && o.OpenFrom.Date <= asOf)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.OpenFrom)
                .TakeLast(8)
                .ToList();

            var result = new MonitoringMatrixDTO
            {
                ProjectId = projectId,
                ProjectName = await GetProjectNameAsync(db, projectId),
                DateId = dateId,
                PeriodLabel = selectedDate?.Name ?? "Latest reported period"
            };

            result.Periods = periods.Select(o => new MonitoringMatrixPeriodDTO
            {
                DateId = o.DateId,
                Label = o.Name,
                OpenFrom = o.OpenFrom,
                OpenTo = o.OpenTo
            }).ToList();

            foreach (var indicator in indicators)
            {
                var latest = ComputePerformance(indicator, null, allDates, asOf);
                var row = new MonitoringMatrixRowDTO
                {
                    IndicatorId = indicator.IndicatorId,
                    Code = indicator.Code,
                    Name = indicator.Name,
                    Units = indicator.Units,
                    MonitoringType = indicator.MonitoringType,
                    MonitoringTypeLabel = indicator.MonitoringType.Label(),
                    Direction = indicator.Direction,
                    Baseline = latest?.Baseline,
                    Coverage = latest?.Coverage,
                    ResponsibleUserName = indicator.ResponsibleUser?.FullName
                };

                foreach (var period in periods)
                {
                    var cell = ComputePerformance(indicator, period.DateId, allDates, asOf);
                    row.Cells.Add(new MonitoringMatrixCellDTO
                    {
                        DateId = period.DateId,
                        PeriodLabel = period.Name,
                        Target = cell?.Target,
                        Actual = cell?.Actual,
                        PercentageAchievement = cell?.PercentageAchievement,
                        Rag = cell?.Rag ?? RagStatus.NotAssessed
                    });
                }

                result.Rows.Add(row);
            }

            return result;
        }

        // ───────────────────────────── results tree roll-up ─────────────────────────────

        public static async Task<MonitoringResultsDTO> ComputeResultsAsync(
            ApplicationDbContext db,
            Guid logFrameId,
            Guid? dateId = null)
        {
            var asOf = DateTime.UtcNow.Date;
            var allDates = await db.Dates.AsNoTracking().ToListAsync();
            var selectedDate = dateId.HasValue ? allDates.FirstOrDefault(o => o.DateId == dateId.Value) : null;

            var logFrame = await db.LogFrames.AsNoTracking()
                .Where(o => o.LogFrameId == logFrameId)
                .FirstOrDefaultAsync();

            if (logFrame == null) return null;

            var nodes = await db.LogFrameNodes.AsNoTracking()
                .Where(o => o.LogFrameId == logFrameId)
                .Include(o => o.Level)
                .Include(o => o.Indicators).ThenInclude(o => o.Indicator).ThenInclude(o => o.Data).ThenInclude(o => o.Date)
                .Include(o => o.Indicators).ThenInclude(o => o.Indicator).ThenInclude(o => o.IndicatorTargets).ThenInclude(o => o.Date)
                .Include(o => o.Indicators).ThenInclude(o => o.Indicator).ThenInclude(o => o.IndicatorDates).ThenInclude(o => o.Date)
                .Include(o => o.Indicators).ThenInclude(o => o.Indicator).ThenInclude(o => o.ResponsibleUser)
                .OrderBy(o => o.SortOrder)
                .ToListAsync();

            var risks = logFrame.ProjectId.HasValue
                ? await db.RiskAssumptions.AsNoTracking()
                    .Where(o => o.ProjectId == logFrame.ProjectId.Value)
                    .ToListAsync()
                : new List<RiskAssumption>();

            var childrenByParent = nodes
                .GroupBy(o => o.ParentNodeId)
                .ToDictionary(o => o.Key, o => o.OrderBy(n => n.SortOrder).ToList());

            MonitoringResultNodeDTO Build(LogFrameNode node, int depth)
            {
                var dto = new MonitoringResultNodeDTO
                {
                    LogFrameNodeId = node.LogFrameNodeId,
                    ParentNodeId = node.ParentNodeId,
                    Code = node.Code,
                    Title = node.Title,
                    LevelName = node.Level?.Name,
                    Depth = depth,
                    SortOrder = node.SortOrder,
                    RisksAndAssumptions = node.RisksAndAssumptions,
                    OpenRisks = risks.Count(o =>
                        o.LogFrameNodeId == node.LogFrameNodeId &&
                        o.Status != RiskStatus.Closed &&
                        o.Status != RiskStatus.Mitigated)
                };

                var weighted = new List<(decimal? Value, decimal Weight)>();
                foreach (var link in node.Indicators.OrderBy(o => o.SortOrder))
                {
                    var perf = ComputePerformance(link.Indicator, dateId, allDates, asOf);
                    if (perf == null) continue;
                    dto.Indicators.Add(perf);
                    weighted.Add((perf.PercentageAchievement, link.Weight <= 0 ? 1m : link.Weight));
                }

                dto.IndicatorCount = dto.Indicators.Count;
                dto.Green = dto.Indicators.Count(o => o.Rag == RagStatus.Green);
                dto.Amber = dto.Indicators.Count(o => o.Rag == RagStatus.Amber);
                dto.Red = dto.Indicators.Count(o => o.Rag == RagStatus.Red);
                dto.NoData = dto.Indicators.Count(o => o.Rag == RagStatus.NotAssessed);
                dto.Coverage = MonitoringMetrics.Average(dto.Indicators.Select(o => o.Coverage));

                if (childrenByParent.TryGetValue(node.LogFrameNodeId, out var children))
                {
                    foreach (var child in children)
                    {
                        dto.Children.Add(Build(child, depth + 1));
                    }
                }

                // A node with direct indicators scores from them; otherwise it rolls up its children.
                if (weighted.Count > 0)
                {
                    dto.WeightedAchievement = MonitoringMetrics.WeightedAverage(weighted);
                }
                else if (dto.Children.Count > 0)
                {
                    dto.WeightedAchievement = MonitoringMetrics.WeightedAverage(
                        dto.Children.Select(o => (o.WeightedAchievement, 1m)));
                    dto.Coverage ??= MonitoringMetrics.Average(dto.Children.Select(o => o.Coverage));
                }

                dto.Rag = MonitoringMetrics.ClassifyRag(dto.WeightedAchievement);
                return dto;
            }

            var roots = new List<MonitoringResultNodeDTO>();
            foreach (var node in nodes.Where(o => o.ParentNodeId == null))
                roots.Add(Build(node, 0));

            var overall = MonitoringMetrics.WeightedAverage(roots.Select(o => (o.WeightedAchievement, 1m)));

            return new MonitoringResultsDTO
            {
                LogFrameId = logFrame.LogFrameId,
                LogFrameName = logFrame.Name,
                DateId = dateId,
                PeriodLabel = selectedDate?.Name ?? "Latest reported period",
                OverallAchievement = overall,
                OverallRag = MonitoringMetrics.ClassifyRag(overall),
                Coverage = MonitoringMetrics.Average(roots.Select(o => o.Coverage)),
                Nodes = roots
            };
        }

        // ───────────────────────────── coverage & timeliness ─────────────────────────────

        public static async Task<MonitoringCoverageDTO> ComputeCoverageAsync(
            ApplicationDbContext db,
            Guid projectId)
        {
            var asOf = DateTime.UtcNow.Date;
            var indicators = await LoadProjectIndicatorsAsync(db, projectId);
            var allDates = await db.Dates.AsNoTracking().ToListAsync();

            var result = new MonitoringCoverageDTO
            {
                ProjectId = projectId,
                ProjectName = await GetProjectNameAsync(db, projectId)
            };

            foreach (var indicator in indicators)
            {
                var stats = ComputeCoverageStats(indicator, allDates, asOf);
                result.Rows.Add(new MonitoringCoverageRowDTO
                {
                    IndicatorId = indicator.IndicatorId,
                    Code = indicator.Code,
                    Name = indicator.Name,
                    MonitoringType = indicator.MonitoringType,
                    MonitoringTypeLabel = indicator.MonitoringType.Label(),
                    Frequency = indicator.Frequency,
                    Expected = stats.Expected,
                    Reported = stats.Reported,
                    Approved = stats.Approved,
                    Coverage = stats.Coverage,
                    Timeliness = stats.Timeliness,
                    ApprovalRate = stats.ApprovalRate,
                    ResponsibleUserName = indicator.ResponsibleUser?.FullName
                });
            }

            result.OverallCoverage = MonitoringMetrics.Average(result.Rows.Select(o => o.Coverage));
            result.OverallTimeliness = MonitoringMetrics.Average(result.Rows.Select(o => o.Timeliness));
            result.OverallApprovalRate = MonitoringMetrics.Average(result.Rows.Select(o => o.ApprovalRate));
            return result;
        }

        // ───────────────────────────── financial monitoring ─────────────────────────────

        public static async Task<MonitoringFinancialDTO> ComputeFinancialAsync(
            ApplicationDbContext db,
            Guid projectId,
            Guid? dateId = null)
        {
            var dbSettings = db.GetDbSettings();
            var baseCurrency = BudgetAnalytics.ResolveBaseCurrency(dbSettings);

            var summary = await BudgetAnalytics.ComputeSummaryAsync(db, projectId, baseCurrency, true);

            var result = new MonitoringFinancialDTO
            {
                ProjectId = projectId,
                BaseCurrency = summary.BaseCurrency,
                Rows = summary.Rows,
                MissingRates = summary.MissingRates
            };

            result.Planned = summary.Rows.Sum(o => o.Planned);
            result.Committed = summary.Rows.Sum(o => o.Committed);
            result.Disbursed = summary.Rows.Sum(o => o.Disbursed);
            result.Actual = summary.Rows.Sum(o => o.Actual);
            result.Variance = result.Planned - result.Actual;
            result.Utilization = MonitoringMetrics.Utilization(result.Planned, result.Actual) ?? 0m;
            result.CommitmentRate = MonitoringMetrics.Utilization(result.Planned, result.Committed) ?? 0m;

            result.BurnSeries = await BudgetAnalytics.ComputeBurnSeriesAsync(db, projectId, baseCurrency);

            // Cost per output for indicators explicitly linked to budget lines.
            var lines = await db.BudgetLines.AsNoTracking()
                .Where(o => o.ProjectId == projectId && !o.IsDeleted && o.IndicatorId != null)
                .Select(o => new { o.IndicatorId, o.ActualAmount, o.Currency })
                .ToListAsync();

            if (lines.Count > 0)
            {
                var indicators = await LoadProjectIndicatorsAsync(db, projectId);
                var allDates = await db.Dates.AsNoTracking().ToListAsync();
                var asOf = DateTime.UtcNow.Date;

                var byIndicator = lines.GroupBy(o => o.IndicatorId!.Value);
                foreach (var group in byIndicator)
                {
                    var indicator = indicators.FirstOrDefault(o => o.IndicatorId == group.Key);
                    if (indicator == null) continue;

                    decimal expended = 0m;
                    foreach (var line in group)
                    {
                        var currency = string.IsNullOrWhiteSpace(line.Currency) ? baseCurrency : line.Currency;
                        var factor = 1m;

                        if (!string.Equals(currency, baseCurrency, StringComparison.OrdinalIgnoreCase))
                        {
                            var rate = await BudgetAnalytics.GetRateAsync(db, currency, baseCurrency, asOf);
                            if (!rate.HasValue) continue;
                            factor = rate.Value;
                        }

                        expended += line.ActualAmount * factor;
                    }

                    var perf = ComputePerformance(indicator, dateId, allDates, asOf);
                    var output = perf?.Actual;

                    result.CostPerOutput.Add(new MonitoringCostPerOutputDTO
                    {
                        IndicatorId = indicator.IndicatorId,
                        Code = indicator.Code,
                        Name = indicator.Name,
                        Expended = Math.Round(expended, 2),
                        OutputValue = output,
                        CostPerOutput = output.HasValue && output.Value != 0m
                            ? Math.Round(expended / output.Value, 4)
                            : (decimal?)null,
                        Currency = baseCurrency
                    });
                }
            }

            return result;
        }
    }
}
