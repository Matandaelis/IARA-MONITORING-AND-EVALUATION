using System;
using System.Collections.Generic;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class VarianceEngineTests
    {
        [Fact]
        public void CalculateVariance_StandardHigherIsBetter_ReturnsCorrectPercentagesAndBadges()
        {
            // On Track (75% - 110%)
            var onTrack = VarianceEngine.CalculateVariance(850m, 1000m, IndicatorDirection.HigherIsBetter);
            Assert.Equal(85.0m, onTrack.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, onTrack.StatusBadge);
            Assert.Equal(-150m, onTrack.AbsoluteVariance);

            // Underperforming (< 75%)
            var under = VarianceEngine.CalculateVariance(600m, 1000m, IndicatorDirection.HigherIsBetter);
            Assert.Equal(60.0m, under.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusUnderperforming, under.StatusBadge);
            Assert.Equal(-400m, under.AbsoluteVariance);

            // Exceeding (> 110%)
            var exceed = VarianceEngine.CalculateVariance(1200m, 1000m, IndicatorDirection.HigherIsBetter);
            Assert.Equal(120.0m, exceed.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusExceeding, exceed.StatusBadge);
            Assert.Equal(200m, exceed.AbsoluteVariance);
        }

        [Fact]
        public void CalculateVariance_LowerIsBetter_InvertsAchievementCorrectly()
        {
            // Lower than target = positive achievement (Exceeding)
            var exceed = VarianceEngine.CalculateVariance(80m, 100m, IndicatorDirection.LowerIsBetter);
            Assert.Equal(120.0m, exceed.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusExceeding, exceed.StatusBadge);

            // Higher than target = negative achievement (Underperforming)
            var under = VarianceEngine.CalculateVariance(130m, 100m, IndicatorDirection.LowerIsBetter);
            Assert.Equal(70.0m, under.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusUnderperforming, under.StatusBadge);

            // Exactly on target = 100% (On Track)
            var exact = VarianceEngine.CalculateVariance(100m, 100m, IndicatorDirection.LowerIsBetter);
            Assert.Equal(100.0m, exact.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, exact.StatusBadge);
        }

        [Fact]
        public void CalculateVariance_ZeroTarget_SafeguardsAgainstDivideByZero()
        {
            // Both zero
            var bothZero = VarianceEngine.CalculateVariance(0m, 0m, IndicatorDirection.HigherIsBetter);
            Assert.Equal(100m, bothZero.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusOnTrack, bothZero.StatusBadge);

            // Target 0, Actual 10 (Lower is better -> bad outcome)
            var nonZeroLower = VarianceEngine.CalculateVariance(10m, 0m, IndicatorDirection.LowerIsBetter);
            Assert.Equal(0m, nonZeroLower.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusUnderperforming, nonZeroLower.StatusBadge);

            // Target 0, Actual 10 (Higher is better -> bonus outcome)
            var nonZeroHigher = VarianceEngine.CalculateVariance(10m, 0m, IndicatorDirection.HigherIsBetter);
            Assert.Equal(100m, nonZeroHigher.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusExceeding, nonZeroHigher.StatusBadge);
        }

        [Fact]
        public void CalculateVariance_NullInputs_ReturnsNotApplicable()
        {
            var nullActual = VarianceEngine.CalculateVariance(null, 500m);
            Assert.Null(nullActual.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusNotApplicable, nullActual.StatusBadge);

            var nullTarget = VarianceEngine.CalculateVariance(500m, null);
            Assert.Null(nullTarget.PercentageAchievement);
            Assert.Equal(VarianceEngine.StatusNotApplicable, nullTarget.StatusBadge);
        }

        [Fact]
        public void ValidateDisaggregationParity_MatchingSum_ReturnsValid()
        {
            var res = VarianceEngine.ValidateDisaggregationParity(100m, new List<decimal> { 60m, 40m });
            Assert.True(res.IsValid);
            Assert.Equal(100m, res.CalculatedSum);
            Assert.Equal(0m, res.Discrepancy);
        }

        [Fact]
        public void ValidateDisaggregationParity_MismatchedSum_ReturnsInvalid()
        {
            var res = VarianceEngine.ValidateDisaggregationParity(120m, new List<decimal> { 60m, 40m });
            Assert.False(res.IsValid);
            Assert.Equal(100m, res.CalculatedSum);
            Assert.Equal(20m, res.Discrepancy);
        }

        [Fact]
        public void ValidateDisaggregationParity_DecimalPrecision_HonorsTolerance()
        {
            // 33.33 + 33.33 + 33.34 = 100.00
            var res = VarianceEngine.ValidateDisaggregationParity(100.00m, new List<decimal> { 33.33m, 33.33m, 33.34m });
            Assert.True(res.IsValid);
            Assert.Equal(100.00m, res.CalculatedSum);
        }
    }
}
