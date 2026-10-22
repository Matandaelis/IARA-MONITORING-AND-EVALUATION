using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// Subject-level access control for comments. A comment's polymorphic subject
    /// (Response, Task, Indicator, DataReview) must be visible to the user before
    /// they can read, create, react to or delete comments on it.
    /// Unknown subject types are denied by default (Administrator excepted).
    /// </summary>
    public static class CommentAccess
    {
        public static async System.Threading.Tasks.Task<bool> CanAccessAsync(ApplicationDbContext db, AppUser user, string subjectType, Guid subjectId)
        {
            if (user == null || !user.IsLoggedIn) return false;

            if (user.IsInRole(Roles.Administrator) || user.IsInRole(Roles.Manager) || user.IsInRole(Roles.Oversight))
                return true;

            switch ((subjectType ?? "").Trim().ToLowerInvariant())
            {
                case "response":
                {
                    var entityId = await db.Responses
                        .Where(o => o.ResponseId == subjectId)
                        .Select(o => (Guid?)o.EntityId)
                        .FirstOrDefaultAsync();
                    return entityId.HasValue && user.HasEntityPermission(entityId.Value);
                }

                case "task":
                {
                    var projectEntityId = await db.Tasks
                        .Where(o => o.TaskId == subjectId)
                        .Select(o => o.Milestone.Project.EntityId)
                        .FirstOrDefaultAsync();
                    // projects without an owning org unit are open to authenticated users
                    return !projectEntityId.HasValue || user.HasEntityPermission(projectEntityId.Value);
                }

                case "indicator":
                {
                    return user.HasIndicatorPermission(PermissionType.View, subjectId);
                }

                case "datareview":
                {
                    var entityIds = await db.DataReviewLinks
                        .Where(o => o.DataReviewId == subjectId)
                        .Select(o => o.EntityId)
                        .Distinct()
                        .ToListAsync();
                    // a review with no linked rows is only visible to privileged roles (handled above)
                    return entityIds.Any(id => user.HasEntityPermission(id));
                }

                default:
                    return false;
            }
        }
    }
}
