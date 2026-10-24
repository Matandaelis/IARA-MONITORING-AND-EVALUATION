using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class Project
    {
        [Key, Required]
        public Guid ProjectId { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(250)]
        public string Name { get; set; }

        [MaxLength(7)]
        public string Colour { get; set; }

        /// <summary>The organisational unit (Entity) that owns this project. Used for access scoping and roll-ups.</summary>
        public Guid? EntityId { get; set; }

        public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
        /// <summary>Primary sector/typology (Health, Education, Agriculture, WASH, Environment, Economic, Governance, Humanitarian, Infrastructure, etc.).</summary>
        [MaxLength(100)]
        public string Sector { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        public string CurrentPhase { get; set; }

        [MaxLength(250)]
        public string DonorName { get; set; }

        [MaxLength(100)]
        public string GrantCode { get; set; }

        [Column(TypeName = "decimal(20, 8)")]
        public decimal? TotalBudget { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        public long? TargetDirectBeneficiaries { get; set; }

        public long? TargetIndirectBeneficiaries { get; set; }

        /// <summary>Workflow mode: "Streamlined" (Entry -> Approve) vs "Rigorous" (Entry -> Verify -> Approve).</summary>
        [MaxLength(50)]
        public string WorkflowMode { get; set; } = "Rigorous";

        /// <summary>Custom display label for LogFrame Level 4 (Highest: e.g. Goal, Impact, Overall Objective).</summary>
        [MaxLength(100)]
        public string Level4Label { get; set; }

        /// <summary>Custom display label for LogFrame Level 3 (e.g. Outcome, Specific Objective, Strategic Objective).</summary>
        [MaxLength(100)]
        public string Level3Label { get; set; }

        /// <summary>Custom display label for LogFrame Level 2 (e.g. Output, Expected Result, Deliverable).</summary>
        [MaxLength(100)]
        public string Level2Label { get; set; }

        /// <summary>Custom display label for LogFrame Level 1 (Operational: e.g. Activity, Action, Input).</summary>
        [MaxLength(100)]
        public string Level1Label { get; set; }

        [ForeignKey("EntityId")]
        public virtual Entity Entity { get; set; }

        public Project()
        {
            ProjectId = Guid.NewGuid();
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            Project other = (Project)obj;

            return ProjectId == other.ProjectId;
        }

        public override int GetHashCode()
        {
            return ProjectId.GetHashCode();
        }
    }
}
