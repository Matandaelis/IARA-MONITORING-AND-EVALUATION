using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Monic.Web.Models
{
    public static class ResultSafetyIndexes
    {
        public static void Configure(EntityTypeBuilder<Response> response)
        {
            response.HasIndex(r => new { r.QuestionnaireId, r.ClientGeneratedId })
                .IsUnique()
                .HasFilter("[ClientGeneratedId] IS NOT NULL AND [IsDeleted] = 0")
                .HasDatabaseName("UX_Responses_Questionnaire_ClientGeneratedId");

            response.HasIndex(r => new { r.QuestionnaireId, r.EntityId, r.DateId, r.ResponseId })
                .HasDatabaseName("IX_Responses_MeasurementContext");
        }

        public static void Configure(EntityTypeBuilder<DisaggregationDatum> disaggregation)
        {
            disaggregation.HasIndex(d => new
            {
                d.IndicatorId,
                d.EntityId,
                d.DateId,
                d.DisaggregationOptionId
            }).IsUnique().HasDatabaseName("UX_DisaggregationDatum_Series_Option");
        }

        public static void Configure(EntityTypeBuilder<IndicatorContribution> contribution)
        {
            contribution.HasIndex(c => new
            {
                c.ResponseId,
                c.FormIndicatorMappingId,
                c.DisaggregationOptionId
            }).IsUnique().HasDatabaseName("UX_IndicatorContribution_Response_Mapping_Option");
        }
    }
}
