using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class LogFrameRowRiskAssumption
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid LogFrameRowId { get; set; }

        [Required]
        public Guid RiskAssumptionId { get; set; }

        [ForeignKey("LogFrameRowId")]
        public virtual LogFrameRow LogFrameRow { get; set; }

        [ForeignKey("RiskAssumptionId")]
        public virtual RiskAssumption RiskAssumption { get; set; }
    }

    public class LogFrameRowTask
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid LogFrameRowId { get; set; }

        [Required]
        public Guid TaskId { get; set; }

        [ForeignKey("LogFrameRowId")]
        public virtual LogFrameRow LogFrameRow { get; set; }

        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }
    }

    public class LogFrameRowBudgetLine
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid LogFrameRowId { get; set; }

        [Required]
        public Guid BudgetLineId { get; set; }

        [ForeignKey("LogFrameRowId")]
        public virtual LogFrameRow LogFrameRow { get; set; }

        [ForeignKey("BudgetLineId")]
        public virtual BudgetLine BudgetLine { get; set; }
    }
}
