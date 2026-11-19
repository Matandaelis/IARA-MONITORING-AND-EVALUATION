using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    public static class ResultSafety
    {
        public static async Task<(bool IsSafe, string Reason)> ValidateSubmissionContextAsync(
            ApplicationDbContext db,
            Guid questionnaireId,
            Guid entityId,
            Guid dateId)
        {
            var questionnaire = await db.Questionnaires.AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuestionnaireId == questionnaireId);
            if (questionnaire == null)
                return (false, "Questionnaire not found.");

            var entity = await db.Entities.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EntityId == entityId);
            if (entity == null)
                return (false, "Entity not found.");
            if (entity.EntityTypeId != questionnaire.EntityTypeId)
                return (false, "Entity type does not match questionnaire entity type.");

            var date = await db.Dates.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DateId == dateId);
            if (date == null)
                return (false, "Reporting period not found.");
            if (date.DateType != questionnaire.DateType)
                return (false, "Reporting period type does not match questionnaire period type.");

            return (true, null);
        }

        public static async Task<(bool IsSafe, string Reason)> ValidateResponseForOfficialReportingAsync(
            ApplicationDbContext db,
            Guid responseId)
        {
            var response = await db.Responses.AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResponseId == responseId);
            if (response == null)
                return (false, "Response not found.");

            var context = await ValidateSubmissionContextAsync(
                db,
                response.QuestionnaireId,
                response.EntityId,
                response.DateId);
            if (!context.IsSafe)
                return context;

            if (!response.SubmittedOnUtc.HasValue)
                return (false, "The response has not been submitted.");

            return (true, null);
        }

        public static async Task<(bool IsSafe, string Reason)> ValidateApprovedDatumAsync(
            ApplicationDbContext db,
            Guid indicatorId,
            Guid entityId,
            Guid dateId)
        {
            var datum = await db.Data.AsNoTracking().FirstOrDefaultAsync(d =>
                d.IndicatorId == indicatorId && d.EntityId == entityId && d.DateId == dateId);

            if (datum == null)
                return (false, "No datum exists for the selected indicator, entity and period.");
            if (!datum.Approved)
                return (false, "The datum is not approved.");
            if (!datum.Value.HasValue)
                return (false, "The approved datum has no value.");

            var disaggregated = await db.DisaggregationData.AsNoTracking()
                .Where(d => d.IndicatorId == indicatorId && d.EntityId == entityId && d.DateId == dateId)
                .Select(d => d.Value)
                .ToListAsync();

            if (disaggregated.Count > 0)
            {
                var sum = disaggregated.Sum();
                if (Math.Abs(datum.Value.Value - sum) > 0.0001m)
                    return (false, "The datum does not reconcile with its disaggregated values.");
            }

            return (true, null);
        }
    }
}
