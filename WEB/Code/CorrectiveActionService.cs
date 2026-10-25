using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// Feedback-loop watcher: detects indicators whose achievement deviates beyond the configured
    /// threshold and auto-generates Corrective Action Plan (CAP) records following the M&E framework's
    /// Tier 4 requirement (deviation &gt; ±15% triggers a CAP; field intervention due within 7 business days).
    /// </summary>
    public static class CorrectiveActionService
    {
        /// <summary>
        /// Default deviation threshold in percent. |achievement - 100%| greater than this value triggers a CAP.
        /// </summary>
        public const decimal DefaultDeviationThresholdPercent = 15m;

        /// <summary>
        /// SLA in business days for documenting a field intervention once a CAP is triggered.
        /// </summary>
        public const int DefaultSlaBusinessDays = 7;

        /// <summary>
        /// Scan a project's indicators for deviations beyond the threshold and create CAPs for any that lack one.
        /// Returns the CAPs that were created.
        /// </summary>
        public static async Task<List<MonitoringAction>> DetectAndTriggerCapsAsync(
            ApplicationDbContext db,
            Guid projectId,
            Guid? dateId = null,
            decimal? deviationThresholdPercent = null,
            int? slaBusinessDays = null)
        {
            var threshold = deviationThresholdPercent ?? DefaultDeviationThresholdPercent;
            var slaDays = slaBusinessDays ?? DefaultSlaBusinessDays;

            var indicators = await MonitoringAnalytics.LoadProjectIndicatorsAsync(db, projectId);
            var allDates = await db.Dates.AsNoTracking().ToListAsync();

            var capsCreated = new List<MonitoringAction>();

            foreach (var indicator in indicators)
            {
                var perf = MonitoringAnalytics.ComputePerformance(indicator, dateId, allDates);
                if (perf == null) continue;
                if (!perf.PercentageAchievement.HasValue) continue;

                // Deviation from target: achievement of 100% = on target.
                // Positive = above target, negative = below target.
                var deviation = perf.PercentageAchievement.Value - 100m;

                if (Math.Abs(deviation) <= threshold) continue;

                // Skip if an open CAP already exists for this indicator + period.
                var existingCap = await db.MonitoringActions
                    .AnyAsync(o => o.ProjectId == projectId
                        && o.IndicatorId == indicator.IndicatorId
                        && o.TriggerSource == "VarianceThreshold"
                        && o.Status != MonitoringActionStatus.Completed
                        && o.Status != MonitoringActionStatus.Cancelled);

                if (existingCap) continue;

                var cap = new MonitoringAction
                {
                    ProjectId = projectId,
                    IndicatorId = indicator.IndicatorId,
                    Description = $"Indicator '{indicator.Code ?? indicator.Name}' achievement is {perf.PercentageAchievement.Value:F1}% " +
                                  $"({(deviation >= 0 ? "above" : "below")} target by {Math.Abs(deviation):F1} percentage points). " +
                                  $"Review and document corrective intervention.",
                    TriggerSource = "VarianceThreshold",
                    DeviationPercent = deviation,
                    TriggeredOnUtc = DateTime.UtcNow.Date,
                    DueDate = AddBusinessDays(DateTime.UtcNow.Date, slaDays),
                    Status = MonitoringActionStatus.Open,
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedById = null // system-triggered; no specific user
                };

                db.MonitoringActions.Add(cap);
                capsCreated.Add(cap);
            }

            if (capsCreated.Count > 0)
            {
                await db.SaveChangesAsync();
            }

            return capsCreated;
        }

        /// <summary>
        /// Add business days (Mon–Fri) to a date, skipping weekends.
        /// </summary>
        private static DateTime AddBusinessDays(DateTime startDate, int businessDays)
        {
            var date = startDate;
            var added = 0;
            while (added < businessDays)
            {
                date = date.AddDays(1);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    added++;
                }
            }
            return date;
        }
    }
}
