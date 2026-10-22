using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class CommentReactionDTO
    {
        [Required]
        public Guid CommentReactionId { get; set; }

        [Required]
        public Guid CommentId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(25)]
        public string Reaction { get; set; }

        public UserDTO User { get; set; }
    }

    public class CommentDTO
    {
        [Required]
        public Guid CommentId { get; set; }

        [Required, MaxLength(50)]
        public string SubjectType { get; set; }

        [Required]
        public Guid SubjectId { get; set; }

        [Required, MaxLength(4000)]
        public string Body { get; set; }

        public DateTime? CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public DateTime? ModifiedOnUtc { get; set; }

        public Guid? ModifiedById { get; set; }

        public UserDTO CreatedBy { get; set; }

        public UserDTO ModifiedBy { get; set; }

        public List<CommentReactionDTO> Reactions { get; set; } = new List<CommentReactionDTO>();

        /// <summary>User ids mentioned in this comment. Used on save to create notifications; never returned.</summary>
        public List<Guid> MentionUserIds { get; set; } = new List<Guid>();

        /// <summary>Client route used for mention notifications created when saving this comment.</summary>
        [MaxLength(300)]
        public string NotificationLink { get; set; }
    }

    public static partial class ModelFactory
    {
        public static CommentReactionDTO Create(CommentReaction reaction)
        {
            if (reaction == null) return null;

            var dto = new CommentReactionDTO
            {
                CommentReactionId = reaction.CommentReactionId,
                CommentId = reaction.CommentId,
                UserId = reaction.UserId,
                Reaction = reaction.Reaction,
                User = Create(reaction.User)
            };

            return dto;
        }

        public static CommentDTO Create(Comment comment, bool includeParents = true, bool includeChildren = true)
        {
            if (comment == null) return null;

            var commentDTO = new CommentDTO
            {
                CommentId = comment.CommentId,
                SubjectType = comment.SubjectType,
                SubjectId = comment.SubjectId,
                Body = comment.Body,
                CreatedOnUtc = comment.CreatedOnUtc,
                CreatedById = comment.CreatedById,
                ModifiedOnUtc = comment.ModifiedOnUtc,
                ModifiedById = comment.ModifiedById
            };

            if (includeParents)
            {
                commentDTO.CreatedBy = Create(comment.CreatedBy);
                commentDTO.ModifiedBy = Create(comment.ModifiedBy);
            }

            if (includeChildren && comment.Reactions != null)
            {
                foreach (var reaction in comment.Reactions)
                    commentDTO.Reactions.Add(Create(reaction));
            }

            return commentDTO;
        }

        public static void Hydrate(Comment comment, CommentDTO commentDTO)
        {
            comment.SubjectType = commentDTO.SubjectType;
            comment.SubjectId = commentDTO.SubjectId;
            comment.Body = commentDTO.Body;
        }
    }
}
