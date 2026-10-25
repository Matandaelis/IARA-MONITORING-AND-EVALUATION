using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    /// <summary>
    /// An expense category used to classify budget lines (e.g. Staff, Travel, Equipment).
    /// </summary>
    public class BudgetCategory
    {
        [Key, Required]
        public Guid BudgetCategoryId { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }

        [Required]
        public int SortOrder { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public virtual ICollection<BudgetLine> BudgetLines { get; set; } = new List<BudgetLine>();

        public BudgetCategory()
        {
            BudgetCategoryId = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            BudgetCategory other = (BudgetCategory)obj;

            return BudgetCategoryId == other.BudgetCategoryId;
        }

        public override int GetHashCode()
        {
            return BudgetCategoryId.GetHashCode();
        }
    }

    /// <summary>
    /// A planned and/or actual amount against a project and budget category, for an optional period.
    /// </summary>
    public class BudgetLine
    {
        [Key, Required]
        public Guid BudgetLineId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public Guid BudgetCategoryId { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PlannedAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CommittedAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DisbursedAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ActualAmount { get; set; }

        [MaxLength(3), MinLength(3)]
        public string Currency { get; set; }

        /// <summary>Optional reporting-period alignment (Dates table) for burn-rate analysis next to indicator data.</summary>
        public Guid? DateId { get; set; }

        /// <summary>Optional activity (Task) this budget line belongs to, for activity-level budgeting.</summary>
        public Guid? TaskId { get; set; }

        /// <summary>Optional logframe result this budget line is delivering — enables cost-per-output analysis.</summary>
        public Guid? LogFrameNodeId { get; set; }

        /// <summary>Optional indicator this budget line is delivering — enables cost-per-unit analysis.</summary>
        public Guid? IndicatorId { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? PeriodStart { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? PeriodEnd { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public DateTime? ModifiedOnUtc { get; set; }

        public Guid? ModifiedById { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [ForeignKey("BudgetCategoryId")]
        public virtual BudgetCategory BudgetCategory { get; set; }

        [ForeignKey("DateId")]
        public virtual Date Date { get; set; }

        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }

        [ForeignKey("LogFrameNodeId")]
        public virtual LogFrameNode LogFrameNode { get; set; }

        [ForeignKey("IndicatorId")]
        public virtual Indicator Indicator { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User ModifiedBy { get; set; }

        public BudgetLine()
        {
            BudgetLineId = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
            Currency = "USD";
        }

        public override string ToString()
        {
            return Description ?? BudgetLineId.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            BudgetLine other = (BudgetLine)obj;

            return BudgetLineId == other.BudgetLineId;
        }

        public override int GetHashCode()
        {
            return BudgetLineId.GetHashCode();
        }
    }

    /// <summary>
    /// Exchange rate between two currencies, effective from a date. Used to convert budget lines
    /// to the organisation's base currency (Settings.BaseCurrency) for roll-ups.
    /// </summary>
    public class FxRate
    {
        [Key, Required]
        public Guid FxRateId { get; set; }

        [Required, MaxLength(3), MinLength(3)]
        public string FromCurrency { get; set; }

        [Required, MaxLength(3), MinLength(3)]
        public string ToCurrency { get; set; }

        [Column(TypeName = "decimal(18, 8)")]
        public decimal Rate { get; set; }

        [Column(TypeName = "Date")]
        public DateTime EffectiveFrom { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public FxRate()
        {
            FxRateId = Guid.NewGuid();
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"{FromCurrency}/{ToCurrency} {Rate} @ {EffectiveFrom:yyyy-MM-dd}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            FxRate other = (FxRate)obj;

            return FxRateId == other.FxRateId;
        }

        public override int GetHashCode()
        {
            return FxRateId.GetHashCode();
        }
    }
}
