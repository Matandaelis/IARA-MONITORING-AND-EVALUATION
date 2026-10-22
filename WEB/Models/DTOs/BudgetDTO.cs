using System;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class BudgetCategoryDTO
    {
        [Required]
        public Guid BudgetCategoryId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }

        public int SortOrder { get; set; }

        public DateTime? CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public UserDTO CreatedBy { get; set; }

        public int LineCount { get; set; }
    }

    public class BudgetLineDTO
    {
        [Required]
        public Guid BudgetLineId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public Guid BudgetCategoryId { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }

        public decimal PlannedAmount { get; set; }

        public decimal CommittedAmount { get; set; }

        public decimal DisbursedAmount { get; set; }

        public decimal ActualAmount { get; set; }

        [MaxLength(3), MinLength(3)]
        public string Currency { get; set; }

        public Guid? DateId { get; set; }

        public Guid? TaskId { get; set; }

        public DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        public DateTime? CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public DateTime? ModifiedOnUtc { get; set; }

        public Guid? ModifiedById { get; set; }

        public ProjectDTO Project { get; set; }

        public BudgetCategoryDTO BudgetCategory { get; set; }

        public DateDTO Date { get; set; }
    }

    /// <summary>Aggregated 4-ledger figures for one category within a project summary.</summary>
    public class BudgetSummaryRowDTO
    {
        [Required]
        public Guid BudgetCategoryId { get; set; }

        [Required]
        public string CategoryName { get; set; }

        public decimal Planned { get; set; }

        public decimal Committed { get; set; }

        public decimal Disbursed { get; set; }

        public decimal Actual { get; set; }

        public decimal Variance => Planned - Actual;

        public double Utilization => Planned == 0 ? 0 : Math.Round((double)(Actual / Planned) * 100, 1);

        public int LineCount { get; set; }

        public string Currency { get; set; }
    }

    /// <summary>Wrapper for the summary endpoint: converted totals plus currencies lacking an FX rate.</summary>
    public class BudgetSummaryResponseDTO
    {
        [Required]
        public string BaseCurrency { get; set; }

        public List<BudgetSummaryRowDTO> Rows { get; set; } = new List<BudgetSummaryRowDTO>();

        /// <summary>Currencies present on budget lines with no FX rate to the base currency — excluded from base-currency totals.</summary>
        public List<string> MissingRates { get; set; } = new List<string>();
    }

    /// <summary>One point on the burn/S-curve: planned vs expended for a reporting period.</summary>
    public class BudgetBurnPointDTO
    {
        [Required]
        public Guid DateId { get; set; }

        public string Label { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal Planned { get; set; }

        public decimal Expended { get; set; }

        public decimal CumulativeExpended { get; set; }
    }

    /// <summary>Cost-per-outcome: expended ÷ latest approved indicator actual.</summary>
    public class BudgetCostPerOutcomeDTO
    {
        [Required]
        public Guid IndicatorId { get; set; }

        public string IndicatorCode { get; set; }

        public string IndicatorName { get; set; }

        public decimal Expended { get; set; }

        public decimal? OutcomeValue { get; set; }

        public DateTime? OutcomeDateUtc { get; set; }

        public decimal? CostPerOutcome { get; set; }

        public string Currency { get; set; }
    }

    public static partial class ModelFactory
    {
        public static BudgetCategoryDTO Create(BudgetCategory budgetCategory, bool includeParents = true)
        {
            if (budgetCategory == null) return null;

            var dto = new BudgetCategoryDTO
            {
                BudgetCategoryId = budgetCategory.BudgetCategoryId,
                Name = budgetCategory.Name,
                Description = budgetCategory.Description,
                SortOrder = budgetCategory.SortOrder,
                CreatedOnUtc = budgetCategory.CreatedOnUtc,
                CreatedById = budgetCategory.CreatedById,
                LineCount = budgetCategory.BudgetLines?.Count ?? 0
            };

            if (includeParents)
            {
                dto.CreatedBy = Create(budgetCategory.CreatedBy);
            }

            return dto;
        }

        public static void Hydrate(BudgetCategory budgetCategory, BudgetCategoryDTO budgetCategoryDTO)
        {
            budgetCategory.Name = budgetCategoryDTO.Name;
            budgetCategory.Description = budgetCategoryDTO.Description;
            budgetCategory.SortOrder = budgetCategoryDTO.SortOrder;
        }

        public static BudgetLineDTO Create(BudgetLine budgetLine, bool includeParents = true)
        {
            if (budgetLine == null) return null;

            var dto = new BudgetLineDTO
            {
                BudgetLineId = budgetLine.BudgetLineId,
                ProjectId = budgetLine.ProjectId,
                BudgetCategoryId = budgetLine.BudgetCategoryId,
                Description = budgetLine.Description,
                PlannedAmount = budgetLine.PlannedAmount,
                CommittedAmount = budgetLine.CommittedAmount,
                DisbursedAmount = budgetLine.DisbursedAmount,
                ActualAmount = budgetLine.ActualAmount,
                Currency = budgetLine.Currency,
                DateId = budgetLine.DateId,
                TaskId = budgetLine.TaskId,
                PeriodStart = budgetLine.PeriodStart,
                PeriodEnd = budgetLine.PeriodEnd,
                CreatedOnUtc = budgetLine.CreatedOnUtc,
                CreatedById = budgetLine.CreatedById,
                ModifiedOnUtc = budgetLine.ModifiedOnUtc,
                ModifiedById = budgetLine.ModifiedById
            };

            if (includeParents)
            {
                dto.Project = Create(budgetLine.Project);
                dto.BudgetCategory = Create(budgetLine.BudgetCategory);
            }

            return dto;
        }

        public static void Hydrate(BudgetLine budgetLine, BudgetLineDTO budgetLineDTO)
        {
            budgetLine.ProjectId = budgetLineDTO.ProjectId;
            budgetLine.BudgetCategoryId = budgetLineDTO.BudgetCategoryId;
            budgetLine.Description = budgetLineDTO.Description;
            budgetLine.PlannedAmount = budgetLineDTO.PlannedAmount;
            budgetLine.CommittedAmount = budgetLineDTO.CommittedAmount;
            budgetLine.DisbursedAmount = budgetLineDTO.DisbursedAmount;
            budgetLine.ActualAmount = budgetLineDTO.ActualAmount;
            budgetLine.Currency = string.IsNullOrWhiteSpace(budgetLineDTO.Currency) ? "USD" : budgetLineDTO.Currency;
            budgetLine.DateId = budgetLineDTO.DateId;
            budgetLine.TaskId = budgetLineDTO.TaskId;
            budgetLine.PeriodStart = budgetLineDTO.PeriodStart;
            budgetLine.PeriodEnd = budgetLineDTO.PeriodEnd;
        }
    }
}
