using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Code;

namespace Monic.Web.Controllers
{
    /// <summary>
    /// External push API for automated pipelines (Kestra flows, partner systems, KoboToolbox, ODK)
    /// and field PWA offline synchronization.
    /// Authenticated by the X-Api-Key header (Settings.ExternalApiKey) or query token.
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

        /// <summary>
        /// Inbound KoboToolbox / ODK Aggregate Webhook:
        /// Ingests live form submissions, creates Responses with Answers, and automatically
        /// triggers the Layer 2 FormIndicatorContributionEngine for immediate LogFrame roll-up.
        /// </summary>
        [HttpPost("kobo-webhook/{questionnaireId:Guid}")]
        public async Task<IActionResult> IngestKoboWebhook(
            Guid questionnaireId,
            [FromBody] JsonElement payload,
            [FromQuery] string token = null)
        {
            using var db = dbFactory.CreateDbContext();
            var settings = db.GetDbSettings();

            var providedKey = Request.Headers["X-Api-Key"].FirstOrDefault() ?? token;
            if (string.IsNullOrWhiteSpace(settings.ExternalApiKey))
                return StatusCode(503, new { error = "External push is not configured." });

            if (string.IsNullOrWhiteSpace(providedKey) || !string.Equals(providedKey, settings.ExternalApiKey, StringComparison.Ordinal))
                return Unauthorized(new { error = "Invalid or missing API key." });

            var questionnaire = await db.Questionnaires
                .Include(q => q.Sections)
                    .ThenInclude(s => s.Questions)
                        .ThenInclude(qu => qu.QuestionOptionGroup)
                            .ThenInclude(g => g.QuestionOptions)
                .FirstOrDefaultAsync(q => q.QuestionnaireId == questionnaireId);

            if (questionnaire == null) return NotFound(new { error = "Questionnaire not found." });

            // Flatten Kobo JSON payload
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string koboSubmissionId = null;

            if (payload.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in payload.EnumerateObject())
                {
                    if (prop.NameEquals("_id") || prop.NameEquals("id"))
                    {
                        koboSubmissionId = prop.Value.ToString();
                    }

                    if (prop.Value.ValueKind == JsonValueKind.String)
                        dict[prop.Name] = prop.Value.GetString();
                    else if (prop.Value.ValueKind == JsonValueKind.Number)
                        dict[prop.Name] = prop.Value.GetRawText();
                    else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                        dict[prop.Name] = prop.Value.GetRawText();
                    else if (prop.Value.ValueKind == JsonValueKind.Array)
                        dict[prop.Name] = string.Join("|", prop.Value.EnumerateArray().Select(x => x.ToString()));
                }
            }

            // Resolve Entity
            string entityCode = dict.GetValueOrDefault("entity_code")
                ?? dict.GetValueOrDefault("entity")
                ?? dict.GetValueOrDefault("facility_code")
                ?? dict.GetValueOrDefault("site");

            var entity = !string.IsNullOrWhiteSpace(entityCode)
                ? await db.Entities.FirstOrDefaultAsync(e => e.Code == entityCode)
                : await db.Entities.FirstOrDefaultAsync(e => e.EntityTypeId == questionnaire.EntityTypeId);

            if (entity == null)
            {
                return BadRequest(new { error = "Could not resolve valid Entity for this submission." });
            }

            // Resolve Date / Period
            string dateCode = dict.GetValueOrDefault("date_code")
                ?? dict.GetValueOrDefault("period")
                ?? DateTime.UtcNow.ToString("yyyy");

            var date = await db.Dates.FirstOrDefaultAsync(d => d.Code == dateCode)
                ?? await db.Dates.OrderByDescending(d => d.SortOrder).FirstOrDefaultAsync();

            if (date == null)
            {
                return BadRequest(new { error = "Could not resolve valid Date/Period for this submission." });
            }

            // Check if submission already ingested by ClientGeneratedId
            if (!string.IsNullOrWhiteSpace(koboSubmissionId))
            {
                var existing = await db.Responses.FirstOrDefaultAsync(r => r.ClientGeneratedId == koboSubmissionId);
                if (existing != null)
                {
                    return Ok(new
                    {
                        responseId = existing.ResponseId,
                        status = "AlreadyProcessed",
                        message = "Submission already received and processed."
                    });
                }
            }

