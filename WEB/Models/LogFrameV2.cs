using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models;

// v2 hierarchy. Keeps LogFrameRow for backward compatibility during migration.
public class LogFrameNode
{
    [Key] public Guid LogFrameNodeId { get; set; } = Guid.NewGuid();

    [Required] public Guid LogFrameId { get; set; }
    public Guid? ParentNodeId { get; set; }
    [Required] public Guid LogFrameLevelId { get; set; }

    [Required(AllowEmptyStrings = true), MaxLength(50)]
    public string Code { get; set; } = "";

    [Required(AllowEmptyStrings = true), MaxLength(500)]
    public string Title { get; set; } = "";

    public string Description { get; set; }
    public string MeansOfVerification { get; set; }
    public string RisksAndAssumptions { get; set; }

    [Required] public int SortOrder { get; set; }   // 1000, 2000, 3000 … gaps ease drag-drop
    [Required] public int Depth { get; set; }
    [Required] public bool IsActive { get; set; } = true;

    // Configurable fields for this level (validated against LogFrameLevel.RequiredFieldsJson)
    public string CustomFieldsJson { get; set; } = "{}";

    public Guid? LegacyLogFrameRowId { get; set; }  // traceability during backfill
    public Guid? InheritedFromNodeId { get; set; }  // corporate/global indicator reused in a project

    [Required] public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedById { get; set; }
    [Required] public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedById { get; set; }

    [Timestamp] public byte[] RowVersion { get; set; }

    public virtual LogFrameNode ParentNode { get; set; }
    public virtual ICollection<LogFrameNode> ChildNodes { get; set; } = new List<LogFrameNode>();
    public virtual LogFrame LogFrame { get; set; }
    public virtual LogFrameLevel Level { get; set; }
    public virtual ICollection<LogFrameNodeIndicator> Indicators { get; set; } = new List<LogFrameNodeIndicator>();
}

// Custom vocabulary: Goals/Outcomes/Outputs or Objectives/Results/Impacts — any number of levels.
public class LogFrameLevel
{
    [Key] public Guid LogFrameLevelId { get; set; } = Guid.NewGuid();

    public Guid? LogFrameId { get; set; }        // null = organization-wide template
    public Guid? OrganisationId { get; set; }    // scope for org-level vocabulary
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(100)] public string PluralName { get; set; } = "";
    [Required] public int Rank { get; set; }     // 1 = top
    [Required] public bool IsIndicatorLevel { get; set; }
    [MaxLength(7)] public string Color { get; set; }

    /// <summary>How this level aggregates child achievement into a level score.</summary>
    [Required] public RollupMethod RollupMethod { get; set; } = RollupMethod.WeightedAverage;

    // e.g. ["description","meansOfVerification","risksAndAssumptions"]
    public string RequiredFieldsJson { get; set; } = "[]";
    [Required] public bool IsActive { get; set; } = true;

    public virtual LogFrame LogFrame { get; set; }
    public virtual ICollection<LogFrameNode> Nodes { get; set; } = new List<LogFrameNode>();
}

public class LogFrameNodeIndicator
{
    [Required] public Guid LogFrameNodeId { get; set; }
    [Required] public Guid IndicatorId { get; set; }

    [Column(TypeName = "decimal(9,4)")] public decimal Weight { get; set; } = 1m;
    [Required] public bool IsPrimary { get; set; }
    [Required] public int SortOrder { get; set; }

    public virtual LogFrameNode Node { get; set; }
    public virtual Indicator Indicator { get; set; }
}
