using System;
using System.Linq;
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
    /// ETL Tier 1: upload → parse → validate (row-level report) → explicit commit.
    /// Committed rows are ordinary live rows; nothing is written to live tables before commit.
    /// </summary>
    [Route("api/[Controller]"), Authorize]
    public class ImportsController : BaseApiController
    {
        public ImportsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        [HttpGet, AuthorizeRoles(Roles.Manager)]
        public async Task<IActionResult> Search([FromQuery] ImportSearchOptions searchOptions)
        {
            IQueryable<ImportJob> results = db.ImportJobs;

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.CreatedBy);
                results = results.Include(o => o.CommittedBy);
            }

            if (searchOptions.ImportType.HasValue)
                results = results.Where(o => o.ImportType == searchOptions.ImportType.Value);

            if (searchOptions.Status.HasValue)
                results = results.Where(o => o.Status == searchOptions.Status.Value);

            results = results.Where(o => !o.IsDeleted).OrderByDescending(o => o.CreatedOnUtc);

            return Ok((await GetPaginatedResponse(results, searchOptions)).Select(o => ModelFactory.Create(o)));
        }

        [HttpGet("{importJobId:Guid}"), AuthorizeRoles(Roles.Manager)]
        public async Task<IActionResult> Get(Guid importJobId)
        {
            var importJob = await db.ImportJobs
                .Include(o => o.CreatedBy)
                .Include(o => o.CommittedBy)
                .FirstOrDefaultAsync(o => o.ImportJobId == importJobId && !o.IsDeleted);

            if (importJob == null)
                return NotFound();

            return Ok(ModelFactory.Create(importJob));
        }

        /// <summary>Generates the import template (columns) for the requested import type and target.</summary>
        [HttpGet("template"), AuthorizeRoles(Roles.Manager)]
        public async Task<IActionResult> Template([FromQuery] ImportType importType, [FromQuery] Guid questionnaireId)
        {
            if (importType != ImportType.Records)
                return BadRequest("Templates are only generated for record imports");

            var template = await ImportProcessor.BuildRecordsTemplateAsync(db, questionnaireId);
            if (template == null) return NotFound();

            return Ok(new { template.FileName, template.Contents, template.Columns });
        }

        /// <summary>Uploads a file, parses and validates it. No live rows are written at this stage.</summary>
        [HttpPost("{importJobId:Guid}"), AuthorizeRoles(Roles.Manager)]
        public async Task<IActionResult> Save(Guid importJobId, [FromBody] ImportJobDTO importJobDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (importJobDTO.ImportJobId != importJobId) return BadRequest("Id mismatch");

            var isNew = importJobDTO.ImportJobId == Guid.Empty;
            if (!isNew) return BadRequest("Import jobs cannot be updated — upload a new file instead");

            if (string.IsNullOrEmpty(importJobDTO.FileContents)) return BadRequest("A file is required");
            if (importJobDTO.ImportType != ImportType.Budget && importJobDTO.ImportType != ImportType.Records)
                return BadRequest("Only budget and record imports are supported at this stage");

            if (importJobDTO.ImportType == ImportType.Records)
            {
                if (!importJobDTO.TargetId.HasValue || importJobDTO.TargetId.Value == Guid.Empty)
                    return BadRequest("A questionnaire is required for record imports");

                if (!await db.Questionnaires.AnyAsync(o => o.QuestionnaireId == importJobDTO.TargetId.Value))
                    return BadRequest("Questionnaire not found");
            }

            var importJob = new ImportJob
            {
                ImportType = importJobDTO.ImportType,
                FileName = importJobDTO.FileName ?? "import",
                TargetId = importJobDTO.TargetId,
                CreatedById = CurrentUser.Id
            };

            db.Entry(importJob).State = EntityState.Added;

            byte[] fileContents;
            try
            {
                fileContents = Convert.FromBase64String(importJobDTO.FileContents);
            }
            catch (FormatException)
            {
                return BadRequest("FileContents must be base64-encoded");
            }

            importJob.ImportJobContent = new ImportJobContent
            {
                ImportJobId = importJob.ImportJobId,
                FileContents = fileContents
            };

            db.Entry(importJob.ImportJobContent).State = EntityState.Added;

            await db.SaveChangesAsync();

            // parse + validate
            try
            {
                using var stream = new MemoryStream(fileContents);
                var baseCurrency = Code.BudgetAnalytics.ResolveBaseCurrency(dbSettings);

                if (importJob.ImportType == ImportType.Budget)
                {
                    var rows = await ImportProcessor.ParseAsync(stream, importJob.ImportType, null);
                    var report = await ImportProcessor.ValidateBudgetRowsAsync(db, rows, baseCurrency);

                    importJob.TotalRows = report.Rows.Count;
                    importJob.ValidRows = report.ValidCount;
                    importJob.ErrorRows = report.ErrorCount;
                    importJob.ValidationReportJson = Newtonsoft.Json.JsonConvert.SerializeObject(report);
                }
                else
                {
                    var template = await ImportProcessor.BuildRecordsTemplateAsync(db, importJob.TargetId.Value);
                    var rows = await ImportProcessor.ParseAsync(stream, importJob.ImportType, template.Columns);
                    var report = await ImportProcessor.ValidateRecordsRowsAsync(db, importJob.TargetId.Value, rows);

                    importJob.TotalRows = report.Rows.Count;
                    importJob.ValidRows = report.ValidCount;
                    importJob.ErrorRows = report.ErrorCount;
                    importJob.ValidationReportJson = Newtonsoft.Json.JsonConvert.SerializeObject(report);
                }

                importJob.Status = ImportStatus.Validated;

                db.Entry(importJob).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                importJob.Status = ImportStatus.Failed;
                importJob.ValidationReportJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { error = exception.Message });
                db.Entry(importJob).State = EntityState.Modified;
                await db.SaveChangesAsync();

                return BadRequest($"The file could not be parsed: {exception.Message}");
            }

            return await Get(importJob.ImportJobId);
        }

        /// <summary>Writes the validated rows into live tables (audited). Record rows enter the questionnaire's submit workflow.</summary>
        [HttpPost("{importJobId:Guid}/commit"), AuthorizeRoles(Roles.Manager)]
        public async Task<IActionResult> Commit(Guid importJobId, [FromQuery] bool submitAfterCommit = false)
        {
            var importJob = await db.ImportJobs
                .FirstOrDefaultAsync(o => o.ImportJobId == importJobId && !o.IsDeleted);

            if (importJob == null)
                return NotFound();

            if (importJob.Status != ImportStatus.Validated)
                return BadRequest("Only validated imports can be committed");

            if (importJob.ValidRows == 0)
                return BadRequest("This import has no valid rows to commit");

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                if (importJob.ImportType == ImportType.Budget)
                {
                    var report = Newtonsoft.Json.JsonConvert.DeserializeObject<Code.BudgetImportReport>(importJob.ValidationReportJson);
                    await ImportProcessor.CommitBudgetAsync(db, importJob, report, CurrentUser.Id);
                }
                else
                {
                    var report = Newtonsoft.Json.JsonConvert.DeserializeObject<Code.RecordsImportReport>(importJob.ValidationReportJson);
                    await ImportProcessor.CommitRecordsAsync(db, importJob.TargetId.Value, report, CurrentUser.Id, submitAfterCommit);
                }

                importJob.Status = ImportStatus.Committed;
                importJob.CommittedOnUtc = DateTime.UtcNow;
                importJob.CommittedById = CurrentUser.Id;

                db.Entry(importJob).State = EntityState.Modified;

                await db.SaveChangesAsync();

                transactionScope.Complete();
            }

            return await Get(importJob.ImportJobId);
        }

        [HttpDelete("{importJobId:Guid}"), AuthorizeRoles(Roles.Manager)]
        public async Task<IActionResult> Delete(Guid importJobId)
        {
            var importJob = await db.ImportJobs
                .FirstOrDefaultAsync(o => o.ImportJobId == importJobId && !o.IsDeleted);

            if (importJob == null)
                return NotFound();

            if (importJob.Status == ImportStatus.Committed)
                return BadRequest("Committed imports cannot be deleted — they are part of the audit trail");

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                await db.ImportJobContents.Where(o => o.ImportJobId == importJobId).ExecuteDeleteAsync();

                importJob.IsDeleted = true;
                db.Entry(importJob).State = EntityState.Modified;

                await db.SaveChangesAsync();

                transactionScope.Complete();
            }

            return Ok();
        }

    }
}
