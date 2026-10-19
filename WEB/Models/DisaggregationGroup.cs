using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class DisaggregationGroup
    {
        [Key]
        public Guid DisaggregationGroupId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<DisaggregationOption> Options { get; set; } = new List<DisaggregationOption>();
        public virtual ICollection<Indicator> Indicators { get; set; } = new List<Indicator>();
    }
}