            var allQuestions = questionnaire.Sections.SelectMany(s => s.Questions).ToList();

            var response = new Response
            {
                ResponseId = Guid.NewGuid(),
                QuestionnaireId = questionnaireId,
                EntityId = entity.EntityId,
                DateId = date.DateId,
                CreatedOnUtc = DateTime.UtcNow,
                SubmittedOnUtc = DateTime.UtcNow,
                TotalQuestions = allQuestions.Count,
                ApplicableQuestions = allQuestions.Count,
                ClientGeneratedId = koboSubmissionId ?? Guid.NewGuid().ToString()
            };

            foreach (var q in allQuestions)
            {
                string val = null;
                if (!string.IsNullOrWhiteSpace(q.Code) && dict.ContainsKey(q.Code))
                {
                    val = dict[q.Code];
                }
                else
                {
                    var match = dict.Keys.FirstOrDefault(k => k.Contains(q.Code ?? "", StringComparison.OrdinalIgnoreCase));
                    if (match != null) val = dict[match];
                }

                if (string.IsNullOrWhiteSpace(val)) continue;

                var answer = new Answer
                {
                    AnswerId = Guid.NewGuid(),
                    ResponseId = response.ResponseId,
                    QuestionId = q.QuestionId,
                    Value = val
                };

                if (q.QuestionOptionGroup?.QuestionOptions != null && q.QuestionOptionGroup.QuestionOptions.Any())
                {
                    var matchedOpt = q.QuestionOptionGroup.QuestionOptions
                        .FirstOrDefault(opt => string.Equals(opt.Label, val, StringComparison.OrdinalIgnoreCase) ||
                                               (opt.Value.HasValue && string.Equals(opt.Value.Value.ToString(), val, StringComparison.OrdinalIgnoreCase)));
                    if (matchedOpt != null)
                    {
                        answer.AnswerOptions.Add(new AnswerOption
                        {
                            AnswerId = answer.AnswerId,
                            QuestionOptionId = matchedOpt.QuestionOptionId
                        });
                    }
                }

                response.Answers.Add(answer);
            }

            response.CompletedQuestions = response.Answers.Count;
            db.Responses.Add(response);
            await db.SaveChangesAsync();

            // Trigger Runtime Contribution Roll-up
            var contributions = await FormIndicatorContributionEngine.ProcessResponseContributionsAsync(db, response.ResponseId);

