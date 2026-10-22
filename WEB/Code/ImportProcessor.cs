using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    /// <summary>
    /// ETL Tier 1: in-app import pipeline. Parses XLSX (EPPlus) and CSV (CsvHelper), validates
    /// rows against system data, and — only after explicit user confirmation — commits rows
    /// into the live tables. Committed budget rows are ordinary BudgetLines and participate in
    /// the audit trail; committed record imports would enter the Submit workflow.
    ///
    /// Budget template (header row, column order fixed):
    /// Project | Category | Description | Planned | Committed | Disbursed | Expended | Currency | PeriodStart | PeriodEnd
    /// </summary>
    public static class ImportProcessor
    {
        public static readonly string[] BudgetColumns =
            { "Project", "Category", "Description", "Planned", "Committed", "Disbursed", "Expended", "Currency", "PeriodStart", "PeriodEnd" };

        public static readonly string[] RecordsFixedColumns = { "EntityCode", "DateCode", "ResponseCode" };

        public static async System.Threading.Tasks.Task<List<Dictionary<string, string>>> ParseAsync(
            Stream stream, ImportType importType, string[] columns)
        {
            if (importType == ImportType.Budget)
            {
                if (columns == null || columns.Length == 0) columns = BudgetColumns;
                return ParseTable(stream, columns);
            }

            if (importType == ImportType.Records)
            {
                if (columns == null || columns.Length == 0)
                    throw new NotSupportedException("Records imports require the questionnaire column layout");
                return ParseTable(stream, columns);
            }

            throw new NotSupportedException($"Import type '{importType}' is not supported yet");
        }

        /// <summary>Builds the import column layout + CSV template for a questionnaire (one column per importable question).</summary>
        public static async System.Threading.Tasks.Task<RecordsTemplate> BuildRecordsTemplateAsync(
            ApplicationDbContext db, Guid questionnaireId)
        {
            var questionnaire = await db.Questionnaires
                .Include(o => o.Sections)
                .ThenInclude(o => o.Questions)
                .FirstOrDefaultAsync(o => o.QuestionnaireId == questionnaireId);

            if (questionnaire == null) return null;

            var questions = questionnaire.Sections
                .SelectMany(o => o.Questions)
                .Where(o => o.QuestionType != QuestionType.Note)
                .OrderBy(o => o.SortOrder)
                .ToList();

            var columns = RecordsFixedColumns.Concat(questions.Select(o => o.Code)).ToArray();

            var csv = new StringBuilder();
            csv.AppendLine(string.Join(",", columns.Select(c => $"\"{c}\"")));

            var entityCode = await db.Entities
                .Where(o => o.EntityTypeId == questionnaire.EntityTypeId)
                .Select(o => o.Code)
                .FirstOrDefaultAsync();

            var sample = new List<string> { entityCode ?? "ENTITY-CODE", "DATE-CODE", "OPTIONAL-DEDUP-CODE" };
            foreach (var question in questions)
                sample.Add(question.QuestionType == QuestionType.OptionList ? "Option label 1|Option label 2" : "Sample answer");
            csv.AppendLine(string.Join(",", sample.Select(c => $"\"{c}\"")));

            return new RecordsTemplate
            {
                FileName = $"{SanitizeFileName(questionnaire.Name)}-import-template.csv",
                Contents = csv.ToString(),
                Columns = columns
            };
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "records";
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var clean = new string(name.Where(o => !invalid.Contains(o)).ToArray()).Trim();
            return clean.Length == 0 ? "records" : clean.Length > 80 ? clean.Substring(0, 80) : clean;
        }

        private static List<Dictionary<string, string>> ParseTable(Stream stream, string[] columns)
        {
            var rows = new List<Dictionary<string, string>>();

            var buffer = new byte[4];
            var position = stream.Position;
            stream.ReadExactly(buffer, 0, 4);
            stream.Position = position;
            var isXlsx = buffer[0] == 0x50 && buffer[1] == 0x4B; // "PK" zip magic

            if (isXlsx)
            {
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.Dimension == null) return rows;

                var endRow = worksheet.Dimension.End.Row;
                var endColumn = Math.Min(worksheet.Dimension.End.Column, columns.Length);

                for (var row = 2; row <= endRow; row++) // row 1 = header
                {
                    var rowValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var hasContent = false;

                    for (var column = 1; column <= endColumn; column++)
                    {
                        var value = worksheet.Cells[row, column].Text?.Trim() ?? "";
                        rowValues[columns[column - 1]] = value;
                        if (value.Length > 0) hasContent = true;
                    }

                    if (hasContent) rows.Add(rowValues);
                }
            }
            else
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    var rowValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var hasContent = false;

                    foreach (var column in columns)
                    {
                        var value = (csv.TryGetField(column, out string field) ? field : null)?.Trim() ?? "";
                        rowValues[column] = value;
                        if (value.Length > 0) hasContent = true;
                    }

                    if (hasContent) rows.Add(rowValues);
                }
            }

            return rows;
        }

        /// <summary>Validates parsed record rows against the questionnaire; no live rows are touched.</summary>
        public static async System.Threading.Tasks.Task<RecordsImportReport> ValidateRecordsRowsAsync(
            ApplicationDbContext db, Guid questionnaireId, List<Dictionary<string, string>> rows)
        {
            var report = new RecordsImportReport();

            var questionnaire = await db.Questionnaires
                .Include(o => o.Sections)
                .ThenInclude(o => o.Questions)
                .FirstOrDefaultAsync(o => o.QuestionnaireId == questionnaireId);

            if (questionnaire == null) throw new InvalidOperationException("Questionnaire not found");

            var questions = questionnaire.Sections
                .SelectMany(o => o.Questions)
                .Where(o => o.QuestionType != QuestionType.Note)
                .ToDictionary(o => o.Code.Trim(), StringComparer.OrdinalIgnoreCase);

            var optionGroupId = questions.Values
                .Where(o => o.QuestionOptionGroupId.HasValue)
                .Select(o => o.QuestionOptionGroupId.Value)
                .Distinct()
                .ToList();

            var optionsByGroup = await db.QuestionOptions
                .Where(o => optionGroupId.Contains(o.QuestionOptionGroupId))
                .ToListAsync();

            var entities = await db.Entities
                .Where(o => o.EntityTypeId == questionnaire.EntityTypeId)
                .ToDictionaryAsync(o => o.Code.Trim().ToLowerInvariant(), o => o.EntityId);

            var dates = await db.Dates.ToDictionaryAsync(o => o.Code.Trim().ToLowerInvariant(), o => o.DateId);

            var seenResponseCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 2;
                var row = rows[i];
                var errors = new List<string>();
                var answerCount = 0;

                var entityCode = row.GetValueOrDefault("EntityCode", "")?.Trim() ?? "";
                var dateCode = row.GetValueOrDefault("DateCode", "")?.Trim() ?? "";
                var responseCode = row.GetValueOrDefault("ResponseCode", "")?.Trim() ?? "";

                if (entityCode.Length == 0) errors.Add("EntityCode is required");
                else if (!entities.TryGetValue(entityCode.ToLowerInvariant(), out _))
                    errors.Add($"Entity '{entityCode}' was not found (or is of the wrong type for this questionnaire)");

                if (dateCode.Length == 0) errors.Add("DateCode is required");
                else if (!dates.TryGetValue(dateCode.ToLowerInvariant(), out _))
                    errors.Add($"Date period '{dateCode}' was not found");

                if (responseCode.Length > 0 && !seenResponseCodes.Add(responseCode))
                    errors.Add($"ResponseCode '{responseCode}' is duplicated in this file");

                // validate every answered question cell
                foreach (var (column, cell) in row)
                {
                    if (column is "EntityCode" or "DateCode" or "ResponseCode") continue;
                    if (string.IsNullOrWhiteSpace(cell)) continue;

                    if (!questions.TryGetValue(column, out var question))
                    {
                        errors.Add($"Unknown question code '{column}' — use the generated template");
                        continue;
                    }

                    if (question.QuestionType == QuestionType.Document)
                    {
                        errors.Add($"Question '{column}' is a document question and cannot be imported from a spreadsheet");
                        continue;
                    }

                    if (question.QuestionType == QuestionType.OptionList)
                    {
                        var tokens = cell.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var groupOptions = optionsByGroup.Where(o => o.QuestionOptionGroupId == question.QuestionOptionGroupId).ToList();

                        foreach (var token in tokens)
                        {
                            if (!groupOptions.Any(o => o.Label.Trim().Equals(token, StringComparison.OrdinalIgnoreCase)))
                                errors.Add($"'{token}' is not a valid option for question '{column}'");
                        }

                        if (tokens.Length == 0) errors.Add($"Question '{column}' has an empty option cell");
                    }

                    if (errors.Count == 0) answerCount++;
                }

                report.Rows.Add(new RecordsImportRowReport
                {
                    RowNumber = rowNumber,
                    EntityCode = entityCode,
                    DateCode = dateCode,
                    ResponseCode = responseCode,
                    AnswersCount = answerCount,
                    Errors = errors,
                    RawCells = new Dictionary<string, string>(row, StringComparer.OrdinalIgnoreCase)
                });

                if (errors.Count == 0) report.ValidCount++;
                else report.ErrorCount++;
            }

            return report;
        }

        /// <summary>
        /// Commits validated record rows as live Responses + Answers, then recalculates progress
        /// through the real skip-logic engine (Response.CalculateProgressAsync). Optional submit
        /// mirrors SurveysController.Submit (SubmittedById + SubmittedOnUtc). userId may be null
        /// for automated pushes (in which case auto-submit is skipped).
        /// </summary>
        public static async System.Threading.Tasks.Task CommitRecordsAsync(
            ApplicationDbContext db, Guid questionnaireId, RecordsImportReport report, Guid? userId, bool submitAfterCommit)
        {
            var questionnaire = await db.Questionnaires
                .Include(o => o.Sections)
                .ThenInclude(o => o.Questions)
                .FirstOrDefaultAsync(o => o.QuestionnaireId == questionnaireId);

            var questions = questionnaire.Sections
                .SelectMany(o => o.Questions)
                .Where(o => o.QuestionType != QuestionType.Note)
                .ToDictionary(o => o.Code.Trim(), StringComparer.OrdinalIgnoreCase);

            var optionGroupId = questions.Values
                .Where(o => o.QuestionOptionGroupId.HasValue)
                .Select(o => o.QuestionOptionGroupId.Value)
                .Distinct()
                .ToList();

            var optionsByGroup = await db.QuestionOptions
                .Where(o => optionGroupId.Contains(o.QuestionOptionGroupId))
                .ToListAsync();

            var entities = await db.Entities
                .Where(o => o.EntityTypeId == questionnaire.EntityTypeId)
                .ToDictionaryAsync(o => o.Code.Trim().ToLowerInvariant(), o => o.EntityId);

            var dates = await db.Dates.ToDictionaryAsync(o => o.Code.Trim().ToLowerInvariant(), o => o.DateId);

            foreach (var row in report.Rows.Where(o => o.Errors.Count == 0))
            {
                var entityId = entities[row.EntityCode.Trim().ToLowerInvariant()];
                var dateId = dates[row.DateCode.Trim().ToLowerInvariant()];

                // find-or-create the response (ResponseCode is the idempotency key)
                Response response = null;
                if (row.ResponseCode.Length > 0)
                {
                    response = await db.Responses
                        .Include(o => o.Answers)
                        .ThenInclude(o => o.AnswerOptions)
                        .FirstOrDefaultAsync(o => o.QuestionnaireId == questionnaireId
                            && o.EntityId == entityId && o.DateId == dateId
                            && o.ClientGeneratedId == row.ResponseCode);
                }

                if (response == null)
                {
                    response = new Response
                    {
                        QuestionnaireId = questionnaireId,
                        EntityId = entityId,
                        DateId = dateId,
                        ClientGeneratedId = row.ResponseCode.Length > 0 ? row.ResponseCode : $"import-{Guid.NewGuid():N}",
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    db.Responses.Add(response);
                    await db.SaveChangesAsync();
                }

                foreach (var (column, cell) in row.RawCells)
                {
                    if (column is "EntityCode" or "DateCode" or "ResponseCode") continue;
                    if (string.IsNullOrWhiteSpace(cell)) continue;

                    if (!questions.TryGetValue(column, out var question)) continue;

                    var answer = response.Answers.FirstOrDefault(o => o.QuestionId == question.QuestionId)
                        ?? db.ChangeTracker.Entries<Answer>()
                            .Select(o => o.Entity)
                            .FirstOrDefault(o => o.ResponseId == response.ResponseId && o.QuestionId == question.QuestionId);

                    if (answer == null)
                    {
                        answer = new Answer { ResponseId = response.ResponseId, QuestionId = question.QuestionId };
                        response.Answers.Add(answer);
                        db.Answers.Add(answer);
                    }

                    if (question.QuestionType == QuestionType.OptionList)
                    {
                        var tokens = cell.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var groupOptions = optionsByGroup.Where(o => o.QuestionOptionGroupId == question.QuestionOptionGroupId).ToList();

                        var selectedOptionIds = tokens
                            .Select(token => groupOptions.First(o => o.Label.Trim().Equals(token, StringComparison.OrdinalIgnoreCase)).QuestionOptionId)
                            .ToHashSet();

                        foreach (var existingOption in answer.AnswerOptions.Where(o => !selectedOptionIds.Contains(o.QuestionOptionId)).ToList())
                            db.Entry(existingOption).State = EntityState.Deleted;

                        foreach (var optionId in selectedOptionIds.Where(id => !answer.AnswerOptions.Any(o => o.QuestionOptionId == id)))
                        {
                            var answerOption = new AnswerOption
                            {
                                AnswerId = answer.AnswerId,
                                QuestionOptionId = optionId
                            };

                            answer.AnswerOptions.Add(answerOption);
                            db.AnswerOptions.Add(answerOption);
                        }

                        // canonical survey behaviour: option answers carry Value = null (see SurveysController)
                        answer.Value = null;
                        AnswerValueProjector.Apply(answer);
                    }
                    else
                    {
                        answer.Value = cell;
                        AnswerValueProjector.Apply(answer);
                    }
                }

                // recalculate progress through the real skip-logic engine
                // (answers must be saved first — the calculation queries the database)
                await db.SaveChangesAsync();
                await response.CalculateProgressAsync(db);

                if (submitAfterCommit && userId.HasValue && questionnaire.UseSubmit && !response.SubmittedOnUtc.HasValue)
                {
                    response.SubmittedById = userId.Value;
                    response.SubmittedOnUtc = DateTime.UtcNow;
                }

                db.Entry(response).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }

            AuditHelper.Write(db, null, null, "ImportJob", questionnaireId,
                "CommitRecords", null, new { questionnaireId, validRows = report.ValidCount, submitAfterCommit });
            await db.SaveChangesAsync();
        }

        /// <summary>Validates parsed budget rows; returns per-row errors without touching live tables.</summary>
        public static async System.Threading.Tasks.Task<BudgetImportReport> ValidateBudgetRowsAsync(
            ApplicationDbContext db, List<Dictionary<string, string>> rows, string baseCurrency)
        {
            var report = new BudgetImportReport();

            var projects = await db.Projects.ToDictionaryAsync(o => o.Name.Trim().ToLowerInvariant(), o => o.ProjectId);
            var categories = await db.BudgetCategories.ToDictionaryAsync(o => o.Name.Trim().ToLowerInvariant(), o => o.BudgetCategoryId);

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 2; // +2: header row and 1-based numbering
                var row = rows[i];
                var errors = new List<string>();

                var projectName = row.GetValueOrDefault("Project", "")?.Trim() ?? "";
                var categoryName = row.GetValueOrDefault("Category", "")?.Trim() ?? "";

                if (!projects.TryGetValue(projectName.ToLowerInvariant(), out var projectId))
                    errors.Add($"Project '{projectName}' was not found");

                if (!categories.TryGetValue(categoryName.ToLowerInvariant(), out var budgetCategoryId))
                    errors.Add($"Budget category '{categoryName}' was not found");

                var planned = ParseAmount(row.GetValueOrDefault("Planned"), errors, "Planned");
                var committed = ParseAmount(row.GetValueOrDefault("Committed"), errors, "Committed");
                var disbursed = ParseAmount(row.GetValueOrDefault("Disbursed"), errors, "Disbursed");
                var expended = ParseAmount(row.GetValueOrDefault("Expended"), errors, "Expended");

                var currency = string.IsNullOrWhiteSpace(row.GetValueOrDefault("Currency"))
                    ? baseCurrency
                    : row.GetValueOrDefault("Currency").Trim().ToUpperInvariant();

                if (currency.Length != 3)
                    errors.Add($"Currency '{currency}' must be a 3-letter ISO code");

                var periodStart = ParseDate(row.GetValueOrDefault("PeriodStart"), errors, "PeriodStart");
                var periodEnd = ParseDate(row.GetValueOrDefault("PeriodEnd"), errors, "PeriodEnd");

                if (periodStart.HasValue && periodEnd.HasValue && periodEnd < periodStart)
                    errors.Add("PeriodEnd is before PeriodStart");

                report.Rows.Add(new BudgetImportRowReport
                {
                    RowNumber = rowNumber,
                    Project = projectName,
                    Category = categoryName,
                    Description = row.GetValueOrDefault("Description"),
                    Planned = planned,
                    Committed = committed,
                    Disbursed = disbursed,
                    Expended = expended,
                    Currency = currency,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    Errors = errors
                });

                if (errors.Count == 0) report.ValidCount++;
                else report.ErrorCount++;
            }

            return report;
        }

        /// <summary>Commits the valid rows of a validated import as live BudgetLines (audited). userId may be null for automated pushes.</summary>
        public static async System.Threading.Tasks.Task CommitBudgetAsync(
            ApplicationDbContext db, ImportJob importJob, BudgetImportReport report, Guid? userId)
        {
            var validRows = report.Rows.Where(o => o.Errors.Count == 0).ToList();

            var projects = await db.Projects.ToDictionaryAsync(o => o.Name.Trim().ToLowerInvariant(), o => o.ProjectId);
            var categories = await db.BudgetCategories.ToDictionaryAsync(o => o.Name.Trim().ToLowerInvariant(), o => o.BudgetCategoryId);

            foreach (var row in validRows)
            {
                var budgetLine = new BudgetLine
                {
                    ProjectId = projects[row.Project.Trim().ToLowerInvariant()],
                    BudgetCategoryId = categories[row.Category.Trim().ToLowerInvariant()],
                    Description = row.Description,
                    PlannedAmount = row.Planned ?? 0m,
                    CommittedAmount = row.Committed ?? 0m,
                    DisbursedAmount = row.Disbursed ?? 0m,
                    ActualAmount = row.Expended ?? 0m,
                    Currency = row.Currency,
                    PeriodStart = row.PeriodStart,
                    PeriodEnd = row.PeriodEnd,
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedById = userId
                };

                db.BudgetLines.Add(budgetLine);
            }

            await db.SaveChangesAsync();

            AuditHelper.Write(db, null, null, "ImportJob", importJob.ImportJobId,
                "Commit", null, new { importJob.ImportType, validRows = validRows.Count });
            await db.SaveChangesAsync();
        }

        private static decimal? ParseAmount(string value, List<string> errors, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                errors.Add($"{fieldName} '{value}' is not a valid number");
                return null;
            }

            if (amount < 0)
            {
                errors.Add($"{fieldName} must not be negative");
                return null;
            }

            return amount;
        }

        private static DateTime? ParseDate(string value, List<string> errors, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                errors.Add($"{fieldName} '{value}' is not a valid date (expected yyyy-mm-dd)");
                return null;
            }

            return date.Date;
        }
    }

    public class BudgetImportReport
    {
        public int ValidCount { get; set; }
        public int ErrorCount { get; set; }
        public List<BudgetImportRowReport> Rows { get; set; } = new List<BudgetImportRowReport>();
    }

    public class BudgetImportRowReport
    {
        public int RowNumber { get; set; }
        public string Project { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal? Planned { get; set; }
        public decimal? Committed { get; set; }
        public decimal? Disbursed { get; set; }
        public decimal? Expended { get; set; }
        public string Currency { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class RecordsTemplate
    {
        public string FileName { get; set; }
        public string Contents { get; set; }
        public string[] Columns { get; set; }
    }

    public class RecordsImportReport
    {
        public int ValidCount { get; set; }
        public int ErrorCount { get; set; }
        public List<RecordsImportRowReport> Rows { get; set; } = new List<RecordsImportRowReport>();
    }

    public class RecordsImportRowReport
    {
        public int RowNumber { get; set; }
        public string EntityCode { get; set; }
        public string DateCode { get; set; }
        public string ResponseCode { get; set; }
        public int AnswersCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>Raw cell values keyed by column (serialized so commit can replay them).</summary>
        public Dictionary<string, string> RawCells { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
