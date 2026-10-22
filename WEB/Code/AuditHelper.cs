using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// Writes append-only audit entries (AuditLog) for sensitive mutations — financial data,
    /// shared dictionaries and workflow-affecting changes.
    /// </summary>
    public static class AuditHelper
    {
        public static void Write(ApplicationDbContext db, HttpContext httpContext, AppUser user,
            string entityType, Guid entityId, string action, object before, object after)
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = user?.Id,
                BeforeJson = before == null ? null : JsonConvert.SerializeObject(before),
                AfterJson = after == null ? null : JsonConvert.SerializeObject(after),
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = Truncate(httpContext?.Request?.Headers["User-Agent"].ToString(), 300),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.AuditLogs.Add(auditLog);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
