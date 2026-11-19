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
                .Include(r => r.Answers)
                    .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(r => r.ResponseId == responseId);

            if (response == null)
                return new List<IndicatorContribution>();

            var mappings = await db.FormIndicatorMappings
                .Include(m => m.QuestionLinks)
                    .ThenInclude(ql => ql.Question)
                .Include(m => m.Indicator)
                .Where(m => m.QuestionnaireId == response.QuestionnaireId && m.IsActive)
                .ToListAsync();

            var produced = new List<IndicatorContribution>();
            var touched = new HashSet<(Guid IndicatorId, Guid EntityId, Guid DateId)>();

            foreach (var mapping in mappings)
            {
                if (mapping.CountOnlySubmitted && !response.SubmittedOnUtc.HasValue)
                    continue;

                if (!string.IsNullOrWhiteSpace(mapping.FilterJson) &&
                    !EvaluateFilter(mapping.FilterJson, response))
                    continue;

                var disaggregationLinks = mapping.QuestionLinks
                    .Where(ql => ql.Role == QuestionIndicatorRole.Disaggregation)
                    .ToList();

                if (disaggregationLinks.Count > 0)
                {
                    foreach (var link in disaggregationLinks)
                    {
                        if (!link.DisaggregationOptionId.HasValue)
                            continue;

                        var answer = response.Answers.FirstOrDefault(a => a.QuestionId == link.QuestionId);
                        var value = ExtractNumericValue(answer, link.ValueMapJson);
                        produced.Add(await RecordContributionAsync(db, mapping, responseId,
                            response.EntityId, response.DateId, link.DisaggregationOptionId,
                            value, null, null));
                    }

                    var total = produced
                        .Where(c => c.ResponseId == responseId && c.FormIndicatorMappingId == mapping.FormIndicatorMappingId)
                        .Sum(c => c.Value);

                    produced.Add(await RecordContributionAsync(db, mapping, responseId,
                        response.EntityId, response.DateId, null, total, null, null));
                }
                else
                {
                    var valueLink = mapping.QuestionLinks.FirstOrDefault(ql => ql.Role == QuestionIndicatorRole.Value);
                    var numeratorLink = mapping.QuestionLinks.FirstOrDefault(ql => ql.Role == QuestionIndicatorRole.Numerator);
                    var denominatorLink = mapping.QuestionLinks.FirstOrDefault(ql => ql.Role == QuestionIndicatorRole.Denominator);

                    decimal value = 0m;
                    decimal? numerator = null;
                    decimal? denominator = null;

                    if (numeratorLink != null && denominatorLink != null)
                    {
                        var numeratorAnswer = response.Answers.FirstOrDefault(a => a.QuestionId == numeratorLink.QuestionId);
                        var denominatorAnswer = response.Answers.FirstOrDefault(a => a.QuestionId == denominatorLink.QuestionId);
                        numerator = numeratorAnswer == null ? 0m : ExtractNumericValue(numeratorAnswer, numeratorLink.ValueMapJson);
                        denominator = denominatorAnswer == null ? 0m : ExtractNumericValue(denominatorAnswer, denominatorLink.ValueMapJson);

                        if (denominator.Value > 0m)
                        {
                            value = numerator.Value / denominator.Value;
                            if (mapping.Indicator != null &&
                                (mapping.Indicator.Units == "%" ||
                                 string.Equals(mapping.Indicator.Units, "percentage", StringComparison.OrdinalIgnoreCase)))
                                value *= 100m;
                        }
                        else
                        {
                            value = 0m;
                        }
                    }
                    else if (valueLink != null)
                    {
                        var answer = response.Answers.FirstOrDefault(a => a.QuestionId == valueLink.QuestionId);
                        value = ExtractNumericValue(answer, valueLink.ValueMapJson);
                    }
                    else if (mapping.Aggregation == MappingAggregation.Count)
                    {
                        value = 1m;
                    }

                    produced.Add(await RecordContributionAsync(db, mapping, responseId,
                        response.EntityId, response.DateId, null, value, numerator, denominator));
                }

                touched.Add((mapping.IndicatorId, response.EntityId, response.DateId));
            }

            await db.SaveChangesAsync();

            foreach (var key in touched)
            {
                await RollupContributionsToDatumAsync(db, key.IndicatorId, key.EntityId, key.DateId, userId);
            }

            await db.SaveChangesAsync();

            if (touched.Count > 0 && appSettings != null)
            {
                try
                {
                    var datums = await db.Data
                        .Where(d => touched.Contains(new ValueTuple<Guid, Guid, Guid>(d.IndicatorId, d.EntityId, d.DateId)))
                        .ToListAsync();

                    var calculation = new Calculation(db, appSettings, userId ?? Guid.Empty);
                    await calculation.SaveAsync(datums);
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("The contribution ledger was saved, but downstream indicator recalculation failed.");
                }
            }

            return produced;
        }

        private static async Task<IndicatorContribution> RecordContributionAsync(
            ApplicationDbContext db,
            FormIndicatorMapping mapping,
            Guid responseId,
            Guid entityId,
            Guid dateId,
            Guid? disaggregationOptionId,
            decimal value,
            decimal? numerator,
            decimal? denominator)
        {
            var existing = await db.IndicatorContributions.FirstOrDefaultAsync(c =>
                c.ResponseId == responseId &&
                c.FormIndicatorMappingId == mapping.FormIndicatorMappingId &&
                c.DisaggregationOptionId == disaggregationOptionId);

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

            var contribution = new IndicatorContribution
            {
                IndicatorContributionId = Guid.NewGuid(),
                FormIndicatorMappingId = mapping.FormIndicatorMappingId,
                ResponseId = responseId,
                IndicatorId = mapping.IndicatorId,
                EntityId = entityId,
                DateId = dateId,
                DisaggregationOptionId = disaggregationOptionId,
                Value = value,
                Numerator = numerator,
                Denominator = denominator,
                ComputedAtUtc = DateTime.UtcNow
            };

            db.IndicatorContributions.Add(contribution);
            return contribution;
        }

        private static async Task<Datum> RollupContributionsToDatumAsync(
            ApplicationDbContext db,
            Guid indicatorId,
            Guid entityId,
            Guid dateId,
            Guid? userId)
        {
            var all = await db.IndicatorContributions
                .Include(c => c.Mapping)
                .Where(c => c.IndicatorId == indicatorId && c.EntityId == entityId && c.DateId == dateId)
                .ToListAsync();

            if (all.Count == 0)
                return null;

            var aggregation = all.FirstOrDefault()?.Mapping?.Aggregation ?? MappingAggregation.Sum;
            var totals = all.Where(c => c.DisaggregationOptionId == null).ToList();
            if (totals.Count == 0)
                totals = all;

            decimal aggregateValue;
            if (totals.Any(c => c.Numerator.HasValue && c.Denominator.HasValue))
            {
                var numerator = totals.Sum(c => c.Numerator ?? 0m);
                var denominator = totals.Sum(c => c.Denominator ?? 0m);
                aggregateValue = denominator > 0m ? numerator / denominator : 0m;

                var indicator = await db.Indicators.FirstOrDefaultAsync(i => i.IndicatorId == indicatorId);
                if (indicator != null &&
                    (indicator.Units == "%" || string.Equals(indicator.Units, "percentage", StringComparison.OrdinalIgnoreCase)))
                    aggregateValue *= 100m;
            }
            else
            {
                aggregateValue = aggregation switch
                {
                    MappingAggregation.Count => totals.Count,
                    MappingAggregation.Average => totals.Average(c => c.Value),
                    MappingAggregation.Min => totals.Min(c => c.Value),
                    MappingAggregation.Max => totals.Max(c => c.Value),
                    _ => totals.Sum(c => c.Value)
                };
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
                    datum.LastSavedById = defaultUserId;
            }

            var disaggregated = all
                .Where(c => c.DisaggregationOptionId.HasValue)
                .GroupBy(c => c.DisaggregationOptionId.Value);

            foreach (var group in disaggregated)
            {
                var value = aggregation switch
                {
                    MappingAggregation.Count => (decimal)group.Count(),
                    MappingAggregation.Average => group.Average(c => c.Value),
                    _ => group.Sum(c => c.Value)
                };

                var existing = await db.DisaggregationData.FirstOrDefaultAsync(d =>
                    d.IndicatorId == indicatorId && d.EntityId == entityId && d.DateId == dateId &&
                    d.DisaggregationOptionId == group.Key);

                if (existing == null)
                {
                    db.DisaggregationData.Add(new DisaggregationDatum
                    {
                        DisaggregationDatumId = Guid.NewGuid(),
                        IndicatorId = indicatorId,
                        EntityId = entityId,
                        DateId = dateId,
                        DisaggregationOptionId = group.Key,
                        Value = value
                    });
                }
                else
                {
                    existing.Value = value;
                }
            }

            return datum;
        }

        private static decimal ExtractNumericValue(Answer answer, string valueMapJson)
        {
            if (answer == null)
                return 0m;
            if (answer.ValueNumber.HasValue)
                return answer.ValueNumber.Value;

            if (!string.IsNullOrWhiteSpace(valueMapJson))
            {
                try
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string, decimal>>(valueMapJson);
                    if (map != null)
                    {
                        if (!string.IsNullOrWhiteSpace(answer.Value) && map.TryGetValue(answer.Value.Trim(), out var mapped))
                            return mapped;
                        foreach (var option in answer.AnswerOptions)
                        {
                            if (map.TryGetValue(option.QuestionOptionId.ToString(), out var optionMapped))
                                return optionMapped;
                        }
                    }
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException("A mapping value map is malformed JSON.");
                }
            }

            if (!string.IsNullOrWhiteSpace(answer.Value) &&
                decimal.TryParse(answer.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            if (answer.ValueBoolean.HasValue)
                return answer.ValueBoolean.Value ? 1m : 0m;

            return 0m;
        }

        private static bool EvaluateFilter(string filterJson, Response response)
        {
            try
            {
                using var doc = JsonDocument.Parse(filterJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("questionCode", out var codeProperty) &&
                    root.TryGetProperty("equals", out var equalsProperty))
                {
                    var code = codeProperty.GetString();
                    var expected = equalsProperty.GetString();
                    var answer = response.Answers.FirstOrDefault(a =>
                        a.Question != null && string.Equals(a.Question.Code, code, StringComparison.Ordinal));

                    if (answer == null)
                        return false;

                    return string.Equals(answer.Value?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase);
                }

                throw new InvalidOperationException("Unsupported or malformed mapping filter.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("A mapping filter is malformed JSON.", ex);
            }
        }
    }
}
