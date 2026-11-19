using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    public static partial class BudgetAnalytics
    {
        public sealed record BudgetCompletenessResult(
            bool IsComplete,
            decimal? Planned,
            decimal? Actual,
            string[] MissingCurrencies);

        /// <summary>
        /// Budget completeness guard. Base-currency totals are not complete unless every
        /// project line in scope has a valid exchange-rate conversion.
        /// </summary>
        public static async Task<BudgetCompletenessResult> ValidateBudgetCompletenessAsync(
            ApplicationDbContext db,
            Guid projectId,
            string baseCurrency)
        {
            var summary = await ComputeSummaryAsync(db, projectId, baseCurrency, true);
            var missing = summary.MissingRates?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                ?? Array.Empty<string>();

            var planned = summary.Rows.Sum(r => r.Planned);
            var actual = summary.Rows.Sum(r => r.Actual);

            return new BudgetCompletenessResult(
                missing.Length == 0,
                planned,
                actual,
                missing);
        }

        /// <summary>
        /// Prevents using a date-period exchange rate before it is known.
        /// </summary>
        public static async Task<(bool IsComplete, decimal? CostPerOutput)> ValidateCostPerOutputAsync(
            ApplicationDbContext db,
            Guid projectId,
            Guid indicatorId,
            string baseCurrency,
            Guid? dateId)
        {
            var cost = await ComputeCostPerOutcomeAsync(db, projectId, indicatorId, baseCurrency);
            if (!cost.OutcomeValue.HasValue)
                return (false, null);

            var lines = await db.BudgetLines.AsNoTracking()
                .Where(l => l.ProjectId == projectId && !l.IsDeleted)
                .ToListAsync();

            foreach (var line in lines)
            {
                var currency = string.IsNullOrWhiteSpace(line.Currency) ? baseCurrency : line.Currency;
                if (!string.Equals(currency, baseCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    var rate = await GetRateAsync(db, currency, baseCurrency, DateTime.UtcNow.Date);
                    if (!rate.HasValue)
                        return (false, null);
                }
            }

            return (cost.CostPerOutcome.HasValue, cost.CostPerOutcome);
        }
    }
}
