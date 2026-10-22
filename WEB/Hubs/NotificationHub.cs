using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Monic.Web.Hubs
{
    /// <summary>
    /// Real-time notification hub. Authenticated clients are added to a per-user group
    /// ("user-{id}") and can join per-subject comment groups ("comment-{type}-{id}")
    /// for live comment updates on the record they are viewing.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
                Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

            return base.OnConnectedAsync();
        }

        /// <summary>Join the comment stream of a subject (e.g. while viewing a record page).</summary>
        public Task WatchComments(string subjectType, Guid subjectId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, $"comment-{subjectType}-{subjectId}");
        }

        public Task UnwatchComments(string subjectType, Guid subjectId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"comment-{subjectType}-{subjectId}");
        }
    }
}
