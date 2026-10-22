using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    /// <summary>
    /// A user comment attached to any subject (form record, indicator datum, task, logframe row, etc.).
    /// The subject is addressed polymorphically via SubjectType + SubjectId.
    /// </summary>
    public class Comment
    {
        [Key, Required]
        public Guid CommentId { get; set; }

        [Required, MaxLength(50)]
        public string SubjectType { get; set; }

        [Required]
        public Guid SubjectId { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(4000)]
        public string Body { get; set; }

        /// <summary>Optional parent comment, for one-level-deep threaded replies.</summary>
        public Guid? ParentCommentId { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public DateTime? ModifiedOnUtc { get; set; }

        public Guid? ModifiedById { get; set; }

        public virtual ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();

        [ForeignKey("ParentCommentId")]
        public virtual Comment ParentComment { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User ModifiedBy { get; set; }

        public Comment()
        {
            CommentId = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Body;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            Comment other = (Comment)obj;

            return CommentId == other.CommentId;
        }

        public override int GetHashCode()
        {
            return CommentId.GetHashCode();
        }
    }

    /// <summary>
    /// A user's reaction (e.g. "like", "dislike") to a comment. One reaction per user per comment.
    /// </summary>
    public class CommentReaction
    {
        [Key, Required]
        public Guid CommentReactionId { get; set; }

        [Required]
        public Guid CommentId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(25)]
        public string Reaction { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        [ForeignKey("CommentId")]
        public virtual Comment Comment { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public CommentReaction()
        {
            CommentReactionId = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Reaction;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            CommentReaction other = (CommentReaction)obj;

            return CommentReactionId == other.CommentReactionId;
        }

        public override int GetHashCode()
        {
            return CommentReactionId.GetHashCode();
        }
    }

    /// <summary>
    /// An in-app notification for a user (mention, review outcome, etc.), with an optional client link.
    /// </summary>
    public class Notification
    {
        [Key, Required]
        public Guid NotificationId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Body { get; set; }

        [MaxLength(300)]
        public string Link { get; set; }

        [Required]
        public bool IsRead { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public Notification()
        {
            NotificationId = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            Notification other = (Notification)obj;

            return NotificationId == other.NotificationId;
        }

        public override int GetHashCode()
        {
            return NotificationId.GetHashCode();
        }
    }

    /// <summary>
    /// A named tag that can be applied to documents (and other items in future phases).
    /// </summary>
    public class Tag
    {
        [Key, Required]
        public Guid TagId { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public virtual ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();

        public Tag()
        {
            TagId = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            Tag other = (Tag)obj;

            return TagId == other.TagId;
        }

        public override int GetHashCode()
        {
            return TagId.GetHashCode();
        }
    }

    /// <summary>
    /// Join entity: a tag applied to a document.
    /// </summary>
    public class DocumentTag
    {
        [Key, Required]
        public Guid DocumentTagId { get; set; }

        [Required]
        public Guid DocumentId { get; set; }

        [Required]
        public Guid TagId { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; }

        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; }

        public DocumentTag()
        {
            DocumentTagId = Guid.NewGuid();
        }

        public override string ToString()
        {
            return Tag?.ToString() ?? DocumentTagId.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            DocumentTag other = (DocumentTag)obj;

            return DocumentTagId == other.DocumentTagId;
        }

        public override int GetHashCode()
        {
            return DocumentTagId.GetHashCode();
        }
    }

    /// <summary>
    /// Per-user notification channel preferences, per notification type (e.g. Mention, DataReview).
    /// </summary>
    public class NotificationPreference
    {
        [Key, Required]
        public Guid NotificationPreferenceId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; }

        [Required]
        public bool InApp { get; set; }

        [Required]
        public bool Email { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public NotificationPreference()
        {
            NotificationPreferenceId = Guid.NewGuid();
            InApp = true;
        }

        public override string ToString()
        {
            return $"{Type}: inApp={InApp}, email={Email}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            NotificationPreference other = (NotificationPreference)obj;

            return NotificationPreferenceId == other.NotificationPreferenceId;
        }

        public override int GetHashCode()
        {
            return NotificationPreferenceId.GetHashCode();
        }
    }

    /// <summary>
    /// Join entity: a tag applied to an indicator (indicator library search by tags).
    /// </summary>
    public class IndicatorTag
    {
        [Key, Required]
        public Guid IndicatorTagId { get; set; }

        [Required]
        public Guid IndicatorId { get; set; }

        [Required]
        public Guid TagId { get; set; }

        [ForeignKey("IndicatorId")]
        public virtual Indicator Indicator { get; set; }

        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; }

        public IndicatorTag()
        {
            IndicatorTagId = Guid.NewGuid();
        }

        public override string ToString()
        {
            return Tag?.ToString() ?? IndicatorTagId.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            IndicatorTag other = (IndicatorTag)obj;

            return IndicatorTagId == other.IndicatorTagId;
        }

        public override int GetHashCode()
        {
            return IndicatorTagId.GetHashCode();
        }
    }
}
