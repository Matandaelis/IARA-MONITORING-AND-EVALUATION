using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Hubs;
using Monic.Web.Models;

namespace Monic.Web.Services
{
    /// <summary>
    /// Central notification dispatcher: persists an in-app notification (respecting the
    /// recipient's preferences), pushes it in real time via SignalR and optionally emails it.
    /// Default preferences: in-app enabled, email disabled.
    /// </summary>
    public class NotificationDispatcher
    {
        private readonly IHubContext<NotificationHub> hubContext;
        private readonly IEmailService emailService;

        public NotificationDispatcher(IHubContext<NotificationHub> hubContext, IEmailService emailService)
        {
            this.hubContext = hubContext;
            this.emailService = emailService;
        }

        public async System.Threading.Tasks.Task DispatchAsync(
            ApplicationDbContext db,
            User recipient,
            string type,
            string title,
            string body,
            string link,
            Guid? createdById)
        {
            if (recipient == null || recipient.Disabled) return;

            var preference = await db.NotificationPreferences
                .FirstOrDefaultAsync(o => o.UserId == recipient.Id && o.Type == type);

            var inApp = preference?.InApp ?? true;
            var email = preference?.Email ?? false;

            if (inApp)
            {
                var notification = new Notification
                {
                    UserId = recipient.Id,
                    Type = type,
                    Title = title,
                    Body = body,
                    Link = link,
                    IsRead = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedById = createdById
                };

                db.Notifications.Add(notification);

                await db.SaveChangesAsync();

                await hubContext.Clients.Group($"user-{recipient.Id}").SendAsync("notification", new
                {
                    notificationId = notification.NotificationId,
                    type = notification.Type,
                    title = notification.Title,
                    body = notification.Body,
                    link = notification.Link
                });
            }

            if (email && !string.IsNullOrEmpty(recipient.Email))
            {
                var bodyHtml = $"<p><strong>{WebUtility.HtmlEncode(title)}</strong></p><p>{WebUtility.HtmlEncode(body ?? "")}</p>"
                    + (string.IsNullOrEmpty(link) ? "" : $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Open in MONIC</a></p>");

                await emailService.SendEmailAsync(recipient.Email, recipient.FullName, title, body ?? "", bodyHtml);
            }
        }
    }
}
