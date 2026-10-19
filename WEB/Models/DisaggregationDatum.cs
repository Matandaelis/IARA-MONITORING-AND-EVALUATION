using System;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class DisaggregationDatum
    {
        [Key]
        public Guid DisaggregationDatumId { get; set; }

        [Required]
        public Guid DatumId { get; set; }

        [Required]
        public Guid DisaggregationOptionId { get; set; }

        [Required]
        public decimal Value { get; set; }

        // Navigation
        public virtual Datum Datum { get; set; }

        public virtual DisaggregationOption DisaggregationOption { get; set; }
    }
}
