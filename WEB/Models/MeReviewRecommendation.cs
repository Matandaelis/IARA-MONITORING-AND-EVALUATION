using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class MeReviewRecommendation
    {
        [Key, Required]
        public Guid MeReviewRecommendationId { get; set; }

        [Required]
        public Guid MeReviewId { get; set; }

        public Guid? MeReviewFindingId { get; set; }

        [MaxLength(20)]
        public string Priority { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        [Required]
        public string RecommendationText { get; set; }

        public string ActionSteps { get; set; }

        public bool IsImplemented { get; set; } = false;

        [ForeignKey("MeReviewId")]
        public virtual MeReview MeReview { get; set; }

        [ForeignKey("MeReviewFindingId")]
        public virtual MeReviewFinding Finding { get; set; }

        public MeReviewRecommendation()
        {
            MeReviewRecommendationId = Guid.NewGuid();
            Priority = "MediumTerm";
            IsImplemented = false;
        }

        public override string ToString()
        {
            return RecommendationText;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            MeReviewRecommendation other = (MeReviewRecommendation)obj;
            return MeReviewRecommendationId == other.MeReviewRecommendationId;
        }

        public override int GetHashCode()
        {
            return MeReviewRecommendationId.GetHashCode();
        }
    }
}
