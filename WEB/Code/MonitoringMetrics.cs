using System;
using System.Collections.Generic;
using System.Linq;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// Pure monitoring metrics: RAG classification, weighted roll-ups and the composite performance score.
    /// Kept free of EF so the formulas can be unit-tested and reused by any projection.
    /// </summary>
    public static class MonitoringMetrics
    {
        public const decimal DefaultGreenThreshold = 90m;
        public const decimal DefaultAmberThreshold = 70m;

        public static RagStatus ClassifyRag(decimal? percentageAchievement, decimal? greenThreshold = null, decimal? amberThreshold = null)
        {
            if (!percentageAchievement.HasValue) return RagStatus.NotAssessed;

            var green = greenThreshold.GetValueOrDefault(DefaultGreenThreshold);
            var amber = amberThreshold.GetValueOrDefault(DefaultAmberThreshold);

            // Guard against bad configuration: amber may never exceed green.
            if (amber > green) amber = green;

            if (percentageAchievement.Value >= green) return RagStatus.Green;
            if (percentageAchievement.Value >= amber) return RagStatus.Amber;
            return RagStatus.Red;
        }

        /// <summary>Weighted average that ignores null achievements and normalises the remaining weights.</summary>
        public static decimal? WeightedAverage(IEnumerable<(decimal? Value, decimal Weight)> items)
        {
            if (items == null) return null;

            decimal weighted = 0m;
            decimal weightTotal = 0m;
            foreach (var item in items)
            {
                if (!item.Value.HasValue || item.Weight <= 0m) continue;
                weighted += item.Value.Value * item.Weight;
                weightTotal += item.Weight;
            }

            if (weightTotal <= 0m) return null;
            return Math.Round(weighted / weightTotal, 2);
        }

        public static decimal? Average(IEnumerable<decimal?> values)
        {
            if (values == null) return null;
            var present = values.Where(o => o.HasValue).Select(o => o!.Value).ToList();
            if (present.Count == 0) return null;
            return Math.Round(present.Average(), 2);
        }

        public static decimal? Percent(int? numerator, int? denominator)
        {
            if (!numerator.HasValue || !denominator.HasValue || denominator.Value <= 0) return null;
            return Math.Round((decimal)numerator.Value / denominator.Value * 100m, 1);
        }

        public static decimal? RatioPercent(decimal? numerator, decimal? denominator)
        {
            if (!numerator.HasValue || !denominator.HasValue || denominator.Value == 0m) return null;
            return Math.Round(numerator.Value / denominator.Value * 100m, 1);
        }

        /// <summary>Utilisation of a budget (expended ÷ planned × 100).</summary>
        public static decimal? Utilization(decimal planned, decimal actual)
        {
            if (planned == 0m) return null;
            return Math.Round(actual / planned * 100m, 1);
        }

        /// <summary>
        /// Financial health score: 100 when spend matches plan, degrading as spend moves away from plan
        /// in either direction (under-spend delays delivery; over-spend risks the budget).
        /// </summary>
        public static decimal? FinancialScore(decimal planned, decimal actual)
        {
            if (planned <= 0m) return null;
            var utilization = actual / planned * 100m;
            var distance = Math.Abs(100m - utilization);
            return Math.Round(Math.Clamp(100m - distance, 0m, 100m), 1);
        }

        public record CompositeScoreResult(
            decimal? CompositeScore,
            decimal? ResultsScore,
            decimal? FinancialScore,
            decimal? WorkplanScore,
            decimal? DataQualityScore);

        /// <summary>
        /// Composite monitoring score:
        /// 40% results achievement · 25% financial delivery · 20% workplan delivery · 15% data quality.
        /// Components with no data are excluded and remaining weights are normalised.
        /// </summary>
        public static CompositeScoreResult ComputeComposite(
            decimal? overallAchievement,
            decimal? utilization,
            decimal? workplanCompletion,
            decimal? approvalRate)
        {
            var results = overallAchievement.HasValue
                ? Math.Round(Math.Clamp(overallAchievement.Value, 0m, 100m), 1)
                : (decimal?)null;

            var financial = utilization.HasValue
                ? Math.Round(Math.Clamp(100m - Math.Abs(100m - utilization.Value), 0m, 100m), 1)
                : (decimal?)null;

            var workplan = workplanCompletion.HasValue
                ? Math.Round(Math.Clamp(workplanCompletion.Value, 0m, 100m), 1)
                : (decimal?)null;

            var dataQuality = approvalRate.HasValue
                ? Math.Round(Math.Clamp(approvalRate.Value, 0m, 100m), 1)
                : (decimal?)null;

            var parts = new List<(decimal? Value, decimal Weight)>
            {
                (results, 0.40m),
                (financial, 0.25m),
                (workplan, 0.20m),
                (dataQuality, 0.15m)
            };

            var score = WeightedAverage(parts);
            return new CompositeScoreResult(score, results, financial, workplan, dataQuality);
        }

        public static RagStatus CombineRag(IEnumerable<RagStatus> statuses)
        {
            var list = statuses?.ToList() ?? new List<RagStatus>();
            if (list.Count == 0) return RagStatus.NotAssessed;
            if (list.Any(o => o == RagStatus.Red)) return RagStatus.Red;
            if (list.Any(o => o == RagStatus.Amber)) return RagStatus.Amber;
            if (list.Any(o => o == RagStatus.Green)) return RagStatus.Green;
            return RagStatus.NotAssessed;
        }
    }
}
