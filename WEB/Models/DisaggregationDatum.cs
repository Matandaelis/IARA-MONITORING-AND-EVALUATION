using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class DisaggregationDatum
    {
        [Key]
        public Guid DisaggregationDatumId { get; set; }

        public Guid? DatumId { get; set; }

        public Guid IndicatorId { get; set; }

        public Guid EntityId { get; set; }

        public Guid DateId { get; set; }

        [Required]
        public Guid DisaggregationOptionId { get; set; }

        [Required]
        [Column(TypeName = "decimal(20, 8)")]
        public decimal Value { get; set; }

        // Navigation
        public virtual Datum Datum { get; set; }

        public virtual DisaggregationOption DisaggregationOption { get; set; }
    }
}
