using System;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class BeneficiaryFeedbackDTO
    {
        public Guid Id { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        public string ProjectName { get; set; }

        public string TrackingCode { get; set; }

        public DateTime DateReceivedUtc { get; set; }

        [Required, MaxLength(100)]
        public string Channel { get; set; }

        [Required, MaxLength(100)]
        public string Category { get; set; }

        public FeedbackSeverity Severity { get; set; }

        public string SeverityLabel { get; set; }

        [Required]
        public string Description { get; set; }

        public bool IsAnonymous { get; set; }

        public string ContactDetails { get; set; }

        public FeedbackStatus Status { get; set; }

        public string StatusLabel { get; set; }

        public string ResolutionNotes { get; set; }

        public DateTime? ResolvedAtUtc { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string AssignedToUserName { get; set; }
    }

    public static partial class ModelFactory
    {
        public static BeneficiaryFeedbackDTO Create(BeneficiaryFeedback f)
        {
            if (f == null) return null;
            return new BeneficiaryFeedbackDTO
            {
                Id = f.Id,
                ProjectId = f.ProjectId,
                ProjectName = f.Project?.Name,
                TrackingCode = f.TrackingCode,
                DateReceivedUtc = f.DateReceivedUtc,
                Channel = f.Channel,
                Category = f.Category,
                Severity = f.Severity,
                SeverityLabel = f.Severity.ToString(),
                Description = f.Description,
                IsAnonymous = f.IsAnonymous,
                ContactDetails = f.ContactDetails,
                Status = f.Status,
                StatusLabel = f.Status.ToString(),
                ResolutionNotes = f.ResolutionNotes,
                ResolvedAtUtc = f.ResolvedAtUtc,
                AssignedToUserId = f.AssignedToUserId,
                AssignedToUserName = f.AssignedToUser?.UserName
            };
        }

        public static void Hydrate(BeneficiaryFeedback entity, BeneficiaryFeedbackDTO dto)
        {
            if (entity == null || dto == null) return;
            entity.ProjectId = dto.ProjectId;
            entity.Channel = string.IsNullOrWhiteSpace(dto.Channel) ? "Community Meeting" : dto.Channel;
            entity.Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category;
            entity.Severity = dto.Severity;
            entity.Description = dto.Description;
            entity.IsAnonymous = dto.IsAnonymous;
            entity.ContactDetails = dto.ContactDetails;
            entity.Status = dto.Status;
            entity.ResolutionNotes = dto.ResolutionNotes;
            entity.AssignedToUserId = dto.AssignedToUserId;
            if (dto.Status == FeedbackStatus.Resolved && entity.ResolvedAtUtc == null)
            {
                entity.ResolvedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
