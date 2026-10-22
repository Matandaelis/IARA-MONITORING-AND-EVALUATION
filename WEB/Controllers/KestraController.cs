using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Code;

namespace Monic.Web.Controllers
{
    /// <summary>
    /// Admin management of the Kestra ETL integration: connection settings, connectivity test,
    /// the connector flow-template catalog (downloadable YAML) and the external push API key.
    /// Surfaced on the Tools page.
    /// </summary>
    [Route("api/[Controller]"), Authorize, AuthorizeRoles(Roles.Administrator)]
    public class KestraController : BaseApiController
    {
        private readonly IHttpClientFactory httpClientFactory;

        public KestraController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings,
            IHttpClientFactory httpClientFactory)
            : base(dbFactory, um, appSettings)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public class KestraSettingsDTO
        {
            public string KestraBaseUrl { get; set; }
            public string KestraApiToken { get; set; }
            public string KestraNamespace { get; set; }
            public string ExternalApiKey { get; set; }
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = dbSettings;
            return Ok(new KestraSettingsDTO
            {
                KestraBaseUrl = settings.KestraBaseUrl,
                KestraApiToken = settings.KestraApiToken,
                KestraNamespace = settings.KestraNamespace,
                ExternalApiKey = settings.ExternalApiKey
            });
        }

        [HttpPost("settings")]
        public async Task<IActionResult> SaveSettings([FromBody] KestraSettingsDTO kestraSettingsDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var settings = db.GetDbSettings();

            settings.KestraBaseUrl = kestraSettingsDTO.KestraBaseUrl?.Trim().TrimEnd('/');
            settings.KestraApiToken = kestraSettingsDTO.KestraApiToken;
            settings.KestraNamespace = kestraSettingsDTO.KestraNamespace?.Trim();
            if (kestraSettingsDTO.ExternalApiKey != null)
                settings.ExternalApiKey = kestraSettingsDTO.ExternalApiKey.Trim();

            db.Entry(settings).State = EntityState.Modified;
            await db.SaveChangesAsync();

            AuditHelper.Write(db, HttpContext, CurrentUser, "Settings", settings.Id, "KestraSettingsUpdate",
                new { dbSettings.KestraBaseUrl, dbSettings.KestraNamespace },
                new { settings.KestraBaseUrl, settings.KestraNamespace });
            await db.SaveChangesAsync();

            return await GetSettings();
        }

        /// <summary>Tests connectivity to the configured Kestra instance.</summary>
        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var settings = dbSettings;

            if (string.IsNullOrWhiteSpace(settings.KestraBaseUrl))
                return Ok(new { configured = false, reachable = false, detail = "Kestra base URL is not configured" });

            try
            {
                var client = httpClientFactory.CreateClient("kestra");
                client.Timeout = TimeSpan.FromSeconds(8);

                var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.KestraBaseUrl}/api/v1/namespaces");
                if (!string.IsNullOrWhiteSpace(settings.KestraApiToken))
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.KestraApiToken);

                var response = await client.SendAsync(request);

                return Ok(new
                {
                    configured = true,
                    reachable = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    detail = response.IsSuccessStatusCode
                        ? $"Connected to Kestra ({settings.KestraBaseUrl})"
                        : $"Kestra responded with HTTP {(int)response.StatusCode} — check the base URL and API token"
                });
            }
            catch (Exception exception)
            {
                return Ok(new
                {
                    configured = true,
                    reachable = false,
                    detail = $"Could not reach Kestra: {Truncate(exception.Message, 200)}"
                });
            }
        }

        /// <summary>The connector catalog with rendered Kestra flow YAML.</summary>
        [HttpGet("flows")]
        public IActionResult Flows()
        {
            var settings = dbSettings;
            var pushUrl = $"{Request.Scheme}://{Request.Host}{KestraFlowTemplates.PushPath}";

            var flows = KestraFlowTemplates
                .GetAll(settings.KestraNamespace ?? "monic", pushUrl, settings.ExternalApiKey ?? "<API-KEY>")
                .Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.SourceSystem,
                    o.Description,
                    o.Template,
                    PushUrl = pushUrl
                })
                .ToList();

            return Ok(flows);
        }

        /// <summary>Generates a new external push API key (invalidates the previous one).</summary>
        [HttpPost("regeneratekey")]
        public async Task<IActionResult> RegenerateKey()
        {
            var settings = db.GetDbSettings();

            var previousKey = settings.ExternalApiKey;
            settings.ExternalApiKey = Guid.NewGuid().ToString("N");

            db.Entry(settings).State = EntityState.Modified;
            await db.SaveChangesAsync();

            AuditHelper.Write(db, HttpContext, CurrentUser, "Settings", settings.Id, "ExternalApiKeyRegenerated",
                new { key = previousKey == null ? null : "***" }, new { key = "***" });
            await db.SaveChangesAsync();

            return Ok(new { externalApiKey = settings.ExternalApiKey });
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
