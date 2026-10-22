using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Code;
using Monic.Web.Services;
using Microsoft.AspNetCore.SignalR;
using Monic.Web.Hubs;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class CommentsController : BaseApiController
    {
        private readonly NotificationDispatcher notificationDispatcher;
        private readonly IHubContext<NotificationHub> hubContext;

        public CommentsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings,
            NotificationDispatcher notificationDispatcher, IHubContext<NotificationHub> hubContext)
            : base(dbFactory, um, appSettings)
        {
            this.notificationDispatcher = notificationDispatcher;
            this.hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] CommentSearchOptions searchOptions)
        {
            // comments are always queried per subject; the subject must be visible to the caller
            if (string.IsNullOrWhiteSpace(searchOptions.SubjectType) || !searchOptions.SubjectId.HasValue)
                return BadRequest("SubjectType and SubjectId are required");

            if (!await CommentAccess.CanAccessAsync(db, CurrentUser, searchOptions.SubjectType, searchOptions.SubjectId.Value))
                return Forbid();

            IQueryable<Comment> results = db.Comments;

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.CreatedBy);
                results = results.Include(o => o.ModifiedBy);
            }

            if (searchOptions.IncludeChildren)
            {
                results = results.Include(o => o.Reactions).ThenInclude(r => r.User);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.SubjectType))
                results = results.Where(o => o.SubjectType == searchOptions.SubjectType);

            if (searchOptions.SubjectId.HasValue)
                results = results.Where(o => o.SubjectId == searchOptions.SubjectId);

            if (searchOptions.CreatedById.HasValue)
                results = results.Where(o => o.CreatedById == searchOptions.CreatedById);

            results = results.OrderBy(o => o.CreatedOnUtc);

            return Ok((await GetPaginatedResponse(results, searchOptions)).Select(o => ModelFactory.Create(o, searchOptions.IncludeParents, searchOptions.IncludeChildren)));
        }

        [HttpGet("{commentId:Guid}")]
        public async Task<IActionResult> Get(Guid commentId)
        {
            var comment = await db.Comments
                .Include(o => o.CreatedBy)
                .Include(o => o.ModifiedBy)
                .Include(o => o.Reactions).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(o => o.CommentId == commentId);

            if (comment == null)
                return NotFound();

            if (!await CommentAccess.CanAccessAsync(db, CurrentUser, comment.SubjectType, comment.SubjectId))
                return Forbid();

            return Ok(ModelFactory.Create(comment));
        }

        [HttpPost("{commentId:Guid}")]
        public async Task<IActionResult> Save(Guid commentId, [FromBody] CommentDTO commentDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (commentDTO.CommentId != commentId) return BadRequest("Id mismatch");

            var isNew = commentDTO.CommentId == Guid.Empty;

            Comment comment;
            if (isNew)
            {
                if (string.IsNullOrWhiteSpace(commentDTO.SubjectType))
                    return BadRequest("SubjectType is required");

                if (!await CommentAccess.CanAccessAsync(db, CurrentUser, commentDTO.SubjectType, commentDTO.SubjectId))
                    return Forbid();

                comment = new Comment
                {
                    SubjectType = commentDTO.SubjectType,
                    SubjectId = commentDTO.SubjectId,
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedById = CurrentUser.Id
                };

                db.Entry(comment).State = EntityState.Added;
            }
            else
            {
                comment = await db.Comments
                    .FirstOrDefaultAsync(o => o.CommentId == commentDTO.CommentId);

                if (comment == null)
                    return NotFound();

                if (comment.CreatedById != CurrentUser.Id && !CurrentUserIsInRole(Roles.Administrator))
                    return Forbid();

                // subject must remain visible, and edits are limited to a 15-minute author window
                if (!await CommentAccess.CanAccessAsync(db, CurrentUser, comment.SubjectType, comment.SubjectId))
                    return Forbid();

                if (comment.CreatedById == CurrentUser.Id && !CurrentUserIsInRole(Roles.Administrator)
                    && comment.CreatedOnUtc < DateTime.UtcNow.AddMinutes(-15))
                    return BadRequest("Comments can only be edited within 15 minutes of posting");

                comment.ModifiedOnUtc = DateTime.UtcNow;
                comment.ModifiedById = CurrentUser.Id;

                db.Entry(comment).State = EntityState.Modified;
            }

            ModelFactory.Hydrate(comment, commentDTO);

            await db.SaveChangesAsync();

            // live update for everyone viewing the same record
            await hubContext.Clients
                .Group($"comment-{comment.SubjectType}-{comment.SubjectId}")
                .SendAsync("commentAdded", ModelFactory.Create(comment, true, true));

            if (isNew) await CreateMentionNotificationsAsync(comment, commentDTO);

            return await Get(comment.CommentId);
        }

        /// <summary>Sets or removes the current user's reaction on a comment. Pass a null/empty reaction to remove.</summary>
        [HttpPost("{commentId:Guid}/react")]
        public async Task<IActionResult> React(Guid commentId, [FromBody] CommentReactionDTO reactionDTO)
        {
            var comment = await db.Comments.FirstOrDefaultAsync(o => o.CommentId == commentId);
            if (comment == null) return NotFound();

            if (!await CommentAccess.CanAccessAsync(db, CurrentUser, comment.SubjectType, comment.SubjectId))
                return Forbid();

            var reaction = string.IsNullOrWhiteSpace(reactionDTO?.Reaction) ? null : reactionDTO.Reaction.Trim();

            var existing = await db.CommentReactions
                .FirstOrDefaultAsync(o => o.CommentId == commentId && o.UserId == CurrentUser.Id);

            if (reaction == null)
            {
                if (existing != null)
                    db.Entry(existing).State = EntityState.Deleted;
            }
            else if (existing == null)
            {
                db.CommentReactions.Add(new CommentReaction
                {
                    CommentId = commentId,
                    UserId = CurrentUser.Id,
                    Reaction = reaction,
                    CreatedOnUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.Reaction = reaction;
                db.Entry(existing).State = EntityState.Modified;
            }

            await db.SaveChangesAsync();

            return await Get(commentId);
        }

        [HttpDelete("{commentId:Guid}")]
        public async Task<IActionResult> Delete(Guid commentId)
        {
            var comment = await db.Comments
                .FirstOrDefaultAsync(o => o.CommentId == commentId);

            if (comment == null)
                return NotFound();

            if (comment.CreatedById != CurrentUser.Id && !CurrentUserIsInRole(Roles.Administrator))
                return Forbid();

            if (!await CommentAccess.CanAccessAsync(db, CurrentUser, comment.SubjectType, comment.SubjectId))
                return Forbid();

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                await db.CommentReactions.Where(o => o.CommentId == commentId).ExecuteDeleteAsync();

                db.Entry(comment).State = EntityState.Deleted;

                await db.SaveChangesAsync();

                transactionScope.Complete();
            }

            return Ok();
        }

        private async System.Threading.Tasks.Task CreateMentionNotificationsAsync(Comment comment, CommentDTO commentDTO)
        {
            if (commentDTO.MentionUserIds == null || !commentDTO.MentionUserIds.Any()) return;

            var mentionedUserIds = commentDTO.MentionUserIds
                .Where(o => o != CurrentUser.Id && o != Guid.Empty)
                .Distinct()
                .ToList();

            if (!mentionedUserIds.Any()) return;

            var mentionedUsers = await db.Users
                .Where(o => mentionedUserIds.Contains(o.Id))
                .ToListAsync();

            var body = comment.Body;
            var bodyPreview = body.Length > 300 ? body.Substring(0, 300) + "…" : body;

            foreach (var mentionedUser in mentionedUsers)
            {
                await notificationDispatcher.DispatchAsync(
                    db,
                    mentionedUser,
                    "Mention",
                    $"{CurrentUser.FullName ?? CurrentUser.UserName} mentioned you in a comment",
                    bodyPreview,
                    commentDTO.NotificationLink,
                    CurrentUser.Id);
            }
        }

    }
}
