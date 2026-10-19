using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Monic.Web.Models
{
    public class MeReviewDTO
    {
        [Required]
        public Guid MeReviewId { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; }

        [Required, MaxLength(50)]
        public string Domain { get; set; }

        [Required]
        public ReviewTargetType TargetType { get; set; }

        public Guid? TargetEntityId { get; set; }

        public DateTime CreatedDateUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public string CreatedByName { get; set; }

        public int OverallScore { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; }

        public string Summary { get; set; }

        public string DocumentSource { get; set; }

        public virtual List<MeReviewSectionDTO> Sections { get; set; } = new List<MeReviewSectionDTO>();

        public virtual List<MeReviewFindingDTO> Findings { get; set; } = new List<MeReviewFindingDTO>();

        public virtual List<MeReviewRecommendationDTO> Recommendations { get; set; } = new List<MeReviewRecommendationDTO>();
    }

    public class MeReviewSectionDTO
    {
        [Required]
        public Guid MeReviewSectionId { get; set; }

        [Required]
        public Guid MeReviewId { get; set; }

        [Required, MaxLength(100)]
        public string SectionKey { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; }

        public int Score { get; set; }

        public int Weight { get; set; }

        public string Status { get; set; }

        public string Comments { get; set; }

        public int Order { get; set; }

        public virtual List<MeReviewFindingDTO> Findings { get; set; } = new List<MeReviewFindingDTO>();
    }

    public class MeReviewFindingDTO
    {
        [Required]
        public Guid MeReviewFindingId { get; set; }

        [Required]
        public Guid MeReviewId { get; set; }

        public Guid? MeReviewSectionId { get; set; }

        public string Code { get; set; }

        [Required, MaxLength(20)]
        public string Severity { get; set; }

        public string Criterion { get; set; }

        [Required]
        public string Message { get; set; }

        public string Details { get; set; }

        public string LocationRef { get; set; }
    }

    public class MeReviewRecommendationDTO
    {
        [Required]
        public Guid MeReviewRecommendationId { get; set; }

        [Required]
        public Guid MeReviewId { get; set; }

        public Guid? MeReviewFindingId { get; set; }

        public string Priority { get; set; }

        public string Category { get; set; }

        [Required]
        public string RecommendationText { get; set; }

        public string ActionSteps { get; set; }

        public bool IsImplemented { get; set; }
    }

    public class EvaluateReviewRequestDTO
    {
        public string Domain { get; set; }
        public ReviewTargetType? TargetType { get; set; }
        public Guid? TargetEntityId { get; set; }
        public string Title { get; set; }
        public string NarrativeText { get; set; }
        public SamplingPlanRequest SamplingPlan { get; set; }
    }

    public static partial class ModelFactory
    {
        public static MeReviewDTO Create(MeReview review, bool includeParents = true, bool includeChildren = false)
        {
            if (review == null) return null;

            var dto = new MeReviewDTO
            {
                MeReviewId = review.MeReviewId,
                Title = review.Title,
                Domain = review.Domain,
                TargetType = review.TargetType,
                TargetEntityId = review.TargetEntityId,
                CreatedDateUtc = review.CreatedDateUtc,
                CreatedById = review.CreatedById,
                CreatedByName = review.CreatedBy?.FullName ?? review.CreatedBy?.UserName,
                OverallScore = review.OverallScore,
                Status = review.Status,
                Summary = review.Summary,
                DocumentSource = review.DocumentSource
            };

            if (includeChildren)
            {
                if (review.Sections != null)
                {
                    dto.Sections = review.Sections
                        .OrderBy(s => s.Order)
                        .Select(s => Create(s))
                        .ToList();
                }

                if (review.Findings != null)
                {
                    dto.Findings = review.Findings
                        .Select(f => Create(f))
                        .ToList();
                }

                if (review.Recommendations != null)
                {
                    dto.Recommendations = review.Recommendations
                        .Select(r => Create(r))
                        .ToList();
                }
            }

            return dto;
        }

        public static MeReviewSectionDTO Create(MeReviewSection section)
        {
            if (section == null) return null;

            return new MeReviewSectionDTO
            {
                MeReviewSectionId = section.MeReviewSectionId,
                MeReviewId = section.MeReviewId,
                SectionKey = section.SectionKey,
                Title = section.Title,
                Score = section.Score,
                Weight = section.Weight,
                Status = section.Status,
                Comments = section.Comments,
                Order = section.Order
            };
        }

        public static MeReviewFindingDTO Create(MeReviewFinding finding)
        {
            if (finding == null) return null;

            return new MeReviewFindingDTO
            {
                MeReviewFindingId = finding.MeReviewFindingId,
                MeReviewId = finding.MeReviewId,
                MeReviewSectionId = finding.MeReviewSectionId,
                Code = finding.Code,
                Severity = finding.Severity,
                Criterion = finding.Criterion,
                Message = finding.Message,
                Details = finding.Details,
                LocationRef = finding.LocationRef
            };
        }

        public static MeReviewRecommendationDTO Create(MeReviewRecommendation recommendation)
        {
            if (recommendation == null) return null;

            return new MeReviewRecommendationDTO
            {
                MeReviewRecommendationId = recommendation.MeReviewRecommendationId,
                MeReviewId = recommendation.MeReviewId,
                MeReviewFindingId = recommendation.MeReviewFindingId,
                Priority = recommendation.Priority,
                Category = recommendation.Category,
                RecommendationText = recommendation.RecommendationText,
                ActionSteps = recommendation.ActionSteps,
                IsImplemented = recommendation.IsImplemented
            };
        }

        public static void Hydrate(MeReview review, MeReviewDTO dto)
        {
            if (review == null || dto == null) return;

            review.Title = dto.Title;
            review.Status = dto.Status;
            review.Summary = dto.Summary;
            review.OverallScore = dto.OverallScore;
        }

        public static void Hydrate(MeReviewRecommendation recommendation, MeReviewRecommendationDTO dto)
        {
            if (recommendation == null || dto == null) return;

            recommendation.IsImplemented = dto.IsImplemented;
            recommendation.RecommendationText = dto.RecommendationText;
            recommendation.ActionSteps = dto.ActionSteps;
            recommendation.Priority = dto.Priority;
            recommendation.Category = dto.Category;
        }
    }
}
