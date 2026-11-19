using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
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

            var providedKey = Request.Headers["X-Api-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(settings.ExternalApiKey))
                return StatusCode(503, new { error = "External push is not configured." });
            if (string.IsNullOrWhiteSpace(providedKey) || !string.Equals(providedKey, settings.ExternalApiKey, StringComparison.Ordinal))
                return Unauthorized(new { error = "Invalid or missing X-Api-Key header" });

            if (request.QuestionnaireId == Guid.Empty)
                return BadRequest(new { error = "questionnaireId is required" });
            if (request.Rows == null || request.Rows.Count == 0)
                return BadRequest(new { error = "rows is required" });

            var questionnaire = await db.Questionnaires
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.QuestionnaireId == request.QuestionnaireId);
            if (questionnaire == null)
                return BadRequest(new { error = "Questionnaire not found" });

            var rows = request.Rows.Select(row =>
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["EntityCode"] = row.EntityCode?.Trim() ?? "",
                    ["DateCode"] = row.DateCode?.Trim() ?? "",
                    ["ResponseCode"] = row.ResponseCode?.Trim() ?? ""
                };
                if (row.Cells != null)
                {
                    foreach (var cell in row.Cells)
                        dict[cell.Key.Trim()] = cell.Value?.Trim() ?? "";
                }
                return dict;
            }).ToList();

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, request.QuestionnaireId, rows);
            var importJob = new ImportJob
            {
                ImportType = ImportType.Records,
                FileName = $"external-push-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json",
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

            return Ok(new
            {
                importJobId = importJob.ImportJobId,
                status = importJob.Status.ToString(),
                committed,
                totalRows = importJob.TotalRows,
                validRows = importJob.ValidRows,
                errorRows = importJob.ErrorRows
            });
        }

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
                        .ThenInclude(q => q.QuestionOptionGroup)
                            .ThenInclude(g => g.QuestionOptions)
                .FirstOrDefaultAsync(q => q.QuestionnaireId == questionnaireId);
            if (questionnaire == null)
                return NotFound(new { error = "Questionnaire not found." });

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string koboSubmissionId = null;

            if (payload.ValueKind != JsonValueKind.Object)
                return BadRequest(new { error = "Kobo payload must be a JSON object." });

            foreach (var prop in payload.EnumerateObject())
            {
                if (prop.NameEquals("_id") || prop.NameEquals("id"))
                    koboSubmissionId = prop.Value.ToString();

                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? "",
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True or JsonValueKind.False => prop.Value.GetRawText(),
                    JsonValueKind.Array => string.Join("|", prop.Value.EnumerateArray().Select(x => x.ToString())),
                    _ => ""
                };
            }

            var entityCode = dict.GetValueOrDefault("entity_code")
                ?? dict.GetValueOrDefault("entity")
                ?? dict.GetValueOrDefault("facility_code")
                ?? dict.GetValueOrDefault("site");
            var dateCode = dict.GetValueOrDefault("date_code")
                ?? dict.GetValueOrDefault("period");

            if (string.IsNullOrWhiteSpace(entityCode))
                return BadRequest(new { error = "entity_code (or entity/facility_code/site) is required." });
            if (string.IsNullOrWhiteSpace(dateCode))
                return BadRequest(new { error = "date_code (or period) is required." });

            var entity = await db.Entities
                .FirstOrDefaultAsync(e => e.Code == entityCode && e.EntityTypeId == questionnaire.EntityTypeId);
            if (entity == null)
                return BadRequest(new { error = "Entity code was not found for this questionnaire.", entityCode });

            var date = await db.Dates
                .FirstOrDefaultAsync(d => d.Code == dateCode && d.DateType == questionnaire.DateType);
            if (date == null)
                return BadRequest(new { error = "Date/period code was not found for this questionnaire.", dateCode });

            if (!string.IsNullOrWhiteSpace(koboSubmissionId))
            {
                var existing = await db.Responses.FirstOrDefaultAsync(r =>
                    r.QuestionnaireId == questionnaireId && r.ClientGeneratedId == koboSubmissionId);
                if (existing != null)
                    return Ok(new { responseId = existing.ResponseId, status = "AlreadyProcessed", message = "Submission already received and processed." });
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
                CompletedQuestions = 0,
                ClientGeneratedId = koboSubmissionId ?? Guid.NewGuid().ToString()
            };

            foreach (var question in allQuestions)
            {
                if (string.IsNullOrWhiteSpace(question.Code) || !dict.TryGetValue(question.Code, out var value) || string.IsNullOrWhiteSpace(value))
                    continue;

                var answer = new Answer
                {
                    AnswerId = Guid.NewGuid(),
                    ResponseId = response.ResponseId,
                    QuestionId = question.QuestionId,
                    Value = value
                };

                if (question.QuestionOptionGroup?.QuestionOptions != null)
                {
                    var option = question.QuestionOptionGroup.QuestionOptions.FirstOrDefault(o =>
                        string.Equals(o.Label, value, StringComparison.OrdinalIgnoreCase) ||
                        (o.Value.HasValue && string.Equals(o.Value.Value.ToString(), value, StringComparison.OrdinalIgnoreCase)));
                    if (option != null)
                    {
                        answer.AnswerOptions.Add(new AnswerOption
                        {
                            AnswerId = answer.AnswerId,
                            QuestionOptionId = option.QuestionOptionId
                        });
                    }
                }

                AnswerValueProjector.Apply(answer);
                response.Answers.Add(answer);
            }

            response.CompletedQuestions = response.Answers.Count;
            db.Responses.Add(response);
            await db.SaveChangesAsync();

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

            var synced = 0;
            var duplicates = 0;
            var rejected = 0;
            var processedIds = new List<Guid>();

            foreach (var submission in request.Submissions)
            {
                var questionnaire = await db.Questionnaires
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.QuestionnaireId == submission.QuestionnaireId);
                if (questionnaire == null)
                {
                    rejected++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(submission.ClientGeneratedId))
                {
                    var existing = await db.Responses.FirstOrDefaultAsync(r =>
                        r.QuestionnaireId == submission.QuestionnaireId && r.ClientGeneratedId == submission.ClientGeneratedId);
                    if (existing != null)
                    {
                        duplicates++;
                        processedIds.Add(existing.ResponseId);
                        continue;
                    }
                }

                Entity entity = null;
                if (submission.EntityId.HasValue)
                {
                    entity = await db.Entities.FirstOrDefaultAsync(e =>
                        e.EntityId == submission.EntityId.Value && e.EntityTypeId == questionnaire.EntityTypeId);
                }
                else if (!string.IsNullOrWhiteSpace(submission.EntityCode))
                {
                    entity = await db.Entities.FirstOrDefaultAsync(e =>
                        e.Code == submission.EntityCode && e.EntityTypeId == questionnaire.EntityTypeId);
                }

                if (entity == null)
                {
                    rejected++;
                    continue;
                }

                Date date = null;
                if (submission.DateId.HasValue)
                {
                    date = await db.Dates.FirstOrDefaultAsync(d =>
                        d.DateId == submission.DateId.Value && d.DateType == questionnaire.DateType);
                }
                else if (!string.IsNullOrWhiteSpace(submission.DateCode))
                {
                    date = await db.Dates.FirstOrDefaultAsync(d =>
                        d.Code == submission.DateCode && d.DateType == questionnaire.DateType);
                }

                if (date == null)
                {
                    rejected++;
                    continue;
                }

                var response = new Response
                {
                    ResponseId = Guid.NewGuid(),
                    QuestionnaireId = submission.QuestionnaireId,
                    EntityId = entity.EntityId,
                    DateId = date.DateId,
                    ClientGeneratedId = submission.ClientGeneratedId ?? Guid.NewGuid().ToString(),
                    ClientDeviceId = request.ClientDeviceId,
                    CreatedOnUtc = submission.CreatedOnUtc != default ? submission.CreatedOnUtc : DateTime.UtcNow,
                    SubmittedOnUtc = submission.SubmittedOnUtc != default ? submission.SubmittedOnUtc : DateTime.UtcNow,
                    LastSyncedAtUtc = DateTime.UtcNow,
                    TotalQuestions = submission.Answers?.Count ?? 0,
                    ApplicableQuestions = submission.Answers?.Count ?? 0,
                    CompletedQuestions = submission.Answers?.Count ?? 0
                };

                if (submission.Answers != null)
                {
                    foreach (var item in submission.Answers)
                    {
                        var answer = new Answer
                        {
                            AnswerId = Guid.NewGuid(),
                            ResponseId = response.ResponseId,
                            QuestionId = item.QuestionId,
                            Value = item.Value
                        };

                        if (item.SelectedOptionIds != null)
                        {
                            foreach (var optionId in item.SelectedOptionIds)
                            {
                                answer.AnswerOptions.Add(new AnswerOption
                                {
                                    AnswerId = answer.AnswerId,
                                    QuestionOptionId = optionId
                                });
                            }
                        }

                        AnswerValueProjector.Apply(answer);
                        response.Answers.Add(answer);
                    }
                }

                db.Responses.Add(response);
                await db.SaveChangesAsync();

                await FormIndicatorContributionEngine.ProcessResponseContributionsAsync(db, response.ResponseId);
                processedIds.Add(response.ResponseId);
                synced++;
            }

            return Ok(new { synced, duplicates, rejected, processedIds });
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
}
