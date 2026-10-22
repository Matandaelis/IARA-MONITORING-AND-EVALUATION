using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// M&E budget computations: multi-currency roll-ups to the organisation's base currency,
    /// period burn series (S-curve data) and cost-per-outcome (expenditure ÷ indicator actuals).
    /// All figures respect the 4-ledger model (Planned / Committed / Disbursed / Expended).
    /// </summary>
    public static class BudgetAnalytics
    {
        public static string ResolveBaseCurrency(Monic.Web.Models.Settings settings)
        {
            return string.IsNullOrWhiteSpace(settings?.BaseCurrency) ? "USD" : settings.BaseCurrency;
        }

        /// <summary>
        /// Latest effective rate for from→to on or before asOf (falls back to the inverse rate);
        /// identity for the same currency; null when no rate is known.
        /// </summary>
        public static async System.Threading.Tasks.Task<decimal?> GetRateAsync(
            ApplicationDbContext db, string fromCurrency, string toCurrency, DateTime asOf)
        {
            if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase)) return 1m;

            var direct = await db.FxRates
                .Where(o => o.FromCurrency == fromCurrency && o.ToCurrency == toCurrency && o.EffectiveFrom <= asOf)
                .OrderByDescending(o => o.EffectiveFrom)
                .Select(o => (decimal?)o.Rate)
                .FirstOrDefaultAsync();

            if (direct.HasValue && direct.Value != 0) return direct.Value;

            var inverse = await db.FxRates
                .Where(o => o.FromCurrency == toCurrency && o.ToCurrency == fromCurrency && o.EffectiveFrom <= asOf)
                .OrderByDescending(o => o.EffectiveFrom)
                .Select(o => (decimal?)o.Rate)
                .FirstOrDefaultAsync();

            if (inverse.HasValue && inverse.Value != 0) return 1m / inverse.Value;

            return null;
        }

        /// <summary>
        /// Per-category 4-ledger summary for a project. When inBaseCurrency, amounts are converted
        /// to the organisation's base currency; currencies without a known rate are reported back
        /// so the caller can surface them instead of silently producing wrong totals.
        /// </summary>
        public static async System.Threading.Tasks.Task<BudgetSummaryResponseDTO> ComputeSummaryAsync(
            ApplicationDbContext db, Guid projectId, string baseCurrency, bool inBaseCurrency)
        {
            var response = new BudgetSummaryResponseDTO { BaseCurrency = baseCurrency };

            var lines = await db.BudgetLines
                .AsNoTracking()
                .Where(o => o.ProjectId == projectId && !o.IsDeleted)
                .Select(o => new
                {
                    o.BudgetCategoryId,
                    CategoryName = o.BudgetCategory.Name,
                    o.PlannedAmount,
                    o.CommittedAmount,
                    o.DisbursedAmount,
                    o.ActualAmount,
                    o.Currency
                })
                .ToListAsync();

            var asOf = DateTime.UtcNow.Date;
            var rateCache = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in lines.GroupBy(o => new { o.BudgetCategoryId, o.CategoryName }))
            {
                var row = new BudgetSummaryRowDTO
                {
                    BudgetCategoryId = group.Key.BudgetCategoryId,
                    CategoryName = group.Key.CategoryName,
                    LineCount = group.Count()
                };

                foreach (var line in group)
                {
                    decimal factor = 1m;
                    var lineCurrency = string.IsNullOrWhiteSpace(line.Currency) ? baseCurrency : line.Currency;

                    if (inBaseCurrency && !string.Equals(lineCurrency, baseCurrency, StringComparison.OrdinalIgnoreCase))
                    {
                        var cacheKey = $"{lineCurrency}->{baseCurrency}";
                        if (!rateCache.TryGetValue(cacheKey, out var rate))
                        {
                            rate = await GetRateAsync(db, lineCurrency, baseCurrency, asOf);
                            rateCache[cacheKey] = rate;
                        }

                        if (!rate.HasValue)
                        {
                            if (!response.MissingRates.Contains(lineCurrency)) response.MissingRates.Add(lineCurrency);
                            // unconvertible amounts are excluded from base-currency totals
                            continue;
                        }

                        factor = rate.Value;
                    }

                    row.Planned += line.PlannedAmount * factor;
                    row.Committed += line.CommittedAmount * factor;
                    row.Disbursed += line.DisbursedAmount * factor;
                    row.Actual += line.ActualAmount * factor;
                }

                if (row.LineCount > 0)
                {
                    row.Currency = inBaseCurrency ? baseCurrency : (group.Select(o => o.Currency).FirstOrDefault() ?? baseCurrency);
                    response.Rows.Add(row);
                }
            }

            response.Rows = response.Rows.OrderBy(o => o.CategoryName).ToList();
            return response;
        }

        /// <summary>
        /// Planned vs expended per reporting period (DateId), with cumulative expended — the data
        /// behind an S-curve / burn-up chart. Periods without budget lines are not included.
        /// </summary>
        public static async System.Threading.Tasks.Task<List<BudgetBurnPointDTO>> ComputeBurnSeriesAsync(
            ApplicationDbContext db, Guid projectId, string baseCurrency)
        {
            var lines = await db.BudgetLines
                .AsNoTracking()
                .Where(o => o.ProjectId == projectId && !o.IsDeleted && o.DateId != null)
                .Select(o => new
                {
                    o.DateId,
                    DateName = o.Date.Name,
                    o.Date.OpenFrom,
                    o.Date.OpenTo,
                    o.PlannedAmount,
                    o.ActualAmount,
                    o.Currency
                })
                .ToListAsync();

            var asOf = DateTime.UtcNow.Date;
            var rateCache = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

            var points = new List<BudgetBurnPointDTO>();

            foreach (var group in lines.GroupBy(o => new { o.DateId, o.DateName, o.OpenFrom, o.OpenTo }).OrderBy(o => o.Key.OpenFrom))
            {
                var point = new BudgetBurnPointDTO
                {
                    DateId = group.Key.DateId.Value,
                    Label = group.Key.DateName,
                    StartDate = group.Key.OpenFrom,
                    EndDate = group.Key.OpenTo
                };

                foreach (var line in group)
                {
                    decimal factor = 1m;
                    var lineCurrency = string.IsNullOrWhiteSpace(line.Currency) ? baseCurrency : line.Currency;

                    if (!string.Equals(lineCurrency, baseCurrency, StringComparison.OrdinalIgnoreCase))
                    {
                        var cacheKey = $"{lineCurrency}->{baseCurrency}";
                        if (!rateCache.TryGetValue(cacheKey, out var rate))
                        {
                            rate = await GetRateAsync(db, lineCurrency, baseCurrency, asOf);
                            rateCache[cacheKey] = rate;
                        }

                        if (!rate.HasValue) continue; // skip unconvertible
                        factor = rate.Value;
                    }

                    point.Planned += line.PlannedAmount * factor;
                    point.Expended += line.ActualAmount * factor;
                }

                points.Add(point);
            }

            decimal cumulative = 0m;
            foreach (var point in points)
            {
                cumulative += point.Expended;
                point.CumulativeExpended = cumulative;
            }

            return points;
        }

        /// <summary>
        /// Cost-per-outcome: base-currency expended for the project ÷ the latest approved
        /// (non-disaggregated) datum value recorded for the indicator.
        /// </summary>
        public static async System.Threading.Tasks.Task<BudgetCostPerOutcomeDTO> ComputeCostPerOutcomeAsync(
            ApplicationDbContext db, Guid projectId, Guid indicatorId, string baseCurrency)
        {
            var result = new BudgetCostPerOutcomeDTO
            {
                IndicatorId = indicatorId,
                Currency = baseCurrency
            };

            var indicator = await db.Indicators
                .AsNoTracking()
                .Where(o => o.IndicatorId == indicatorId)
                .Select(o => new { o.Code, o.Name })
                .FirstOrDefaultAsync();

            if (indicator != null)
            {
                result.IndicatorCode = indicator.Code;
                result.IndicatorName = indicator.Name;
            }

            // expended, converted to base currency
            var lines = await db.BudgetLines
                .AsNoTracking()
                .Where(o => o.ProjectId == projectId && !o.IsDeleted)
                .Select(o => new { o.ActualAmount, o.Currency })
                .ToListAsync();

            var asOf = DateTime.UtcNow.Date;
            foreach (var line in lines)
            {
                decimal factor = 1m;
                var lineCurrency = string.IsNullOrWhiteSpace(line.Currency) ? baseCurrency : line.Currency;

                if (!string.Equals(lineCurrency, baseCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    var rate = await GetRateAsync(db, lineCurrency, baseCurrency, asOf);
                    if (!rate.HasValue) continue;
                    factor = rate.Value;
                }

                result.Expended += line.ActualAmount * factor;
            }

            // latest approved datum value for the indicator (any entity)
            var datum = await db.Data
                .AsNoTracking()
                .Where(o => o.IndicatorId == indicatorId && o.Approved && o.Value != null)
                .OrderByDescending(o => o.LastSavedDateUtc)
                .Select(o => new { o.Value, o.LastSavedDateUtc })
                .FirstOrDefaultAsync();

            if (datum != null)
            {
                result.OutcomeValue = datum.Value;
                result.OutcomeDateUtc = datum.LastSavedDateUtc;
            }

            if (result.OutcomeValue.HasValue && result.OutcomeValue.Value != 0)
                result.CostPerOutcome = Math.Round(result.Expended / result.OutcomeValue.Value, 4);

            return result;
        }
    }
}
