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
    /// <summary>
    /// Per-user notification channel preferences (in-app / email) per notification type.
    /// Missing entries default to in-app enabled, email disabled.
    /// </summary>
    [Route("api/[Controller]"), Authorize]
    public class NotificationPreferencesController : BaseApiController
    {
        public NotificationPreferencesController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var preferences = await db.NotificationPreferences
                .Where(o => o.UserId == CurrentUser.Id)
                .ToListAsync();

            // default catalogue so the UI can render all known types
            var types = new[] { "Mention", "DataReview", "Comment" };

            var result = types.Select(type =>
            {
                var preference = preferences.FirstOrDefault(o => o.Type == type);
                return new
                {
                    type,
                    inApp = preference?.InApp ?? true,
                    email = preference?.Email ?? false
                };
            }).ToList();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveMine([FromBody] List<NotificationPreferenceDTO> preferencesDTO)
        {
            if (preferencesDTO == null) return BadRequest();

            foreach (var preferenceDTO in preferencesDTO)
            {
                if (string.IsNullOrWhiteSpace(preferenceDTO.Type)) continue;

                var preference = await db.NotificationPreferences
                    .FirstOrDefaultAsync(o => o.UserId == CurrentUser.Id && o.Type == preferenceDTO.Type);

                if (preference == null)
                {
                    preference = new NotificationPreference
                    {
                        UserId = CurrentUser.Id,
                        Type = preferenceDTO.Type
                    };

                    db.Entry(preference).State = EntityState.Added;
                }
                else
                {
                    db.Entry(preference).State = EntityState.Modified;
                }

                preference.InApp = preferenceDTO.InApp;
                preference.Email = preferenceDTO.Email;
            }

            await db.SaveChangesAsync();

            return await GetMine();
        }
    }

    public class NotificationPreferenceDTO
    {
        public string Type { get; set; }
        public bool InApp { get; set; }
        public bool Email { get; set; }
    }
}
