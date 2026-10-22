using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class NotificationsController : BaseApiController
    {
        public NotificationsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] NotificationSearchOptions searchOptions)
        {
            IQueryable<Notification> results = db.Notifications.Where(o => o.UserId == CurrentUser.Id);

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.CreatedBy);
            }

            if (searchOptions.IsRead.HasValue)
                results = results.Where(o => o.IsRead == searchOptions.IsRead.Value);

            if (!string.IsNullOrWhiteSpace(searchOptions.Type))
                results = results.Where(o => o.Type == searchOptions.Type);

            if (!string.IsNullOrWhiteSpace(searchOptions.q))
                results = results.Where(o => o.Title.Contains(searchOptions.q) || (o.Body != null && o.Body.Contains(searchOptions.q)));

            results = results.OrderByDescending(o => o.CreatedOnUtc);

            return Ok((await GetPaginatedResponse(results, searchOptions)).Select(o => ModelFactory.Create(o, searchOptions.IncludeParents)));
        }

        [HttpGet("unreadcount")]
        public async Task<IActionResult> UnreadCount()
        {
            var count = await db.Notifications
                .CountAsync(o => o.UserId == CurrentUser.Id && !o.IsRead);

            return Ok(new { count });
        }

        /// <summary>Marks notifications as read. Pass an empty id list to mark all of the current user's notifications.</summary>
        [HttpPost("markread")]
        public async Task<IActionResult> MarkRead([FromBody] MarkNotificationsReadDTO markReadDTO)
        {
            var query = db.Notifications.Where(o => o.UserId == CurrentUser.Id && !o.IsRead);

            if (markReadDTO?.Ids != null && markReadDTO.Ids.Any())
                query = query.Where(o => markReadDTO.Ids.Contains(o.NotificationId));

            await query.ExecuteUpdateAsync(o => o.SetProperty(n => n.IsRead, true));

            return Ok();
        }

        [HttpDelete("{notificationId:Guid}")]
        public async Task<IActionResult> Delete(Guid notificationId)
        {
            var notification = await db.Notifications
                .FirstOrDefaultAsync(o => o.NotificationId == notificationId && o.UserId == CurrentUser.Id);

            if (notification == null)
                return NotFound();

            db.Entry(notification).State = EntityState.Deleted;

            await db.SaveChangesAsync();

            return Ok();
        }

    }

    public class MarkNotificationsReadDTO
    {
        public System.Collections.Generic.List<Guid> Ids { get; set; } = new System.Collections.Generic.List<Guid>();
    }
}
