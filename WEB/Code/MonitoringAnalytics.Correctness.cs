using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    public static partial class MonitoringAnalytics
    {
        private sealed record ApprovedCoverageResult(
            int DataPoints,
            int ApprovedDataPoints,
            int RejectedDataPoints,
            decimal? Coverage,
            decimal? ApprovalRate);

        /// <summary>
        /// Adds approval-state safeguards to monitoring calculations.
        /// Explicitly approved data is the only data eligible for official achievement reporting.
        /// Live/unapproved data remains visible only as a separate draft/status value.
        /// </summary>
        public static async Task<MonitoringIndicatorPerformanceDTO> ComputeApprovedPerformanceAsync(
            ApplicationDbContext db,
            Indicator indicator,
            Guid? dateId,
            IReadOnlyCollection<Date> allDates,
            DateTime? asOfUtc = null)
        {
            if (indicator == null)
                return null;

            var live = ComputePerformance(indicator, dateId, allDates, asOfUtc);
            if (live == null)
                return null;

            var approvedData = indicator.Data
                .Where(d => d.Value.HasValue && d.Approved &&
                    (!dateId.HasValue || d.DateId == dateId.Value))
                .ToList();

            decimal? approvedActual = null;
            if (approvedData.Count > 0)
            {
                var values = approvedData.Select(d => d.Value!.Value).ToList();
                approvedActual = indicator.EntityAggregationType == EntityAggregationType.Average
                    ? Math.Round(values.Average(), 4)
                    : values.Sum();
            }

            var target = live.Target;
            var baseline = live.Baseline;
            var approvedVariance = VarianceEngine.CalculateVariance(
                approvedActual,
                target,
                indicator.Direction,
                baseline);

            var result = new MonitoringIndicatorPerformanceDTO
            {
                IndicatorId = live.IndicatorId,
                Code = live.Code,
                Name = live.Name,
                Units = live.Units,
                MonitoringType = live.MonitoringType,
                MonitoringTypeLabel = live.MonitoringTypeLabel,
                Direction = live.Direction,
                IsCalculated = live.IsCalculated,
                Baseline = live.Baseline,
                Target = live.Target,
                Actual = approvedActual,
                PercentageAchievement = approvedVariance.PercentageAchievement,
                AbsoluteVariance = approvedVariance.AbsoluteVariance,
                StatusBadge = approvedVariance.StatusBadge,
                Rag = approvedVariance.PercentageAchievement.HasValue
                    ? MonitoringMetrics.ClassifyRag(
                        approvedVariance.PercentageAchievement,
                        indicator.GreenThreshold,
                        indicator.AmberThreshold)
                    : RagStatus.NotAssessed,
                GreenThreshold = live.GreenThreshold,
                AmberThreshold = live.AmberThreshold,
                DataPoints = live.DataPoints,
                ApprovedDataPoints = approvedData.Count,
                Coverage = live.Coverage,
                Timeliness = live.Timeliness,
                ApprovalRate = live.DataPoints > 0
                    ? MonitoringMetrics.Percent(approvedData.Count, live.DataPoints)
                    : null,
                LatestDateId = live.LatestDateId,
                LatestPeriod = live.LatestPeriod,
                ResponsibleUserName = live.ResponsibleUserName
            };

            return result;
        }

        /// <summary>
        /// Detects whether selected monitoring output mixes approved and live values.
        /// Controllers and exports should use this as a presentation warning, not hide the warning.
        /// </summary>
        public static async Task<(bool IsComplete, string Warning)> ValidateOfficialResultScopeAsync(
            ApplicationDbContext db,
            Guid projectId,
            Guid? dateId)
        {
            var indicators = await LoadProjectIndicatorsAsync(db, projectId);
            if (indicators.Count == 0)
                return (false, "No indicators are in scope for this project.");

            var withoutTargets = indicators.Count(i =>
                i.IndicatorTargets.Any(t => !dateId.HasValue || t.DateId == dateId.Value) == false);
            if (withoutTargets > 0)
                return (false, $"{withoutTargets} indicator(s) have no target for the selected period.");

            var liveWithoutApproval = indicators.Sum(i => i.Data.Count(d =>
                d.Value.HasValue &&
                (!dateId.HasValue || d.DateId == dateId.Value) &&
                d.Approved == false));

            if (liveWithoutApproval > 0)
                return (false, $"{liveWithoutApproval} live value(s) are not approved. Official reporting should not include them.");

            return (true, null);
        }

        /// <summary>
        /// Reconciles a value with its disaggregated values and blocks official use when it differs.
        /// </summary>
        public static async Task<(bool IsValid, decimal? Total, decimal? DisaggregatedTotal, decimal? Difference)> ReconcileDisaggregationAsync(
            ApplicationDbContext db,
            Guid indicatorId,
            Guid entityId,
            Guid dateId,
            decimal tolerance = 0.0001m)
        {
            var datum = await db.Data.AsNoTracking().FirstOrDefaultAsync(d =>
                d.IndicatorId == indicatorId && d.EntityId == entityId && d.DateId == dateId);

            if (datum?.Value == null)
                return (false, null, null, null);

            var parts = await db.DisaggregationData
                .AsNoTracking()
                .Where(d => d.IndicatorId == indicatorId && d.EntityId == entityId && d.DateId == dateId)
                .Select(d => d.Value)
                .ToListAsync();

            if (parts.Count == 0)
                return (true, datum.Value, null, null);

            var disaggregatedTotal = parts.Sum();
            var difference = Math.Abs(datum.Value.Value - disaggregatedTotal);
            return (difference <= tolerance, datum.Value, disaggregatedTotal, difference);
        }
    }
}
