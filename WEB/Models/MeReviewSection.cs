using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class MeReviewSection
    {
        [Key, Required]
        public Guid MeReviewSectionId { get; set; }

        [Required]
        public Guid MeReviewId { get; set; }

        [Required, MaxLength(100)]
        public string SectionKey { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; }

        public int Score { get; set; }

        public int Weight { get; set; } = 100;

        [MaxLength(50)]
        public string Status { get; set; }

        public string Comments { get; set; }

        public int Order { get; set; }

        [ForeignKey("MeReviewId")]
        public virtual MeReview MeReview { get; set; }

        public virtual ICollection<MeReviewFinding> Findings { get; set; } = new List<MeReviewFinding>();

        public MeReviewSection()
        {
            MeReviewSectionId = Guid.NewGuid();
            Score = 100;
            Weight = 100;
            Status = "Pass";
        }

        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            MeReviewSection other = (MeReviewSection)obj;
            return MeReviewSectionId == other.MeReviewSectionId;
        }

        public override int GetHashCode()
        {
            return MeReviewSectionId.GetHashCode();
        }
    }
}
