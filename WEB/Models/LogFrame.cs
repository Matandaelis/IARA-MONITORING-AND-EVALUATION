using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    public class LogFrame
    {
        [Key, Required]
        public Guid LogFrameId { get; set; }

        [Required(AllowEmptyStrings = true), MaxLength(255)]
        public string Name { get; set; }

        /// <summary>Project that owns this logical framework (null = shared/global framework).</summary>
        public Guid? ProjectId { get; set; }

        /// <summary>Theory of change that explains the causal logic behind this framework.</summary>
        public Guid? TheoryOfChangeId { get; set; }

        public virtual ICollection<LogFrameRow> LogFrameRows { get; set; } = new List<LogFrameRow>();

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [ForeignKey("TheoryOfChangeId")]
        public virtual TheoryOfChange TheoryOfChange { get; set; }

        public LogFrame()
        {
            LogFrameId = Guid.NewGuid();
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            LogFrame other = (LogFrame)obj;

            return LogFrameId == other.LogFrameId;
        }

        public override int GetHashCode()
        {
            return LogFrameId.GetHashCode();
        }
    }
}
