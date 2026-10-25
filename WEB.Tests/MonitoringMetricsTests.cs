using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace WEB.Tests
{
    public class MonitoringMetricsTests
    {
        [Theory]
        [InlineData(95d, RagStatus.Green)]
        [InlineData(90d, RagStatus.Green)]
        [InlineData(89.9d, RagStatus.Amber)]
        [InlineData(70d, RagStatus.Amber)]
        [InlineData(69.9d, RagStatus.Red)]
        public void ClassifyRag_UsesDefaultThresholds(double achievement, RagStatus expected)
        {
            Assert.Equal(expected, MonitoringMetrics.ClassifyRag((decimal)achievement));
        }

        [Fact]
        public void ClassifyRag_ReturnsNotAssessedForNoData()
        {
            Assert.Equal(RagStatus.NotAssessed, MonitoringMetrics.ClassifyRag(null));
        }

        [Theory]
        [InlineData(85d, 80d, 60d, RagStatus.Green)]
        [InlineData(75d, 80d, 60d, RagStatus.Amber)]
        [InlineData(55d, 80d, 60d, RagStatus.Red)]
        public void ClassifyRag_HonoursPerIndicatorThresholds(
            double achievement, double green, double amber, RagStatus expected)
        {
            Assert.Equal(expected, MonitoringMetrics.ClassifyRag((decimal)achievement, (decimal)green, (decimal)amber));
        }

        [Fact]
        public void ClassifyRag_AmberCannotExceedGreen()
        {
            // Misconfigured amber (95) above green (80) is clamped to green.
            Assert.Equal(RagStatus.Red, MonitoringMetrics.ClassifyRag(79m, 80m, 95m));
        }

        [Fact]
        public void WeightedAverage_IgnoresNullsAndNormalisesWeights()
        {
            var result = MonitoringMetrics.WeightedAverage(new (decimal?, decimal)[]
            {
                (80m, 2m),
                (null, 5m),
                (100m, 2m)
            });

            Assert.Equal(90m, result);
        }

        [Fact]
        public void WeightedAverage_ReturnsNullWhenNothingIsAssessed()
        {
            Assert.Null(MonitoringMetrics.WeightedAverage(new (decimal?, decimal)[]
            {
                (null, 1m),
                (null, 3m)
            }));
        }

        [Fact]
        public void ComputeComposite_RenormalisesAvailableComponents()
        {
            // results 80, financial 90 (from 110% utilisation), workplan 50, data quality 100
            var result = MonitoringMetrics.ComputeComposite(80m, 110m, 50m, 100m);

            Assert.Equal(80m, result.ResultsScore);
            Assert.Equal(90m, result.FinancialScore);
            Assert.Equal(50m, result.WorkplanScore);
            Assert.Equal(100m, result.DataQualityScore);
            Assert.Equal(79.5m, result.CompositeScore);
        }

        [Fact]
        public void ComputeComposite_HandlesMissingComponents()
        {
            var result = MonitoringMetrics.ComputeComposite(75m, null, null, null);

            Assert.Equal(75m, result.CompositeScore);
            Assert.Null(result.FinancialScore);
        }

        [Theory]
        [InlineData(100d, 75d, 75d)]
        [InlineData(100d, 125d, 75d)]
        public void FinancialScore_PenalisesBothUnderAndOverSpend(double planned, double actual, double expected)
        {
            Assert.Equal((decimal)expected, MonitoringMetrics.FinancialScore((decimal)planned, (decimal)actual));
        }

        [Fact]
        public void FinancialScore_ReturnsNullWithoutAPlan()
        {
            Assert.Null(MonitoringMetrics.FinancialScore(0m, 10m));
        }

        [Fact]
        public void Utilization_ReturnsPercentOfPlan()
        {
            Assert.Equal(62.5m, MonitoringMetrics.Utilization(800m, 500m));
            Assert.Null(MonitoringMetrics.Utilization(0m, 10m));
        }

        [Fact]
        public void CombineRag_RedDominates()
        {
            Assert.Equal(RagStatus.Red, MonitoringMetrics.CombineRag(new[]
            {
                RagStatus.Green, RagStatus.Amber, RagStatus.Red
            }));
        }

        [Fact]
        public void Percent_GuardsAgainstZeroDenominator()
        {
            Assert.Null(MonitoringMetrics.Percent(5, 0));
            Assert.Equal(50m, MonitoringMetrics.Percent(5, 10));
        }
    }
}