            return Ok(new
            {
                responseId = response.ResponseId,
                status = "Processed",
                answersCreated = response.Answers.Count,
                contributionsGenerated = contributions.Count,
                source = "KoboToolbox/ODK Webhook"
            });
        }

        /// <summary>
        /// Offline Batch Sync Endpoint:
        /// Receives cached survey responses submitted offline by field enumerators,
        /// creates Responses and Answers, and triggers automated LogFrame roll-ups.
        /// </summary>
        [HttpPost("offline-sync")]
        public async Task<IActionResult> OfflineSync([FromBody] OfflineSyncRequest request)
        {
            if (request == null || request.Submissions == null || !request.Submissions.Any())
                return BadRequest(new { error = "No submissions to synchronize." });

            using var db = dbFactory.CreateDbContext();
            var settings = db.GetDbSettings();

            var providedKey = Request.Headers["X-Api-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(settings.ExternalApiKey))
                return StatusCode(503, new { error = "External push is not configured." });

            if (string.IsNullOrWhiteSpace(providedKey) || !string.Equals(providedKey, settings.ExternalApiKey, StringComparison.Ordinal))
                return Unauthorized(new { error = "Invalid or missing API key." });

            int syncedCount = 0;
            int duplicateCount = 0;
            var processedIds = new List<Guid>();

            foreach (var sub in request.Submissions)
            {
                if (!string.IsNullOrWhiteSpace(sub.ClientGeneratedId))
                {
                    var existing = await db.Responses.FirstOrDefaultAsync(r => r.ClientGeneratedId == sub.ClientGeneratedId);
                    if (existing != null)
                    {
                        duplicateCount++;
                        processedIds.Add(existing.ResponseId);
                        continue;
                    }
                }

                // Resolve Entity
                var entityId = sub.EntityId;
                if (!entityId.HasValue && !string.IsNullOrWhiteSpace(sub.EntityCode))
                {
                    entityId = await db.Entities.Where(e => e.Code == sub.EntityCode).Select(e => (Guid?)e.EntityId).FirstOrDefaultAsync();
                }

                if (!entityId.HasValue)
                {
                    entityId = await db.Entities.Select(e => (Guid?)e.EntityId).FirstOrDefaultAsync();
                }

                // Resolve Date
                var dateId = sub.DateId;
                if (!dateId.HasValue && !string.IsNullOrWhiteSpace(sub.DateCode))
                {
                    dateId = await db.Dates.Where(d => d.Code == sub.DateCode).Select(d => (Guid?)d.DateId).FirstOrDefaultAsync();
                }

                if (!dateId.HasValue)
                {
                    dateId = await db.Dates.OrderByDescending(d => d.SortOrder).Select(d => (Guid?)d.DateId).FirstOrDefaultAsync();
                }

                if (!entityId.HasValue || !dateId.HasValue) continue;

                var response = new Response
                {
                    ResponseId = Guid.NewGuid(),
                    QuestionnaireId = sub.QuestionnaireId,
                    EntityId = entityId.Value,
                    DateId = dateId.Value,
                    ClientGeneratedId = sub.ClientGeneratedId ?? Guid.NewGuid().ToString(),
                    ClientDeviceId = request.ClientDeviceId,
                    CreatedOnUtc = sub.CreatedOnUtc != default ? sub.CreatedOnUtc : DateTime.UtcNow,
                    SubmittedOnUtc = sub.SubmittedOnUtc != default ? sub.SubmittedOnUtc : DateTime.UtcNow,
                    LastSyncedAtUtc = DateTime.UtcNow,
                    TotalQuestions = sub.Answers?.Count ?? 0,
                    ApplicableQuestions = sub.Answers?.Count ?? 0,
                    CompletedQuestions = sub.Answers?.Count ?? 0
                };

                if (sub.Answers != null)
                {
                    foreach (var a in sub.Answers)
                    {
                        var answer = new Answer
                        {
                            AnswerId = Guid.NewGuid(),
                            ResponseId = response.ResponseId,
                            QuestionId = a.QuestionId,
                            Value = a.Value
                        };

                        if (a.SelectedOptionIds != null)
                        {
                            foreach (var optId in a.SelectedOptionIds)
                            {
                                answer.AnswerOptions.Add(new AnswerOption
                                {
                                    AnswerId = answer.AnswerId,
                                    QuestionOptionId = optId
                                });
                            }
                        }

                        response.Answers.Add(answer);
                    }
                }

                db.Responses.Add(response);
                await db.SaveChangesAsync();

                // Roll up contributions
                await FormIndicatorContributionEngine.ProcessResponseContributionsAsync(db, response.ResponseId);

                processedIds.Add(response.ResponseId);
                syncedCount++;
            }

            return Ok(new
            {
                synced = syncedCount,
                duplicates = duplicateCount,
                processedIds
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

    public class OfflineSyncRequest
    {
        public Guid? ClientDeviceId { get; set; }
        public List<OfflineSurveySubmission> Submissions { get; set; } = new List<OfflineSurveySubmission>();
    }

    public class OfflineSurveySubmission
    {
        public string ClientGeneratedId { get; set; }
        public Guid QuestionnaireId { get; set; }
        public Guid? EntityId { get; set; }
        public string EntityCode { get; set; }
        public Guid? DateId { get; set; }
        public string DateCode { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime SubmittedOnUtc { get; set; }
        public List<OfflineAnswerItem> Answers { get; set; } = new List<OfflineAnswerItem>();
    }

    public class OfflineAnswerItem
    {
        public Guid QuestionId { get; set; }
        public string QuestionCode { get; set; }
        public string Value { get; set; }
        public List<Guid> SelectedOptionIds { get; set; } = new List<Guid>();
    }
}
