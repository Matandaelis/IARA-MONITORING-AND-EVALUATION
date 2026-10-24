using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monic.Web.Models;
using static Monic.Web.Code.ImportCSV;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class DownloadsController : BaseApiController
    {
        public DownloadsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings) : base(dbFactory, um, appSettings) { }

        [HttpGet("documents/{documentId:Guid}")]
        public async Task<IActionResult> DownloadDocument(Guid documentId)
        {
            var document = await db.Documents
                .Include(o => o.DocumentContent)
                .FirstOrDefaultAsync(o => o.DocumentId == documentId);
            if (document == null) return NotFound();

            var item = await db.Items.FirstOrDefaultAsync(o => o.ItemId == document.ItemId);

            if (item.ItemType == ItemType.Folder)
            {
                if (!CurrentUser.IsInRole(Roles.Folders)) return Forbid();
            }
            else
            {
                // todo: check other permissions here...
                if (!CurrentUser.IsInRole(Roles.Administrator)) return Forbid();
            }

            return Download.GetFileContentResult(Response, document.FileName, document.DocumentContent.FileContents);
        }

        [HttpPost("export/csv"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> ExportCSV([FromBody] ExportCSVDTO exportCSVDTO)
        {
            var data = await db.Data
                .Where(o => exportCSVDTO.IndicatorIds.Length == 0 || exportCSVDTO.IndicatorIds.Contains(o.IndicatorId))
                .Where(o => exportCSVDTO.EntityIds.Length == 0 || exportCSVDTO.EntityIds.Contains(o.EntityId))
                .Where(o => exportCSVDTO.DateIds.Length == 0 || exportCSVDTO.DateIds.Contains(o.DateId))
                .Select(o =>
                    new CSVRow
                    {
                        IndicatorCode = o.Indicator.Code,
                        EntityCode = o.Entity.Code,
                        DateCode = o.Date.Code,
                        Value = o.Value,
                        Note = o.Note
                    }
                )
                .ToListAsync();

            byte[] contents = null;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                TrimOptions = TrimOptions.Trim
            };

            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var csv = new CsvWriter(streamWriter, config))
            {
                csv.WriteRecords(data);

                csv.Flush();
                streamWriter.Flush();
                contents = memoryStream.ToArray();
            }

            return Download.GetFileContentResult(Response, "MonicDataExport.csv", contents);
        }

        public class ExportCSVDTO
        {
            public Guid[] IndicatorIds { get; set; }
            public Guid[] EntityIds { get; set; }
            public Guid[] DateIds { get; set; }
        }

        [HttpPost("export/variance-csv"), AuthorizeRoles(Roles.Administrator, Roles.Manager)]
        public async Task<IActionResult> ExportVarianceCSV([FromBody] ExportCSVDTO exportCSVDTO)
        {
            var targets = await db.IndicatorTargets
                .Include(t => t.Indicator)
                .Include(t => t.Date)
                .Include(t => t.Entity)
                .Where(t => exportCSVDTO.IndicatorIds == null || exportCSVDTO.IndicatorIds.Length == 0 || exportCSVDTO.IndicatorIds.Contains(t.IndicatorId))
                .Where(t => exportCSVDTO.EntityIds == null || exportCSVDTO.EntityIds.Length == 0 || (t.EntityId.HasValue && exportCSVDTO.EntityIds.Contains(t.EntityId.Value)))
                .Where(t => exportCSVDTO.DateIds == null || exportCSVDTO.DateIds.Length == 0 || exportCSVDTO.DateIds.Contains(t.DateId))
                .ToListAsync();

            var data = await db.Data
                .Include(d => d.DisaggregationData)
                .ThenInclude(dd => dd.DisaggregationOption)
                .Where(d => exportCSVDTO.IndicatorIds == null || exportCSVDTO.IndicatorIds.Length == 0 || exportCSVDTO.IndicatorIds.Contains(d.IndicatorId))
                .Where(d => exportCSVDTO.EntityIds == null || exportCSVDTO.EntityIds.Length == 0 || exportCSVDTO.EntityIds.Contains(d.EntityId))
                .Where(d => exportCSVDTO.DateIds == null || exportCSVDTO.DateIds.Length == 0 || exportCSVDTO.DateIds.Contains(d.DateId))
                .ToListAsync();

            var rows = new List<VarianceCSVRow>();
            foreach (var target in targets)
            {
                var datum = data.FirstOrDefault(d => d.IndicatorId == target.IndicatorId && d.DateId == target.DateId && (target.EntityId == null || d.EntityId == target.EntityId));
                var variance = Code.VarianceEngine.CalculateVariance(datum?.Value, target.TargetValue, target.Indicator.Direction);

                string disaggregationStr = "";
                if (datum?.DisaggregationData != null && datum.DisaggregationData.Count > 0)
                {
                    disaggregationStr = string.Join("; ", datum.DisaggregationData.Select(dd => $"{dd.DisaggregationOption?.Name}: {dd.Value}"));
                }

                rows.Add(new VarianceCSVRow
                {
                    IndicatorCode = target.Indicator?.Code,
                    IndicatorName = target.Indicator?.Name,
                    PeriodCode = target.Date?.Code,
                    EntityCode = target.Entity?.Code ?? "All",
                    TargetValue = target.TargetValue,
                    ActualValue = datum?.Value,
                    PercentageAchievement = variance.PercentageAchievement,
                    StatusBadge = variance.StatusBadge,
                    DisaggregationBreakdown = disaggregationStr
                });
            }

            byte[] contents = null;
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                TrimOptions = TrimOptions.Trim
            };

            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            using (var csv = new CsvWriter(streamWriter, config))
            {
                csv.WriteRecords(rows);
                csv.Flush();
                streamWriter.Flush();
                contents = memoryStream.ToArray();
            }

            return Download.GetFileContentResult(Response, "MonicVarianceExport.csv", contents);
        }

        public class VarianceCSVRow
        {
            public string IndicatorCode { get; set; }
            public string IndicatorName { get; set; }
            public string PeriodCode { get; set; }
            public string EntityCode { get; set; }
            public decimal? TargetValue { get; set; }
            public decimal? ActualValue { get; set; }
            public decimal? PercentageAchievement { get; set; }
            public string StatusBadge { get; set; }
            public string DisaggregationBreakdown { get; set; }
        }

        [HttpGet("logframe/{logFrameId:Guid}/export-csv"), Authorize]
        public async Task<IActionResult> ExportLogFrameCSV(Guid logFrameId)
        {
            var logFrame = await db.LogFrames
                .Include(lf => lf.LogFrameRows)
                    .ThenInclude(r => r.LogFrameRowIndicators)
                        .ThenInclude(ri => ri.Indicator)
                            .ThenInclude(i => i.IndicatorTargets)
                                .ThenInclude(t => t.Date)
                .Include(lf => lf.LogFrameRows)
                    .ThenInclude(r => r.LogFrameRowIndicators)
                        .ThenInclude(ri => ri.Indicator)
                            .ThenInclude(i => i.Data)
                                .ThenInclude(d => d.Date)
                .FirstOrDefaultAsync(lf => lf.LogFrameId == logFrameId);

            if (logFrame == null) return NotFound();

            var rollup = Code.LogFrameRollupEngine.ComputeRollup(logFrame);
            var bytes = Code.LogFrameRollupEngine.GenerateDonorCsv(rollup);

            var rawName = logFrame.Name?.Trim();
            var safeName = string.IsNullOrWhiteSpace(rawName)
                ? "LogFrame"
                : string.Join("_", rawName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

            return Download.GetFileContentResult(Response, $"{safeName}_Donor_Matrix.csv", bytes);
        }

        [HttpGet("projects/{projectId:Guid}/meplan-pdf"), Authorize]
        public async Task<IActionResult> DownloadMePlanPdf(Guid projectId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project == null) return NotFound();

            var (logFrame, indicators, caps, feedbacks) = await LoadProjectMePlanContextAsync(projectId);

            var bytes = Code.MePlanDocumentBuilder.GeneratePdf(project, logFrame, indicators, caps, feedbacks);
            var safeName = GetSafeProjectFileName(project.Name);

            return Download.GetFileContentResult(Response, $"{safeName}_MEAL_Plan.pdf", bytes);
        }

        [HttpGet("projects/{projectId:Guid}/meplan-excel"), Authorize]
        public async Task<IActionResult> DownloadMePlanExcel(Guid projectId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project == null) return NotFound();

            var (logFrame, indicators, caps, feedbacks) = await LoadProjectMePlanContextAsync(projectId);

            var bytes = Code.MePlanDocumentBuilder.GenerateExcel(project, logFrame, indicators, caps, feedbacks);
            var safeName = GetSafeProjectFileName(project.Name);

            return Download.GetFileContentResult(Response, $"{safeName}_MEAL_Plan.xlsx", bytes);
        }

        [HttpGet("projects/{projectId:Guid}/iati-xml"), Authorize]
        public async Task<IActionResult> DownloadIatiXml(Guid projectId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project == null) return NotFound();

            var (logFrame, indicators, _, _) = await LoadProjectMePlanContextAsync(projectId);

            var xmlString = Code.DonorExportService.GenerateIatiXml(project, logFrame, indicators);
            var bytes = Encoding.UTF8.GetBytes(xmlString);
            var safeName = GetSafeProjectFileName(project.Name);

            return Download.GetFileContentResult(Response, $"{safeName}_IATI_v203.xml", bytes);
        }

        [HttpGet("projects/{projectId:Guid}/usaid-qpr"), Authorize]
        public async Task<IActionResult> DownloadUsaidQpr(Guid projectId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project == null) return NotFound();

            var (logFrame, indicators, _, _) = await LoadProjectMePlanContextAsync(projectId);

            var bytes = Code.DonorExportService.GenerateUsaidQprExcel(project, logFrame, indicators);
            var safeName = GetSafeProjectFileName(project.Name);

            return Download.GetFileContentResult(Response, $"{safeName}_USAID_QPR_IPTT.xlsx", bytes);
        }

        [HttpGet("projects/{projectId:Guid}/eu-rom"), Authorize]
        public async Task<IActionResult> DownloadEuRom(Guid projectId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId);
            if (project == null) return NotFound();

            var (logFrame, indicators, _, _) = await LoadProjectMePlanContextAsync(projectId);

            var bytes = Code.DonorExportService.GenerateEuRomExport(project, logFrame, indicators);
            var safeName = GetSafeProjectFileName(project.Name);

            return Download.GetFileContentResult(Response, $"{safeName}_EU_ROM_Matrix.xlsx", bytes);
        }

        private async Task<(LogFrame logFrame, List<Indicator> indicators, List<MonitoringAction> caps, List<BeneficiaryFeedback> feedbacks)> LoadProjectMePlanContextAsync(Guid projectId)
        {
            var logFrame = await db.LogFrames
                .Include(lf => lf.TheoryOfChange)
                .Include(lf => lf.LogFrameRows)
                    .ThenInclude(r => r.LogFrameRowIndicators)
                        .ThenInclude(ri => ri.Indicator)
                            .ThenInclude(i => i.Data)
                .Include(lf => lf.LogFrameRows)
                    .ThenInclude(r => r.LogFrameRowRiskAssumptions)
                        .ThenInclude(ra => ra.RiskAssumption)
                .Include(lf => lf.LogFrameRows)
                    .ThenInclude(r => r.LogFrameRowTasks)
                        .ThenInclude(rt => rt.Task)
                .Include(lf => lf.LogFrameRows)
                    .ThenInclude(r => r.LogFrameRowBudgetLines)
                        .ThenInclude(rb => rb.BudgetLine)
                .FirstOrDefaultAsync(lf => lf.ProjectId == projectId);

            var indicators = logFrame?.LogFrameRows?
                .SelectMany(r => r.LogFrameRowIndicators.Select(ri => ri.Indicator))
                .Where(i => i != null)
                .Distinct()
                .ToList() ?? new List<Indicator>();

            var caps = await db.MonitoringActions
                .Where(a => a.ProjectId == projectId)
                .ToListAsync();

            var feedbacks = await db.BeneficiaryFeedbacks
                .Where(f => f.ProjectId == projectId)
                .ToListAsync();

            return (logFrame, indicators, caps, feedbacks);
        }

        private static string GetSafeProjectFileName(string projectName)
        {
            var raw = projectName?.Trim();
            return string.IsNullOrWhiteSpace(raw)
                ? "Project"
                : string.Join("_", raw.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        }
    }
}
