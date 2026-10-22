using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    /// <summary>
    /// An in-app data import (ETL Tier 1): a file uploaded, mapped, validated and — after user
    /// confirmation — committed into the system. Committed rows enter the Submit→Verify→Approve
    /// governance workflow (imported data is never auto-approved).
    /// </summary>
    public class ImportJob
    {
        [Key, Required]
        public Guid ImportJobId { get; set; }

        [Required]
        public ImportType ImportType { get; set; }

        [Required]
        public ImportStatus Status { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(250)]
        public string FileName { get; set; }

        /// <summary>Polymporphic commit target: QuestionnaireId for Records, ProjectId for Budget, IndicatorId-set parent for IndicatorValues.</summary>
        public Guid? TargetId { get; set; }

        /// <summary>Column→field mapping configuration JSON (user-confirmed in the mapping UI).</summary>
        public string MappingConfigJson { get; set; }

        /// <summary>Row-level validation report JSON produced by the validation step.</summary>
        public string ValidationReportJson { get; set; }

        [Required]
        public int TotalRows { get; set; }

        [Required]
        public int ValidRows { get; set; }

        [Required]
        public int ErrorRows { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public DateTime? CommittedOnUtc { get; set; }

        public Guid? CommittedById { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        [ForeignKey("CommittedById")]
        public virtual User CommittedBy { get; set; }

        public virtual ImportJobContent ImportJobContent { get; set; }

        public ImportJob()
        {
            ImportJobId = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
            Status = ImportStatus.Uploaded;
        }

        public override string ToString()
        {
            return FileName;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            ImportJob other = (ImportJob)obj;

            return ImportJobId == other.ImportJobId;
        }

        public override int GetHashCode()
        {
            return ImportJobId.GetHashCode();
        }
    }

    /// <summary>Binary contents of an import file, kept separate per the Document/DocumentContent pattern.</summary>
    public class ImportJobContent
    {
        [Key, Required, ForeignKey("ImportJob")]
        public Guid ImportJobId { get; set; }

        public byte[] FileContents { get; set; }

        [Required]
        public virtual ImportJob ImportJob { get; set; }
    }
}
