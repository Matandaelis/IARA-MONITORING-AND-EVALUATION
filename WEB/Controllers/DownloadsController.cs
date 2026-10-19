using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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

        //// if using Azure Blob Storage, use something like this to download the document:
        //[HttpGet("documents/{documentId:Guid}")]
        //public async Task<IActionResult> DownloadDocument([FromRoute] Guid documentId)
        //{
        //    var document = await db.Documents.FirstOrDefaultAsync(o => o.DocumentId == documentId);

        //    if (document == null) return NotFound();

        //    var blobStorageService = new BlobStorageService(AppSettings.AzureBlobStorage.ConnectionString, AppSettings.AzureBlobStorage.ConnectionString);
        //    var bytes = await blobStorageService.GetBlobAsync(documentId.ToString().ToLowerInvariant());

        //    return Download.GetFileContentResult(Response, document.FileName, bytes);
        //}

    }
}