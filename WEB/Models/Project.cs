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
