using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// Layer 2 Runtime Automation Engine:
    /// Processes questionnaire responses, evaluates active FormIndicatorMappings, computes
    /// immutable IndicatorContribution ledger rows, and rolls them up into live Datum and
    /// DisaggregationDatum records with recalculation triggering.
    /// </summary>
    public static class FormIndicatorContributionEngine
    {
        public static async Task<List<IndicatorContribution>> ProcessResponseContributionsAsync(
            ApplicationDbContext db,
            Guid responseId,
            AppSettings appSettings = null,
            Guid? userId = null)
        {
            var response = await db.Responses
                .Include(r => r.Answers)
                    .ThenInclude(a => a.AnswerOptions)
                .Include(r => r.Questionnaire)
                .FirstOrDefaultAsync(r => r.ResponseId == responseId);

            if (response == null) return new List<IndicatorContribution>();

            var mappings = await db.FormIndicatorMappings
                .Include(m => m.QuestionLinks)
                    .ThenInclude(ql => ql.Question)
                .Include(m => m.Indicator)
                .Where(m => m.QuestionnaireId == response.QuestionnaireId && m.IsActive)
                .ToListAsync();

            if (mappings.Count == 0) return new List<IndicatorContribution>();

            var contributionsProduced = new List<IndicatorContribution>();
            var touchedIndicatorDateEntities = new HashSet<(Guid IndicatorId, Guid EntityId, Guid DateId)>();

            foreach (var mapping in mappings)
            {
                // Check if mapping requires submitted status
                if (mapping.CountOnlySubmitted && !response.SubmittedOnUtc.HasValue)
                {
                    continue;
                }

                // Check filter if provided
                if (!string.IsNullOrWhiteSpace(mapping.FilterJson) && !EvaluateFilter(mapping.FilterJson, response))
                {
                    continue;
                }

                var entityId = response.EntityId;
                var dateId = response.DateId;

                // Identify if disaggregation links exist
                var disaggLinks = mapping.QuestionLinks
                    .Where(ql => ql.Role == QuestionIndicatorRole.Disaggregation)
                    .ToList();

                var valueLink = mapping.QuestionLinks.FirstOrDefault(ql => ql.Role == QuestionIndicatorRole.Value);
                var numLink = mapping.QuestionLinks.FirstOrDefault(ql => ql.Role == QuestionIndicatorRole.Numerator);
                var denLink = mapping.QuestionLinks.FirstOrDefault(ql => ql.Role == QuestionIndicatorRole.Denominator);

                if (disaggLinks.Count > 0)
                {
                    // Compute per disaggregation option
                    foreach (var dLink in disaggLinks)
                    {
                        var optId = dLink.DisaggregationOptionId;
                        if (!optId.HasValue) continue;

                        var ans = response.Answers.FirstOrDefault(a => a.QuestionId == dLink.QuestionId);
                        decimal val = 0m;
                        if (ans != null)
                        {
                            val = ExtractNumericValue(ans, dLink.ValueMapJson);
                        }

                        var contrib = await RecordContributionAsync(
                            db, mapping, responseId, entityId, dateId, optId, val, null, null);
                        contributionsProduced.Add(contrib);
                    }

                    // Compute total contribution row (null DisaggregationOptionId)
                    decimal totalVal = contributionsProduced
                        .Where(c => c.FormIndicatorMappingId == mapping.FormIndicatorMappingId && c.ResponseId == responseId)
                        .Sum(c => c.Value);

                    var totalContrib = await RecordContributionAsync(
                        db, mapping, responseId, entityId, dateId, null, totalVal, null, null);
                    contributionsProduced.Add(totalContrib);
                }
                else
                {
                    // Single (total) contribution
                    decimal val = 0m;
                    decimal? numerator = null;
                    decimal? denominator = null;

                    if (numLink != null && denLink != null)
                    {
                        var nAns = response.Answers.FirstOrDefault(a => a.QuestionId == numLink.QuestionId);
                        var dAns = response.Answers.FirstOrDefault(a => a.QuestionId == denLink.QuestionId);

                        numerator = nAns != null ? ExtractNumericValue(nAns, numLink.ValueMapJson) : 0m;
                        denominator = dAns != null ? ExtractNumericValue(dAns, denLink.ValueMapJson) : 0m;

                        if (denominator.Value > 0m)
                        {
                            val = numerator.Value / denominator.Value;
                            if (mapping.Indicator != null && (mapping.Indicator.Units == "%" || mapping.Indicator.Units?.ToLower() == "percentage"))
                            {
                                val *= 100m;
                            }
                        }
                    }
                    else if (valueLink != null)
                    {
                        var ans = response.Answers.FirstOrDefault(a => a.QuestionId == valueLink.QuestionId);
                        if (ans != null)
                        {
                            val = ExtractNumericValue(ans, valueLink.ValueMapJson);
                        }
                    }
                    else if (mapping.Aggregation == MappingAggregation.Count)
                    {
                        val = 1m;
                    }

                    var contrib = await RecordContributionAsync(
                        db, mapping, responseId, entityId, dateId, null, val, numerator, denominator);
                    contributionsProduced.Add(contrib);
                }

                touchedIndicatorDateEntities.Add((mapping.IndicatorId, entityId, dateId));
            }

            await db.SaveChangesAsync();

            // Roll up contributions into Datum & DisaggregationDatum for each touched indicator/entity/date
            var rollups = new List<Datum>();
            foreach (var key in touchedIndicatorDateEntities)
            {
                var datum = await RollupContributionsToDatumAsync(db, key.IndicatorId, key.EntityId, key.DateId, userId);
                if (datum != null)
                {
                    rollups.Add(datum);
                }
            }

            await db.SaveChangesAsync();

            // Run calculation pipeline if available
            if (rollups.Count > 0 && appSettings != null)
            {
                try
                {
                    var calc = new Calculation(db, appSettings, userId ?? Guid.Empty);
                    await calc.SaveAsync(rollups, calculateOnly: true);
                }
                catch
                {
                    // Calculation pipeline failure should not revert contribution ledger
                }
            }

            return contributionsProduced;
        }

        private static async Task<IndicatorContribution> RecordContributionAsync(
            ApplicationDbContext db,
            FormIndicatorMapping mapping,
            Guid responseId,
            Guid entityId,
            Guid dateId,
            Guid? disaggOptionId,
            decimal value,
            decimal? numerator,
            decimal? denominator)
        {
            var existing = await db.IndicatorContributions
                .FirstOrDefaultAsync(c => c.ResponseId == responseId
                                       && c.FormIndicatorMappingId == mapping.FormIndicatorMappingId
                                       && c.DisaggregationOptionId == disaggOptionId);

            if (existing != null)
            {
                existing.Value = value;
                existing.Numerator = numerator;
                existing.Denominator = denominator;
                existing.EntityId = entityId;
                existing.DateId = dateId;
                existing.ComputedAtUtc = DateTime.UtcNow;
                return existing;
            }

            var contrib = new IndicatorContribution
            {
                IndicatorContributionId = Guid.NewGuid(),
                FormIndicatorMappingId = mapping.FormIndicatorMappingId,
                ResponseId = responseId,
                IndicatorId = mapping.IndicatorId,
                EntityId = entityId,
                DateId = dateId,
                DisaggregationOptionId = disaggOptionId,
                Value = value,
                Numerator = numerator,
                Denominator = denominator,
                ComputedAtUtc = DateTime.UtcNow
            };

            db.IndicatorContributions.Add(contrib);
            return contrib;
        }

        private static async Task<Datum> RollupContributionsToDatumAsync(
            ApplicationDbContext db,
            Guid indicatorId,
            Guid entityId,
            Guid dateId,
            Guid? userId)
        {
            var allContributions = await db.IndicatorContributions
                .Include(c => c.Mapping)
                .Where(c => c.IndicatorId == indicatorId && c.EntityId == entityId && c.DateId == dateId)
                .ToListAsync();

            if (allContributions.Count == 0) return null;

            var mapping = allContributions.FirstOrDefault()?.Mapping;
            var agg = mapping?.Aggregation ?? MappingAggregation.Sum;

            // Rollup for total (null DisaggregationOptionId)
            var totalContribs = allContributions.Where(c => c.DisaggregationOptionId == null).ToList();
            if (totalContribs.Count == 0)
            {
                totalContribs = allContributions;
            }

            decimal aggregateValue = 0m;
            decimal? aggregateNumerator = null;
            decimal? aggregateDenominator = null;

            if (totalContribs.Any(c => c.Numerator.HasValue && c.Denominator.HasValue))
            {
                aggregateNumerator = totalContribs.Sum(c => c.Numerator ?? 0m);
                aggregateDenominator = totalContribs.Sum(c => c.Denominator ?? 0m);
                if (aggregateDenominator.Value > 0m)
                {
                    aggregateValue = aggregateNumerator.Value / aggregateDenominator.Value;
                    var ind = await db.Indicators.FirstOrDefaultAsync(i => i.IndicatorId == indicatorId);
                    if (ind != null && (ind.Units == "%" || ind.Units?.ToLower() == "percentage"))
                    {
                        aggregateValue *= 100m;
                    }
                }
            }
            else
            {
                switch (agg)
                {
                    case MappingAggregation.Count:
                        aggregateValue = totalContribs.Count;
                        break;
                    case MappingAggregation.Average:
                        aggregateValue = totalContribs.Average(c => c.Value);
                        break;
                    case MappingAggregation.Min:
                        aggregateValue = totalContribs.Min(c => c.Value);
                        break;
                    case MappingAggregation.Max:
                        aggregateValue = totalContribs.Max(c => c.Value);
                        break;
                    case MappingAggregation.Sum:
                    default:
                        aggregateValue = totalContribs.Sum(c => c.Value);
                        break;
                }
            }

            var datum = await db.Data.FirstOrDefaultAsync(d =>
                d.IndicatorId == indicatorId && d.EntityId == entityId && d.DateId == dateId);

            var defaultUserId = userId ?? await db.Users.Select(u => u.Id).FirstOrDefaultAsync();

            if (datum == null)
            {
                datum = new Datum
                {
                    IndicatorId = indicatorId,
                    EntityId = entityId,
                    DateId = dateId,
                    Value = aggregateValue,
                    Aggregated = false,
                    SourceType = DataSourceType.Questionnaire,
                    LastSavedDateUtc = DateTime.UtcNow,
                    LastSavedById = defaultUserId
                };
                db.Data.Add(datum);
            }
            else
            {
                datum.Value = aggregateValue;
                datum.SourceType = DataSourceType.Questionnaire;
                datum.LastSavedDateUtc = DateTime.UtcNow;
                if (defaultUserId != Guid.Empty)
                {
                    datum.LastSavedById = defaultUserId;
                }
            }

            // Also roll up disaggregated contributions into DisaggregationData
            var disaggContribs = allContributions
                .Where(c => c.DisaggregationOptionId != null)
                .GroupBy(c => c.DisaggregationOptionId.Value);

            foreach (var group in disaggContribs)
            {
                var optId = group.Key;
                decimal optValue = 0m;
                switch (agg)
                {
                    case MappingAggregation.Count:
                        optValue = group.Count();
                        break;
                    case MappingAggregation.Average:
                        optValue = group.Average(c => c.Value);
                        break;
                    case MappingAggregation.Sum:
                    default:
                        optValue = group.Sum(c => c.Value);
                        break;
                }

                var disaggDatum = await db.DisaggregationData.FirstOrDefaultAsync(dd =>
                    dd.IndicatorId == indicatorId && dd.EntityId == entityId && dd.DateId == dateId && dd.DisaggregationOptionId == optId);

                if (disaggDatum == null)
                {
                    disaggDatum = new DisaggregationDatum
                    {
                        DisaggregationDatumId = Guid.NewGuid(),
                        IndicatorId = indicatorId,
                        EntityId = entityId,
                        DateId = dateId,
                        DisaggregationOptionId = optId,
                        Value = optValue
                    };
                    db.DisaggregationData.Add(disaggDatum);
                }
                else
                {
                    disaggDatum.Value = optValue;
                }
            }

            return datum;
        }

        private static decimal ExtractNumericValue(Answer ans, string valueMapJson)
        {
            if (ans == null) return 0m;

            if (ans.ValueNumber.HasValue) return ans.ValueNumber.Value;

            if (!string.IsNullOrWhiteSpace(valueMapJson))
            {
                try
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string, decimal>>(valueMapJson);
                    if (map != null)
                    {
                        if (!string.IsNullOrWhiteSpace(ans.Value) && map.TryGetValue(ans.Value.Trim(), out var mappedVal))
                        {
                            return mappedVal;
                        }

                        // Check AnswerOptions
                        foreach (var opt in ans.AnswerOptions)
                        {
                            if (map.TryGetValue(opt.QuestionOptionId.ToString(), out var optMappedVal))
                            {
                                return optMappedVal;
                            }
                        }
                    }
                }
                catch
                {
                    // Fall back to numeric parse
                }
            }

            if (!string.IsNullOrWhiteSpace(ans.Value) &&
                decimal.TryParse(ans.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            if (ans.ValueBoolean.HasValue)
            {
                return ans.ValueBoolean.Value ? 1m : 0m;
            }

            return 0m;
        }

        private static bool EvaluateFilter(string filterJson, Response response)
        {
            try
            {
                using var doc = JsonDocument.Parse(filterJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("questionCode", out var qCodeProp) &&
                    root.TryGetProperty("equals", out var equalsProp))
                {
                    var code = qCodeProp.GetString();
                    var expected = equalsProp.GetString();
                    var matchAnswer = response.Answers.FirstOrDefault(a => a.Question != null && a.Question.Code == code);
                    if (matchAnswer != null)
                    {
                        return string.Equals(matchAnswer.Value?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                // On malformed filter json, allow contribution
            }

            return true;
        }
    }
}
