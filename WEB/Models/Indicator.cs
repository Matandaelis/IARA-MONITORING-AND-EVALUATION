using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public partial class Indicator
    {
        [Key, Required]
        public Guid IndicatorId { get; set; }

        [Required]
        public Guid SubcategoryId { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(500)]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(20)]
        public string Code { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(100)]
        public string Units { get; set; }

        [Required]
        public IndicatorType IndicatorType { get; set; }

        [Required]
        public IndicatorStatus IndicatorStatus { get; set; }

        [Required]
        public bool RequiresSubmit { get; set; }

        [Required]
        public bool RequiresVerify { get; set; }

        [Required]
        public bool RequiresApprove { get; set; }

        [Required]
        public bool LowerIsBetter { get; set; }

        [Required]
        public bool UseIndicatorDates { get; set; }

        [Required]
        public bool DisableNote { get; set; }

        [Required]
        public Guid EntityTypeId { get; set; }

        public virtual ICollection<IndicatorTag> IndicatorTags { get; set; } = new List<IndicatorTag>();

        [Required]
        public DateType Frequency { get; set; }

        [Required]
        public AggregationType DateAggregationType { get; set; }

        [Required]
        public EntityAggregationType EntityAggregationType { get; set; } = EntityAggregationType.Sum;

        [Column(TypeName = "decimal(20, 8)")]
        public decimal? Baseline { get; set; }

        [Required]
        public DataType DataType { get; set; }


        public Guid? OptionListId { get; set; }

        [Required]
        public byte DecimalPlaces { get; set; }

        [Column(TypeName = "decimal(20, 8)")]
        public decimal? Minimum { get; set; }

        [Column(TypeName = "decimal(20, 8)")]
        public decimal? Maximum { get; set; }

        [MaxLength(250)]
        public string ShortName { get; set; }

        [MaxLength(50)]
        public string Label { get; set; }

        [MaxLength(7)]
        public string Color { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(10)]
        public string Prefix { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(10)]
        public string Suffix { get; set; }

        public Guid? GroupingIndicatorId { get; set; }

        [Required]
        public DateTime CreatedDateUtc { get; set; }

        [Required]
        public Guid CreatedById { get; set; }

        [Required]
        public DateTime LastSavedDateUtc { get; set; }

        [Required]
        public Guid LastSavedById { get; set; }

        [Required]
        public int SortOrder { get; set; }

        public virtual ICollection<Token> Tokens { get; set; } = new List<Token>();

        public virtual ICollection<Datum> Data { get; set; } = new List<Datum>();

        public virtual ICollection<Token> SourceTokens { get; set; } = new List<Token>();

        public virtual ICollection<IndicatorPermission> IndicatorPermissions { get; set; } = new List<IndicatorPermission>();

        public virtual ICollection<LogFrameRowIndicator> LogFrameRowIndicators { get; set; } = new List<LogFrameRowIndicator>();

        public virtual ICollection<ComponentIndicator> ComponentIndicators { get; set; } = new List<ComponentIndicator>();

        public virtual ICollection<Indicator> GroupIndicators { get; set; } = new List<Indicator>();

        public virtual ICollection<IndicatorDate> IndicatorDates { get; set; } = new List<IndicatorDate>();

        public Guid? DisaggregationGroupId { get; set; }

        /// <summary>Primary monitoring domain (process, compliance, context, financial, assumption, results, impact…).</summary>
        [Required]
        public MonitoringType MonitoringType { get; set; } = MonitoringType.Results;

        /// <summary>The person accountable for collecting and reporting this indicator.</summary>
        public Guid? ResponsibleUserId { get; set; }

        /// <summary>Optional per-indicator RAG override: achievement % at/above which the status is Green.</summary>
        [Column(TypeName = "decimal(9, 2)")]
        public decimal? GreenThreshold { get; set; }

        /// <summary>Optional per-indicator RAG override: achievement % at/above which the status is Amber.</summary>
        [Column(TypeName = "decimal(9, 2)")]
        public decimal? AmberThreshold { get; set; }
        /// <summary>PIRS: Operational Definition (Plain-English narrative explaining how the metric is calculated and measured).</summary>
        public string OperationalDefinition { get; set; }

        /// <summary>PIRS: Purpose and Strategic Rationale for tracking this indicator.</summary>
        public string PurposeRationale { get; set; }

        /// <summary>PIRS: Data Quality Protocol (Verification procedures, calibration, error tolerance margin).</summary>
        public string DataQualityProtocol { get; set; }

        /// <summary>PIRS: Reporting Schedule (Internal vs donor reporting timeline and cadence).</summary>
        [MaxLength(250)]
        public string ReportingSchedule { get; set; }

        /// <summary>PIRS: Mathematical calculation narrative.</summary>
        public string CalculationFormulaNarrative { get; set; }

        /// <summary>Optional first-class numerator indicator reference for ratio metrics.</summary>
        public Guid? NumeratorIndicatorId { get; set; }

        /// <summary>Optional first-class denominator indicator reference for ratio metrics.</summary>
        public Guid? DenominatorIndicatorId { get; set; }

        [ForeignKey("NumeratorIndicatorId")]
        public virtual Indicator NumeratorIndicator { get; set; }

        [ForeignKey("DenominatorIndicatorId")]
        public virtual Indicator DenominatorIndicator { get; set; }

        [ForeignKey("ResponsibleUserId")]
        public virtual User ResponsibleUser { get; set; }

        public IndicatorDirection Direction
        {
            get => LowerIsBetter ? IndicatorDirection.LowerIsBetter : IndicatorDirection.HigherIsBetter;
            set => LowerIsBetter = (value == IndicatorDirection.LowerIsBetter);
        }

        public virtual DisaggregationGroup DisaggregationGroup { get; set; }

        public virtual ICollection<IndicatorTarget> IndicatorTargets { get; set; } = new List<IndicatorTarget>();

        [ForeignKey("EntityTypeId")]
        public virtual EntityType EntityType { get; set; }

        [ForeignKey("GroupingIndicatorId")]
        public virtual Indicator GroupingIndicator { get; set; }

        [ForeignKey("OptionListId")]
        public virtual OptionList OptionList { get; set; }

        [ForeignKey("SubcategoryId")]
        public virtual Subcategory Subcategory { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public Indicator()
        {
            IndicatorId = Guid.NewGuid();
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            Indicator other = (Indicator)obj;

            return IndicatorId == other.IndicatorId;
        }

        public override int GetHashCode()
        {
            return IndicatorId.GetHashCode();
        }
    }
}
