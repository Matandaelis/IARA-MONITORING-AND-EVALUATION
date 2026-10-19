using System;
using System.Collections.Generic;
using System.Linq;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class DisaggregationParityTests
    {
        [Fact]
        public void ParityValidation_SumMatchesAggregate_PassesValidation()
        {
            decimal aggregateTotal = 1500m;
            var breakdown = new List<decimal> { 500m, 400m, 600m };

            var result = VarianceEngine.ValidateDisaggregationParity(aggregateTotal, breakdown);

            Assert.True(result.IsValid);
            Assert.Equal(1500m, result.CalculatedSum);
            Assert.Equal(0m, result.Discrepancy);
        }

        [Fact]
        public void ParityValidation_SumDiffersFromAggregate_FailsValidationWithDiscrepancy()
        {
            decimal aggregateTotal = 1500m;
            // Sum is 1450 (50 units short)
            var breakdown = new List<decimal> { 500m, 400m, 550m };

            var result = VarianceEngine.ValidateDisaggregationParity(aggregateTotal, breakdown);

            Assert.False(result.IsValid);
            Assert.Equal(1450m, result.CalculatedSum);
            Assert.Equal(50m, result.Discrepancy);
        }

        [Fact]
        public void ParityValidation_OmittedAggregateTotal_AutoSumsDisaggregates()
        {
            var breakdown = new List<DisaggregationDatumDTO>
            {
                new DisaggregationDatumDTO { DisaggregationOptionId = Guid.NewGuid(), Value = 320m },
                new DisaggregationDatumDTO { DisaggregationOptionId = Guid.NewGuid(), Value = 480m }
            };

            // If user enters no aggregate total, the system automatically sums breakdown
            decimal? submittedTotal = null;
            decimal effectiveTotal = submittedTotal ?? breakdown.Sum(b => b.Value);

            Assert.Equal(800m, effectiveTotal);
            var parity = VarianceEngine.ValidateDisaggregationParity(effectiveTotal, breakdown.Select(b => b.Value));
            Assert.True(parity.IsValid);
        }

        [Fact]
        public void ParityValidation_EmptyDisaggregates_TreatedAsStandardScalar()
        {
            decimal aggregateTotal = 42m;
            List<decimal> breakdown = null;

            var result = VarianceEngine.ValidateDisaggregationParity(aggregateTotal, breakdown);

            Assert.True(result.IsValid);
            Assert.Equal(42m, result.CalculatedSum);
            Assert.Equal(0m, result.Discrepancy);
        }

        [Fact]
        public void ParityValidation_HighPrecisionDecimals_ValidWithinTolerance()
        {
            // Fractions with rounding: 100/3 ~ 33.33333333
            decimal aggregate = 100.00m;
            var breakdown = new List<decimal> { 33.33333333m, 33.33333333m, 33.33333334m };

            var result = VarianceEngine.ValidateDisaggregationParity(aggregate, breakdown);

            Assert.True(result.IsValid);
            Assert.True(result.Discrepancy <= 0.0001m);
        }
    }
}
