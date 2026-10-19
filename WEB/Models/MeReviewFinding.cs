using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class MeReviewFinding
    {
        [Key, Required]
        public Guid MeReviewFindingId { get; set; }

        [Required]
        public Guid MeReviewId { get; set; }

        public Guid? MeReviewSectionId { get; set; }

        [MaxLength(50)]
        public string Code { get; set; }

        [Required, MaxLength(20)]
        public string Severity { get; set; }

        [MaxLength(150)]
        public string Criterion { get; set; }

        [Required]
        public string Message { get; set; }

        public string Details { get; set; }

        [MaxLength(250)]
        public string LocationRef { get; set; }

        [ForeignKey("MeReviewId")]
        public virtual MeReview MeReview { get; set; }

        [ForeignKey("MeReviewSectionId")]
        public virtual MeReviewSection Section { get; set; }

        public virtual ICollection<MeReviewRecommendation> Recommendations { get; set; } = new List<MeReviewRecommendation>();

        public MeReviewFinding()
        {
            MeReviewFindingId = Guid.NewGuid();
            Severity = "Medium";
        }

        public override string ToString()
        {
            return $"[{Severity}] {Message}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            MeReviewFinding other = (MeReviewFinding)obj;
            return MeReviewFindingId == other.MeReviewFindingId;
        }

        public override int GetHashCode()
        {
            return MeReviewFindingId.GetHashCode();
        }
    }
}
