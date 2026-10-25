using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models
{
    /// <summary>
    /// The ten monitoring domains described in the M&E framework. Every indicator is classified against one
    /// primary domain so dashboards can separate process monitoring from results/impact monitoring.
    /// </summary>
    public enum MonitoringType
    {
        Process = 0,
        Compliance = 1,
        Context = 2,
        Financial = 3,
        Administrative = 4,
        Assumption = 5,
        Organizational = 6,
        Results = 7,
        Technical = 8,
        Impact = 9
    }

    /// <summary>Traffic-light status used by the monitoring matrix and scorecards.</summary>
    public enum RagStatus
    {
        NotAssessed = 0,
        Red = 1,
        Amber = 2,
        Green = 3
    }

    /// <summary>How a logframe level aggregates the achievement of its child results.</summary>
    public enum RollupMethod
    {
        None = 0,
        Sum = 1,
        WeightedAverage = 2,
        MostRecent = 3
    }

    public enum RiskType
    {
        Risk = 0,
        Assumption = 1
    }

    public enum RiskStatus
    {
        Open = 0,
        Monitoring = 1,
        Mitigated = 2,
        Realized = 3,
        Closed = 4
    }

    public enum MonitoringEventType
    {
        FieldVisit = 0,
        Supervision = 1,
        DataQualityAudit = 2,
        ComplianceCheck = 3,
        StakeholderFeedback = 4,
        TechnicalReview = 5,
        ReviewMeeting = 6,
        Other = 7
    }

    public enum MonitoringEventStatus
    {
        Planned = 0,
        Completed = 1,
        Cancelled = 2
    }

    public enum MonitoringActionStatus
    {
        Open = 0,
        InProgress = 1,
        Completed = 2,
        Overdue = 3,
        Cancelled = 4
    }

    public enum MonitoringCheckType
    {
        Compliance = 0,
        Administrative = 1
    }

    public enum MonitoringCheckStatus
    {
        Pending = 0,
        Compliant = 1,
        NonCompliant = 2,
        NotApplicable = 3
    }

    public enum MonitoringPlanStatus
    {
        Draft = 0,
        Active = 1,
        Closed = 2
    }

    /// <summary>Provenance of an indicator value (manual entry, survey, import or calculation).</summary>
    public enum DataSourceType
    {
        Manual = 0,
        Questionnaire = 1,
        Import = 2,
        Calculated = 3,
        Aggregated = 4
    }

    /// <summary>
    /// Explicit assignment of a (possibly shared/corporate) indicator to a project. Keeping this as a join
    /// avoids forcing a ProjectId onto the global indicator library.
    /// </summary>
    public class ProjectIndicator
    {
        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public Guid IndicatorId { get; set; }

        [Required]
        public bool IsMandatory { get; set; }

        [Required]
        public int SortOrder { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [ForeignKey("IndicatorId")]
        public virtual Indicator Indicator { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public ProjectIndicator()
        {
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"{ProjectId}/{IndicatorId}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (ProjectIndicator)obj;
            return ProjectId == other.ProjectId && IndicatorId == other.IndicatorId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProjectId, IndicatorId);
        }
    }

    /// <summary>The formal M&E plan for a project — the monitoring matrix header.</summary>
    public class MonitoringPlan
    {
        [Key, Required]
        public Guid MonitoringPlanId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(250)]
        public string Name { get; set; }

        public string Description { get; set; }

        public Guid? FromDateId { get; set; }

        public Guid? ToDateId { get; set; }

        [Required]
        public MonitoringPlanStatus Status { get; set; }

        public Guid? OwnerUserId { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public DateTime? ModifiedOnUtc { get; set; }

        public Guid? ModifiedById { get; set; }

        public virtual Project Project { get; set; }

        [ForeignKey("FromDateId")]
        public virtual Date FromDate { get; set; }

        [ForeignKey("ToDateId")]
        public virtual Date ToDate { get; set; }

        [ForeignKey("OwnerUserId")]
        public virtual User OwnerUser { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public virtual ICollection<MonitoringPlanItem> Items { get; set; } = new List<MonitoringPlanItem>();

        public MonitoringPlan()
        {
            MonitoringPlanId = Guid.NewGuid();
            Status = MonitoringPlanStatus.Draft;
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (MonitoringPlan)obj;
            return MonitoringPlanId == other.MonitoringPlanId;
        }

        public override int GetHashCode()
        {
            return MonitoringPlanId.GetHashCode();
        }
    }

    /// <summary>One row of the monitoring matrix: what is measured, how, how often and by whom.</summary>
    public class MonitoringPlanItem
    {
        [Key, Required]
        public Guid MonitoringPlanItemId { get; set; }

        [Required]
        public Guid MonitoringPlanId { get; set; }

        [Required]
        public Guid IndicatorId { get; set; }

        [Required]
        public MonitoringType MonitoringType { get; set; }

        [MaxLength(250)]
        public string DataSource { get; set; }

        [MaxLength(250)]
        public string CollectionMethod { get; set; }

        [Required]
        public DateType Frequency { get; set; }

        public Guid? ResponsibleUserId { get; set; }

        public Guid? ResponsibleEntityId { get; set; }

        [MaxLength(500)]
        public string MeansOfVerification { get; set; }

        [Required]
        public bool IsMandatory { get; set; }

        public string Notes { get; set; }

        [Required]
        public int SortOrder { get; set; }

        [ForeignKey("MonitoringPlanId")]
        public virtual MonitoringPlan MonitoringPlan { get; set; }

        [ForeignKey("IndicatorId")]
        public virtual Indicator Indicator { get; set; }

        [ForeignKey("ResponsibleUserId")]
        public virtual User ResponsibleUser { get; set; }

        [ForeignKey("ResponsibleEntityId")]
        public virtual Entity ResponsibleEntity { get; set; }

        public MonitoringPlanItem()
        {
            MonitoringPlanItemId = Guid.NewGuid();
            Frequency = DateType.Quarter;
            IsMandatory = true;
        }

        public override string ToString()
        {
            return $"{IndicatorId}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (MonitoringPlanItem)obj;
            return MonitoringPlanItemId == other.MonitoringPlanItemId;
        }

        public override int GetHashCode()
        {
            return MonitoringPlanItemId.GetHashCode();
        }
    }

    /// <summary>
    /// Structured risk / assumption register. Assumptions are the "external factors" that monitoring must
    /// track even though the project does not control them; risks are the same construct with a mitigation plan.
    /// </summary>
    public class RiskAssumption
    {
        [Key, Required]
        public Guid RiskAssumptionId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        public Guid? LogFrameNodeId { get; set; }

        [Required]
        public RiskType Type { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(250)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public int Likelihood { get; set; }

        [Required]
        public int Impact { get; set; }

        [Required]
        public RiskStatus Status { get; set; }

        public Guid? OwnerUserId { get; set; }

        public string Mitigation { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? ReviewDate { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public virtual Project Project { get; set; }

        [ForeignKey("LogFrameNodeId")]
        public virtual LogFrameNode LogFrameNode { get; set; }

        [ForeignKey("OwnerUserId")]
        public virtual User OwnerUser { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public virtual ICollection<RiskAssumptionUpdate> Updates { get; set; } = new List<RiskAssumptionUpdate>();

        /// <summary>Likelihood × impact on a 1–25 scale.</summary>
        [NotMapped]
        public int RiskScore => Likelihood * Impact;

        public RiskAssumption()
        {
            RiskAssumptionId = Guid.NewGuid();
            Likelihood = 3;
            Impact = 3;
            Status = RiskStatus.Open;
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (RiskAssumption)obj;
            return RiskAssumptionId == other.RiskAssumptionId;
        }

        public override int GetHashCode()
        {
            return RiskAssumptionId.GetHashCode();
        }
    }

    /// <summary>Periodic re-assessment of a risk or assumption (assumption monitoring trend).</summary>
    public class RiskAssumptionUpdate
    {
        [Key, Required]
        public Guid RiskAssumptionUpdateId { get; set; }

        [Required]
        public Guid RiskAssumptionId { get; set; }

        public Guid? DateId { get; set; }

        [Required]
        public int Likelihood { get; set; }

        [Required]
        public int Impact { get; set; }

        [Required]
        public RiskStatus Status { get; set; }

        public string Comment { get; set; }

        public Guid? EvidenceDocumentId { get; set; }

        [Required]
        public DateTime RecordedOnUtc { get; set; }

        public Guid? RecordedById { get; set; }

        [ForeignKey("RiskAssumptionId")]
        public virtual RiskAssumption RiskAssumption { get; set; }

        [ForeignKey("DateId")]
        public virtual Date Date { get; set; }

        [ForeignKey("EvidenceDocumentId")]
        public virtual Document EvidenceDocument { get; set; }

        [ForeignKey("RecordedById")]
        public virtual User RecordedBy { get; set; }

        [NotMapped]
        public int RiskScore => Likelihood * Impact;

        public RiskAssumptionUpdate()
        {
            RiskAssumptionUpdateId = Guid.NewGuid();
            RecordedOnUtc = DateTime.UtcNow;
            Likelihood = 3;
            Impact = 3;
            Status = RiskStatus.Monitoring;
        }

        public override string ToString()
        {
            return RiskAssumptionUpdateId.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (RiskAssumptionUpdate)obj;
            return RiskAssumptionUpdateId == other.RiskAssumptionUpdateId;
        }

        public override int GetHashCode()
        {
            return RiskAssumptionUpdateId.GetHashCode();
        }
    }

    /// <summary>
    /// A monitoring encounter (field visit, supervision, data-quality audit, compliance check, technical review,
    /// stakeholder feedback) with findings and follow-up actions.
    /// </summary>
    public class MonitoringEvent
    {
        [Key, Required]
        public Guid MonitoringEventId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        public Guid? EntityId { get; set; }

        [Required]
        public MonitoringEventType Type { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(250)]
        public string Title { get; set; }

        [Required, Column(TypeName = "Date")]
        public DateTime EventDate { get; set; }

        public Guid? PeriodDateId { get; set; }

        public Guid? ConductedById { get; set; }

        [MaxLength(1000)]
        public string Participants { get; set; }

        public string Findings { get; set; }

        public string Recommendations { get; set; }

        [Required]
        public MonitoringEventStatus Status { get; set; }

        public Guid? MeReviewId { get; set; }

        public Guid? DocumentId { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public virtual Project Project { get; set; }

        [ForeignKey("EntityId")]
        public virtual Entity Entity { get; set; }

        [ForeignKey("PeriodDateId")]
        public virtual Date PeriodDate { get; set; }

        [ForeignKey("ConductedById")]
        public virtual User ConductedBy { get; set; }

        [ForeignKey("MeReviewId")]
        public virtual MeReview MeReview { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public virtual ICollection<MonitoringAction> Actions { get; set; } = new List<MonitoringAction>();

        public MonitoringEvent()
        {
            MonitoringEventId = Guid.NewGuid();
            EventDate = DateTime.UtcNow.Date;
            Status = MonitoringEventStatus.Completed;
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (MonitoringEvent)obj;
            return MonitoringEventId == other.MonitoringEventId;
        }

        public override int GetHashCode()
        {
            return MonitoringEventId.GetHashCode();
        }
    }

    /// <summary>Follow-up action from a monitoring event / risk review; optionally updates a workplan task.</summary>
    public class MonitoringAction
    {
        [Key, Required]
        public Guid MonitoringActionId { get; set; }

        public Guid? MonitoringEventId { get; set; }

        public Guid? RiskAssumptionId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(500)]
        public string Description { get; set; }

        public Guid? OwnerUserId { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? DueDate { get; set; }

        [Required]
        public MonitoringActionStatus Status { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? CompletedDate { get; set; }

        public string Notes { get; set; }

        public Guid? LinkedTaskId { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        /// <summary>
        /// The indicator that triggered this corrective action, when the action is a CAP
        /// auto-generated by the feedback-loop deviation watcher.
        /// </summary>
        public Guid? IndicatorId { get; set; }

        /// <summary>
        /// What triggered this action: "VarianceThreshold", "Manual", "RiskReview", etc.
        /// </summary>
        [MaxLength(50)]
        public string TriggerSource { get; set; }

        /// <summary>
        /// Percentage deviation from target at trigger time (e.g. -22 means 22% below target).
        /// </summary>
        [Column(TypeName = "decimal(9,2)")]
        public decimal? DeviationPercent { get; set; }

        /// <summary>
        /// When this corrective action was auto-triggered by the feedback loop.
        /// </summary>
        [Column(TypeName = "Date")]
        public DateTime? TriggeredOnUtc { get; set; }

        public virtual Project Project { get; set; }

        [ForeignKey("MonitoringEventId")]
        public virtual MonitoringEvent MonitoringEvent { get; set; }

        [ForeignKey("RiskAssumptionId")]
        public virtual RiskAssumption RiskAssumption { get; set; }

        [ForeignKey("OwnerUserId")]
        public virtual User OwnerUser { get; set; }

        [ForeignKey("LinkedTaskId")]
        public virtual Task LinkedTask { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        [ForeignKey("IndicatorId")]
        public virtual Indicator Indicator { get; set; }

        public MonitoringAction()
        {
            MonitoringActionId = Guid.NewGuid();
            Status = MonitoringActionStatus.Open;
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Description;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (MonitoringAction)obj;
            return MonitoringActionId == other.MonitoringActionId;
        }

        public override int GetHashCode()
        {
            return MonitoringActionId.GetHashCode();
        }
    }

    /// <summary>Compliance or administrative/logistics checklist item (donor rule, PIP regulation, stock, premises).</summary>
    public class MonitoringCheck
    {
        [Key, Required]
        public Guid MonitoringCheckId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        public Guid? EntityId { get; set; }

        [Required]
        public MonitoringCheckType Type { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(250)]
        public string Title { get; set; }

        [MaxLength(100)]
        public string RequirementSource { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? DueDate { get; set; }

        public Guid? PeriodDateId { get; set; }

        [Required]
        public MonitoringCheckStatus Status { get; set; }

        public Guid? EvidenceDocumentId { get; set; }

        public string Notes { get; set; }

        [Required]
        public DateTime CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public virtual Project Project { get; set; }

        [ForeignKey("EntityId")]
        public virtual Entity Entity { get; set; }

        [ForeignKey("PeriodDateId")]
        public virtual Date PeriodDate { get; set; }

        [ForeignKey("EvidenceDocumentId")]
        public virtual Document EvidenceDocument { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public MonitoringCheck()
        {
            MonitoringCheckId = Guid.NewGuid();
            Status = MonitoringCheckStatus.Pending;
            Type = MonitoringCheckType.Compliance;
            CreatedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (MonitoringCheck)obj;
            return MonitoringCheckId == other.MonitoringCheckId;
        }

        public override int GetHashCode()
        {
            return MonitoringCheckId.GetHashCode();
        }
    }

    /// <summary>Lessons-learned entry supporting organisational learning and adaptive management.</summary>
    public class LessonLearned
    {
        [Key, Required]
        public Guid LessonLearnedId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        public Guid? ComponentId { get; set; }

        public Guid? LogFrameNodeId { get; set; }

        [Required]
        public MonitoringType MonitoringType { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(250)]
        public string Title { get; set; }

        public string Description { get; set; }

        public string Recommendation { get; set; }

        [Required]
        public DateTime RecordedOnUtc { get; set; }

        public Guid? RecordedById { get; set; }

        public virtual Project Project { get; set; }

        [ForeignKey("ComponentId")]
        public virtual Component Component { get; set; }

        [ForeignKey("LogFrameNodeId")]
        public virtual LogFrameNode LogFrameNode { get; set; }

        [ForeignKey("RecordedById")]
        public virtual User RecordedBy { get; set; }

        public LessonLearned()
        {
            LessonLearnedId = Guid.NewGuid();
            RecordedOnUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var other = (LessonLearned)obj;
            return LessonLearnedId == other.LessonLearnedId;
        }

        public override int GetHashCode()
        {
            return LessonLearnedId.GetHashCode();
        }
    }
}
