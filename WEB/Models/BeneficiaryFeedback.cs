using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public enum FeedbackStatus
    {
        Received,
        Investigating,
        Resolved,
        Closed
    }

    public enum FeedbackSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class BeneficiaryFeedback
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }

        [Required, MaxLength(50)]
        public string TrackingCode { get; set; }

        [Required]
        public DateTime DateReceivedUtc { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(100)]
        public string Channel { get; set; } = "Community Meeting";

        [Required, MaxLength(100)]
        public string Category { get; set; } = "General";

        [Required]
        public FeedbackSeverity Severity { get; set; } = FeedbackSeverity.Low;

        [Required]
        public string Description { get; set; }

        public bool IsAnonymous { get; set; } = false;

        [MaxLength(250)]
        public string ContactDetails { get; set; }

        [Required]
        public FeedbackStatus Status { get; set; } = FeedbackStatus.Received;

        public string ResolutionNotes { get; set; }

        public DateTime? ResolvedAtUtc { get; set; }

        public Guid? AssignedToUserId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [ForeignKey("AssignedToUserId")]
        public virtual User AssignedToUser { get; set; }
    }
}
