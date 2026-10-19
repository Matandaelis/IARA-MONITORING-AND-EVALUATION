using System;
using System.Collections.Generic;
using System.Linq;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    public record VarianceResult(
        decimal? PercentageAchievement,
        string StatusBadge,
        decimal? AbsoluteVariance
    );

    public record ParityValidationResult(
        bool IsValid,
        decimal CalculatedSum,
        decimal Discrepancy
    );

    public static class VarianceEngine
    {
        public const string StatusUnderperforming = "Underperforming";
        public const string StatusOnTrack = "OnTrack";
        public const string StatusExceeding = "Exceeding";
        public const string StatusNotApplicable = "NotApplicable";

        public static VarianceResult CalculateVariance(decimal? actual, decimal? target, IndicatorDirection direction = IndicatorDirection.HigherIsBetter)
        {
            if (!actual.HasValue || !target.HasValue)
            {
                return new VarianceResult(null, StatusNotApplicable, null);
            }

            decimal act = actual.Value;
            decimal tgt = target.Value;
            decimal absoluteVariance = act - tgt;

            // Zero Target Boundary Safeguards
            if (tgt == 0m)
            {
                if (act == 0m)
                {
                    return new VarianceResult(100m, StatusOnTrack, 0m);
                }

                if (direction == IndicatorDirection.LowerIsBetter)
                {
                    // Target was 0, but actual > 0 -> Underperforming
                    // Target was 0, and actual < 0 -> Exceeding
                    return act > 0m
                        ? new VarianceResult(0m, StatusUnderperforming, absoluteVariance)
                        : new VarianceResult(100m, StatusExceeding, absoluteVariance);
                }
                else
                {
                    // Target was 0, and actual > 0 -> Exceeding
                    // Target was 0, but actual < 0 -> Underperforming
                    return act > 0m
                        ? new VarianceResult(100m, StatusExceeding, absoluteVariance)
                        : new VarianceResult(0m, StatusUnderperforming, absoluteVariance);
                }
            }

            decimal percentage;

            if (direction == IndicatorDirection.LowerIsBetter)
            {
                // Inverted calculation: lower actual means higher achievement
                // e.g. Target = 100, Actual = 80 -> 100 + ((100 - 80) / 100) * 100 = 120% (Exceeding)
                // e.g. Target = 100, Actual = 130 -> 100 + ((100 - 130) / 100) * 100 = 70% (Underperforming)
                percentage = Math.Round((1m + (tgt - act) / tgt) * 100m, 2);
                if (percentage < 0m) percentage = 0m;
            }
            else
            {
                // Standard calculation: higher actual means higher achievement
                percentage = Math.Round((act / tgt) * 100m, 2);
                if (percentage < 0m) percentage = 0m;
            }

            string badge;
            if (percentage < 75m)
            {
                badge = StatusUnderperforming;
            }
            else if (percentage <= 110m)
            {
                badge = StatusOnTrack;
            }
            else
            {
                badge = StatusExceeding;
            }

            return new VarianceResult(percentage, badge, absoluteVariance);
        }

        public static ParityValidationResult ValidateDisaggregationParity(decimal aggregateTotal, IEnumerable<decimal> disaggregatedValues, decimal tolerance = 0.0001m)
        {
            if (disaggregatedValues == null)
            {
                return new ParityValidationResult(true, aggregateTotal, 0m);
            }

            var valuesList = disaggregatedValues as IList<decimal> ?? disaggregatedValues.ToList();
            if (valuesList.Count == 0)
            {
                return new ParityValidationResult(true, aggregateTotal, 0m);
            }

            decimal calculatedSum = valuesList.Sum();
            decimal discrepancy = Math.Abs(aggregateTotal - calculatedSum);
            bool isValid = discrepancy <= tolerance;

            return new ParityValidationResult(isValid, calculatedSum, discrepancy);
        }
    }
}
