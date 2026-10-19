using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class MeReview
    {
        [Key, Required]
        public Guid MeReviewId { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(250)]
        public string Title { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(50)]
        public string Domain { get; set; }

        [Required]
        public ReviewTargetType TargetType { get; set; }

        public Guid? TargetEntityId { get; set; }

        [Required]
        public DateTime CreatedDateUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public int OverallScore { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(50)]
        public string Status { get; set; }

        public string Summary { get; set; }

        [MaxLength(500)]
        public string DocumentSource { get; set; }

        public virtual ICollection<MeReviewSection> Sections { get; set; } = new List<MeReviewSection>();

        public virtual ICollection<MeReviewFinding> Findings { get; set; } = new List<MeReviewFinding>();

        public virtual ICollection<MeReviewRecommendation> Recommendations { get; set; } = new List<MeReviewRecommendation>();

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public MeReview()
        {
            MeReviewId = Guid.NewGuid();
            CreatedDateUtc = DateTime.UtcNow;
            Status = "Completed";
            OverallScore = 0;
        }

        public override string ToString()
        {
            return $"{Title} ({Domain})";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            MeReview other = (MeReview)obj;
            return MeReviewId == other.MeReviewId;
        }

        public override int GetHashCode()
        {
            return MeReviewId.GetHashCode();
        }
    }
}
