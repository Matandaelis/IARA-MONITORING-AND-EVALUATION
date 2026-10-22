using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Code;

namespace Monic.Web.Controllers
{
    /// <summary>
    /// External push API for automated pipelines (Kestra flows, partner systems).
    /// Authenticated by the X-Api-Key header (Settings.ExternalApiKey).
    /// Pushes land as ImportJobs: either left "Validated" for human review on the Imports
    /// page (autoCommit=false, the safe default) or committed immediately (autoCommit=true,
    /// audited). Pushed records are never auto-submitted — submissions stay human-controlled.
    /// </summary>
    [Route("api/external"), AllowAnonymous]
    public class ExternalPushController : Controller
    {
        private readonly IDbContextFactory<ApplicationDbContext> dbFactory;

        public ExternalPushController(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            this.dbFactory = dbFactory;
        }

        [HttpPost("records")]
        public async Task<IActionResult> PushRecords([FromBody] ExternalPushRequest request)
        {
            if (request == null) return BadRequest("A request body is required");

            using var db = dbFactory.CreateDbContext();

            var settings = db.GetDbSettings();

            // API-key gate
            var providedKey = Request.Headers["X-Api-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(settings.ExternalApiKey))
                return StatusCode(503, new { error = "External push is not configured. Generate an API key on the Tools page." });

            if (string.IsNullOrWhiteSpace(providedKey) || !string.Equals(providedKey, settings.ExternalApiKey, StringComparison.Ordinal))
                return Unauthorized(new { error = "Invalid or missing X-Api-Key header" });

            // request validation
            if (request.QuestionnaireId == Guid.Empty) return BadRequest(new { error = "questionnaireId is required" });
            if (request.Rows == null || request.Rows.Count == 0) return BadRequest(new { error = "rows is required" });

            if (!await db.Questionnaires.AnyAsync(o => o.QuestionnaireId == request.QuestionnaireId))
                return BadRequest(new { error = "Questionnaire not found" });

            // flatten to the same row shape the import pipeline uses
            var rows = request.Rows.Select(r =>
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["EntityCode"] = r.EntityCode?.Trim() ?? "",
                    ["DateCode"] = r.DateCode?.Trim() ?? "",
                    ["ResponseCode"] = r.ResponseCode?.Trim() ?? ""
                };

                if (r.Cells != null)
                    foreach (var cell in r.Cells)
                        dict[cell.Key.Trim()] = cell.Value?.Trim() ?? "";

                return dict;
            }).ToList();

            var baseCurrency = BudgetAnalytics.ResolveBaseCurrency(settings);
            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, request.QuestionnaireId, rows);

            var importJob = new ImportJob
            {
                ImportType = ImportType.Records,
                FileName = $"kestra-push-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json",
                TargetId = request.QuestionnaireId,
                CreatedOnUtc = DateTime.UtcNow,
                TotalRows = report.Rows.Count,
                ValidRows = report.ValidCount,
                ErrorRows = report.ErrorCount,
                ValidationReportJson = Newtonsoft.Json.JsonConvert.SerializeObject(report)
            };

            db.ImportJobs.Add(importJob);
            await db.SaveChangesAsync();

            var committed = false;

            if (request.AutoCommit && report.ValidCount > 0)
            {
                await ImportProcessor.CommitRecordsAsync(db, request.QuestionnaireId, report, null, submitAfterCommit: false);

                importJob.Status = ImportStatus.Committed;
                importJob.CommittedOnUtc = DateTime.UtcNow;
                db.Entry(importJob).State = EntityState.Modified;
                await db.SaveChangesAsync();

                committed = true;
            }
            else
            {
                db.Entry(importJob).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }

            return Ok(new
            {
                importJobId = importJob.ImportJobId,
                status = importJob.Status.ToString(),
                committed,
                totalRows = importJob.TotalRows,
                validRows = importJob.ValidRows,
                errorRows = importJob.ErrorRows,
                note = committed
                    ? "Valid rows were committed (not auto-submitted — review and submit in the UI)."
                    : "Rows are staged as a validated import — review and commit on the Imports page."
            });
        }
    }

    public class ExternalPushRequest
    {
        public Guid QuestionnaireId { get; set; }
        public bool AutoCommit { get; set; }
        public List<ExternalPushRow> Rows { get; set; } = new List<ExternalPushRow>();
    }

    public class ExternalPushRow
    {
        public string EntityCode { get; set; }
        public string DateCode { get; set; }
        public string ResponseCode { get; set; }

        /// <summary>Question code → value (option questions accept labels, multi-select separated by |).</summary>
        public Dictionary<string, string> Cells { get; set; } = new Dictionary<string, string>();
    }
}
