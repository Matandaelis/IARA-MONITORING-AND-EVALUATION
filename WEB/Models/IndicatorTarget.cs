using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class IndicatorTarget
    {
        [Key]
        public Guid IndicatorTargetId { get; set; }

        [Required]
        public Guid IndicatorId { get; set; }

        [Required]
        public Guid DateId { get; set; }

        public Guid? EntityId { get; set; }

        [Required]
        [Column(TypeName = "decimal(20, 8)")]
        public decimal TargetValue { get; set; }

        [Column(TypeName = "decimal(20, 8)")]
        public decimal? BaselineValue { get; set; }

        [MaxLength(1000)]
        public string Notes { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("IndicatorId")]
        public virtual Indicator Indicator { get; set; }

        [ForeignKey("DateId")]
        public virtual Date Date { get; set; }

        [ForeignKey("EntityId")]
        public virtual Entity Entity { get; set; }
    }
}
