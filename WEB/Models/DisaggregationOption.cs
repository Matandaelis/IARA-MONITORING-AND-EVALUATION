using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class DisaggregationOption
    {
        [Key]
        public Guid DisaggregationOptionId { get; set; }

        [Required]
        public Guid DisaggregationGroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Code { get; set; }

        public int SortOrder { get; set; } = 0;

        // Navigation
        public virtual DisaggregationGroup DisaggregationGroup { get; set; }

        public virtual ICollection<DisaggregationDatum> DisaggregationData { get; set; } = new List<DisaggregationDatum>();
    }
}
