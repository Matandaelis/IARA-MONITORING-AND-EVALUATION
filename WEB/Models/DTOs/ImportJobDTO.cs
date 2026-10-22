using System;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class ImportSearchOptions : SearchOptions
    {
        public ImportType? ImportType { get; set; }

        public ImportStatus? Status { get; set; }
    }

    public class ImportJobDTO
    {
        [Required]
        public Guid ImportJobId { get; set; }

        [Required]
        public ImportType ImportType { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(250)]
        public string FileName { get; set; }

        public Guid? TargetId { get; set; }

        /// <summary>Base64-encoded file contents (upload only, never returned).</summary>
        public string FileContents { get; set; }

        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public ImportStatus Status { get; set; }
        public DateTime? CreatedOnUtc { get; set; }
        public UserDTO CreatedBy { get; set; }
        public DateTime? CommittedOnUtc { get; set; }
        public UserDTO CommittedBy { get; set; }

        /// <summary>Validation report rows (returned by Get, upload-only on save).</summary>
        public Code.BudgetImportReport ValidationReport { get; set; }

        /// <summary>Record imports carry a records-shaped report instead.</summary>
        public Code.RecordsImportReport RecordsValidationReport { get; set; }
    }

    public static partial class ModelFactory
    {
        public static ImportJobDTO Create(ImportJob importJob)
        {
            if (importJob == null) return null;

            var dto = new ImportJobDTO
            {
                ImportJobId = importJob.ImportJobId,
                ImportType = importJob.ImportType,
                FileName = importJob.FileName,
                TargetId = importJob.TargetId,
                TotalRows = importJob.TotalRows,
                ValidRows = importJob.ValidRows,
                ErrorRows = importJob.ErrorRows,
                Status = importJob.Status,
                CreatedOnUtc = importJob.CreatedOnUtc,
                CreatedBy = Create(importJob.CreatedBy),
                CommittedOnUtc = importJob.CommittedOnUtc,
                CommittedBy = Create(importJob.CommittedBy)
            };

            if (!string.IsNullOrEmpty(importJob.ValidationReportJson))
            {
                try
                {
                    if (importJob.ImportType == ImportType.Records)
                        dto.RecordsValidationReport = Newtonsoft.Json.JsonConvert.DeserializeObject<Code.RecordsImportReport>(importJob.ValidationReportJson);
                    else
                        dto.ValidationReport = Newtonsoft.Json.JsonConvert.DeserializeObject<Code.BudgetImportReport>(importJob.ValidationReportJson);
                }
                catch { /* report is informational */ }
            }

            return dto;
        }
    }
}
