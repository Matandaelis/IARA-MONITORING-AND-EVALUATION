using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Monic.Web.Models
{
    public static class MonitoringLabels
    {
        public static string Label(this MonitoringType type) => type switch
        {
            MonitoringType.Process => "Process",
            MonitoringType.Compliance => "Compliance",
            MonitoringType.Context => "Context",
            MonitoringType.Financial => "Financial",
            MonitoringType.Administrative => "Administrative / Logistics",
            MonitoringType.Assumption => "Assumption",
            MonitoringType.Organizational => "Organisational",
            MonitoringType.Results => "Results",
            MonitoringType.Technical => "Technical",
            MonitoringType.Impact => "Impact",
            _ => type.ToString()
        };

        public static string Label(this RagStatus status) => status switch
        {
            RagStatus.Green => "On track",
            RagStatus.Amber => "At risk",
            RagStatus.Red => "Off track",
            _ => "Not assessed"
        };

        public static string Label(this RiskType type) => type == RiskType.Assumption ? "Assumption" : "Risk";

        public static string Label(this RiskStatus status) => status switch
        {
            RiskStatus.Monitoring => "Monitoring",
            RiskStatus.Mitigated => "Mitigated",
            RiskStatus.Realized => "Realized",
            RiskStatus.Closed => "Closed",
            _ => "Open"
        };

        public static string Label(this MonitoringEventType type) => type switch
        {
            MonitoringEventType.FieldVisit => "Field visit",
            MonitoringEventType.Supervision => "Supervision",
            MonitoringEventType.DataQualityAudit => "Data quality audit",
            MonitoringEventType.ComplianceCheck => "Compliance check",
            MonitoringEventType.StakeholderFeedback => "Stakeholder feedback",
            MonitoringEventType.TechnicalReview => "Technical review",
            MonitoringEventType.ReviewMeeting => "Review meeting",
            _ => "Other"
        };

        public static string Label(this MonitoringCheckType type) => type == MonitoringCheckType.Administrative ? "Administrative / logistics" : "Compliance";
    }

    // ─────────────────────────────── computed monitoring DTOs ───────────────────────────────

    public class MonitoringTypeSummaryDTO
    {
        public MonitoringType MonitoringType { get; set; }
        public string Label { get; set; }
        public int Total { get; set; }
        public int Green { get; set; }
        public int Amber { get; set; }
        public int Red { get; set; }
        public int NoData { get; set; }
        public decimal? AverageAchievement { get; set; }
    }

    public class MonitoringIndicatorPerformanceDTO
    {
        public Guid IndicatorId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Units { get; set; }
        public MonitoringType MonitoringType { get; set; }
        public string MonitoringTypeLabel { get; set; }
        public IndicatorDirection Direction { get; set; }
        public bool IsCalculated { get; set; }
        public decimal? Baseline { get; set; }
        public decimal? Target { get; set; }
        public decimal? Actual { get; set; }
        public decimal? PercentageAchievement { get; set; }
        public decimal? AbsoluteVariance { get; set; }
        public string StatusBadge { get; set; }
        public RagStatus Rag { get; set; }
        public decimal? GreenThreshold { get; set; }
        public decimal? AmberThreshold { get; set; }
        public int DataPoints { get; set; }
        public int ApprovedDataPoints { get; set; }
        public decimal? Coverage { get; set; }
        public decimal? Timeliness { get; set; }
        public decimal? ApprovalRate { get; set; }
        public Guid? LatestDateId { get; set; }
        public string LatestPeriod { get; set; }
        public string ResponsibleUserName { get; set; }
    }

    public class MonitoringSummaryDTO
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public Guid? DateId { get; set; }
        public string PeriodLabel { get; set; }
        public DateTime GeneratedOnUtc { get; set; }

        public int TotalIndicators { get; set; }
        public int AssessedIndicators { get; set; }
        public int Green { get; set; }
        public int Amber { get; set; }
        public int Red { get; set; }
        public int NoData { get; set; }
        public decimal? OverallAchievement { get; set; }
        public string OverallStatus { get; set; }
        public RagStatus OverallRag { get; set; }

        public decimal? Coverage { get; set; }
        public decimal? Timeliness { get; set; }
        public decimal? ApprovalRate { get; set; }

        public decimal? CompositeScore { get; set; }
        public decimal? ResultsScore { get; set; }
        public decimal? FinancialScore { get; set; }
        public decimal? WorkplanScore { get; set; }
        public decimal? DataQualityScore { get; set; }

        public MonitoringFinancialDTO Financial { get; set; }
        public List<MonitoringTypeSummaryDTO> ByType { get; set; } = new List<MonitoringTypeSummaryDTO>();
        public List<MonitoringIndicatorPerformanceDTO> Underperforming { get; set; } = new List<MonitoringIndicatorPerformanceDTO>();
    }

    public class MonitoringMatrixPeriodDTO
    {
        public Guid DateId { get; set; }
        public string Label { get; set; }
        public DateTime OpenFrom { get; set; }
        public DateTime OpenTo { get; set; }
    }

    public class MonitoringMatrixCellDTO
    {
        public Guid DateId { get; set; }
        public string PeriodLabel { get; set; }
        public decimal? Target { get; set; }
        public decimal? Actual { get; set; }
        public decimal? PercentageAchievement { get; set; }
        public RagStatus Rag { get; set; }
    }

    public class MonitoringMatrixRowDTO
    {
        public Guid IndicatorId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Units { get; set; }
        public MonitoringType MonitoringType { get; set; }
        public string MonitoringTypeLabel { get; set; }
        public IndicatorDirection Direction { get; set; }
        public decimal? Baseline { get; set; }
        public decimal? Coverage { get; set; }
        public string ResponsibleUserName { get; set; }
        public List<MonitoringMatrixCellDTO> Cells { get; set; } = new List<MonitoringMatrixCellDTO>();
    }

    public class MonitoringMatrixDTO
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public Guid? DateId { get; set; }
        public string PeriodLabel { get; set; }
        public List<MonitoringMatrixPeriodDTO> Periods { get; set; } = new List<MonitoringMatrixPeriodDTO>();
        public List<MonitoringMatrixRowDTO> Rows { get; set; } = new List<MonitoringMatrixRowDTO>();
    }

    public class MonitoringResultNodeDTO
    {
        public Guid LogFrameNodeId { get; set; }
        public Guid? ParentNodeId { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string LevelName { get; set; }
        public int Depth { get; set; }
        public int SortOrder { get; set; }
        public decimal? WeightedAchievement { get; set; }
        public RagStatus Rag { get; set; }
        public decimal? Coverage { get; set; }
        public int IndicatorCount { get; set; }
        public int Green { get; set; }
        public int Amber { get; set; }
        public int Red { get; set; }
        public int NoData { get; set; }
        public int OpenRisks { get; set; }
        public string RisksAndAssumptions { get; set; }
        public List<MonitoringIndicatorPerformanceDTO> Indicators { get; set; } = new List<MonitoringIndicatorPerformanceDTO>();
        public List<MonitoringResultNodeDTO> Children { get; set; } = new List<MonitoringResultNodeDTO>();
    }

    public class MonitoringResultsDTO
    {
        public Guid LogFrameId { get; set; }
        public string LogFrameName { get; set; }
        public Guid? DateId { get; set; }
        public string PeriodLabel { get; set; }
        public decimal? OverallAchievement { get; set; }
        public RagStatus OverallRag { get; set; }
        public decimal? Coverage { get; set; }
        public List<MonitoringResultNodeDTO> Nodes { get; set; } = new List<MonitoringResultNodeDTO>();
    }

    public class MonitoringCoverageRowDTO
    {
        public Guid IndicatorId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public MonitoringType MonitoringType { get; set; }
        public string MonitoringTypeLabel { get; set; }
        public DateType Frequency { get; set; }
        public int Expected { get; set; }
        public int Reported { get; set; }
        public int Approved { get; set; }
        public decimal? Coverage { get; set; }
        public decimal? Timeliness { get; set; }
        public decimal? ApprovalRate { get; set; }
        public string ResponsibleUserName { get; set; }
    }

    public class MonitoringCoverageDTO
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public decimal? OverallCoverage { get; set; }
        public decimal? OverallTimeliness { get; set; }
        public decimal? OverallApprovalRate { get; set; }
        public List<MonitoringCoverageRowDTO> Rows { get; set; } = new List<MonitoringCoverageRowDTO>();
    }

    public class MonitoringCostPerOutputDTO
    {
        public Guid IndicatorId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Expended { get; set; }
        public decimal? OutputValue { get; set; }
        public decimal? CostPerOutput { get; set; }
        public string Currency { get; set; }
    }

    public class MonitoringFinancialDTO
    {
        public Guid ProjectId { get; set; }
        public string BaseCurrency { get; set; }
        public decimal Planned { get; set; }
        public decimal Committed { get; set; }
        public decimal Disbursed { get; set; }
        public decimal Actual { get; set; }
        public decimal Variance { get; set; }
        public decimal Utilization { get; set; }
        public decimal CommitmentRate { get; set; }
        public List<BudgetSummaryRowDTO> Rows { get; set; } = new List<BudgetSummaryRowDTO>();
        public List<string> MissingRates { get; set; } = new List<string>();
        public List<BudgetBurnPointDTO> BurnSeries { get; set; } = new List<BudgetBurnPointDTO>();
        public List<MonitoringCostPerOutputDTO> CostPerOutput { get; set; } = new List<MonitoringCostPerOutputDTO>();
    }

    // ─────────────────────────────── register DTOs ───────────────────────────────

    public class ProjectIndicatorDTO
    {
        public Guid ProjectId { get; set; }
        public Guid IndicatorId { get; set; }
        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }
        public IndicatorDTO Indicator { get; set; }
    }

    public class MonitoringPlanItemDTO
    {
        public Guid MonitoringPlanItemId { get; set; }
        public Guid MonitoringPlanId { get; set; }
        public Guid IndicatorId { get; set; }
        public MonitoringType MonitoringType { get; set; }
        public string DataSource { get; set; }
        public string CollectionMethod { get; set; }
        public DateType Frequency { get; set; }
        public Guid? ResponsibleUserId { get; set; }
        public string ResponsibleUserName { get; set; }
        public Guid? ResponsibleEntityId { get; set; }
        public string ResponsibleEntityName { get; set; }
        public string MeansOfVerification { get; set; }
        public bool IsMandatory { get; set; }
        public string Notes { get; set; }
        public int SortOrder { get; set; }
        public string IndicatorCode { get; set; }
        public string IndicatorName { get; set; }
    }

    public class MonitoringPlanDTO
    {
        public Guid MonitoringPlanId { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? FromDateId { get; set; }
        public Guid? ToDateId { get; set; }
        public string FromDateName { get; set; }
        public string ToDateName { get; set; }
        public MonitoringPlanStatus Status { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string OwnerUserName { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? ModifiedOnUtc { get; set; }
        public List<MonitoringPlanItemDTO> Items { get; set; } = new List<MonitoringPlanItemDTO>();
    }

    public class RiskAssumptionUpdateDTO
    {
        public Guid RiskAssumptionUpdateId { get; set; }
        public Guid RiskAssumptionId { get; set; }
        public Guid? DateId { get; set; }
        public string DateName { get; set; }
        public int Likelihood { get; set; }
        public int Impact { get; set; }
        public int RiskScore { get; set; }
        public RiskStatus Status { get; set; }
        public string Comment { get; set; }
        public Guid? EvidenceDocumentId { get; set; }
        public DateTime RecordedOnUtc { get; set; }
    }

    public class RiskAssumptionDTO
    {
        public Guid RiskAssumptionId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? LogFrameNodeId { get; set; }
        public string LogFrameNodeTitle { get; set; }
        public RiskType Type { get; set; }
        public string TypeLabel { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Likelihood { get; set; }
        public int Impact { get; set; }
        public int RiskScore { get; set; }
        public RiskStatus Status { get; set; }
        public string StatusLabel { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string OwnerUserName { get; set; }
        public string Mitigation { get; set; }
        public DateTime? ReviewDate { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public List<RiskAssumptionUpdateDTO> Updates { get; set; } = new List<RiskAssumptionUpdateDTO>();
    }

    public class MonitoringActionDTO
    {
        public Guid MonitoringActionId { get; set; }
        public Guid? MonitoringEventId { get; set; }
        public Guid? RiskAssumptionId { get; set; }
        public Guid? IndicatorId { get; set; }
        public string IndicatorCode { get; set; }
        public string IndicatorName { get; set; }
        public Guid ProjectId { get; set; }
        public string Description { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string OwnerUserName { get; set; }
        public DateTime? DueDate { get; set; }
        public MonitoringActionStatus Status { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Notes { get; set; }
        public Guid? LinkedTaskId { get; set; }
        public string LinkedTaskName { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string TriggerSource { get; set; }
        public decimal? DeviationPercent { get; set; }
        public DateTime? TriggeredOnUtc { get; set; }
        public bool IsCap => TriggerSource == "VarianceThreshold";
    }

    public class MonitoringEventDTO
    {
        public Guid MonitoringEventId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? EntityId { get; set; }
        public string EntityName { get; set; }
        public MonitoringEventType Type { get; set; }
        public string TypeLabel { get; set; }
        public string Title { get; set; }
        public DateTime EventDate { get; set; }
        public Guid? PeriodDateId { get; set; }
        public string PeriodDateName { get; set; }
        public Guid? ConductedById { get; set; }
        public string ConductedByName { get; set; }
        public string Participants { get; set; }
        public string Findings { get; set; }
        public string Recommendations { get; set; }
        public MonitoringEventStatus Status { get; set; }
        public Guid? MeReviewId { get; set; }
        public Guid? DocumentId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public List<MonitoringActionDTO> Actions { get; set; } = new List<MonitoringActionDTO>();
    }

    public class MonitoringCheckDTO
    {
        public Guid MonitoringCheckId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? EntityId { get; set; }
        public string EntityName { get; set; }
        public MonitoringCheckType Type { get; set; }
        public string TypeLabel { get; set; }
        public string Title { get; set; }
        public string RequirementSource { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? PeriodDateId { get; set; }
        public string PeriodDateName { get; set; }
        public MonitoringCheckStatus Status { get; set; }
        public Guid? EvidenceDocumentId { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }

    public class LessonLearnedDTO
    {
        public Guid LessonLearnedId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? ComponentId { get; set; }
        public string ComponentName { get; set; }
        public Guid? LogFrameNodeId { get; set; }
        public string LogFrameNodeTitle { get; set; }
        public MonitoringType MonitoringType { get; set; }
        public string MonitoringTypeLabel { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
        public DateTime RecordedOnUtc { get; set; }
    }

    public static partial class ModelFactory
    {
        public static ProjectIndicatorDTO Create(ProjectIndicator projectIndicator, bool includeParents = true)
        {
            if (projectIndicator == null) return null;

            return new ProjectIndicatorDTO
            {
                ProjectId = projectIndicator.ProjectId,
                IndicatorId = projectIndicator.IndicatorId,
                IsMandatory = projectIndicator.IsMandatory,
                SortOrder = projectIndicator.SortOrder,
                Indicator = includeParents ? Create(projectIndicator.Indicator) : null
            };
        }

        public static MonitoringPlanDTO Create(MonitoringPlan plan, bool includeItems = true)
        {
            if (plan == null) return null;

            var dto = new MonitoringPlanDTO
            {
                MonitoringPlanId = plan.MonitoringPlanId,
                ProjectId = plan.ProjectId,
                Name = plan.Name,
                Description = plan.Description,
                FromDateId = plan.FromDateId,
                ToDateId = plan.ToDateId,
                FromDateName = plan.FromDate?.Name,
                ToDateName = plan.ToDate?.Name,
                Status = plan.Status,
                OwnerUserId = plan.OwnerUserId,
                OwnerUserName = plan.OwnerUser?.FullName,
                CreatedOnUtc = plan.CreatedOnUtc,
                ModifiedOnUtc = plan.ModifiedOnUtc
            };

            if (includeItems && plan.Items != null)
            {
                dto.Items = plan.Items.OrderBy(o => o.SortOrder).Select(o => Create(o)).ToList();
            }

            return dto;
        }

        public static MonitoringPlanItemDTO Create(MonitoringPlanItem item)
        {
            if (item == null) return null;

            return new MonitoringPlanItemDTO
            {
                MonitoringPlanItemId = item.MonitoringPlanItemId,
                MonitoringPlanId = item.MonitoringPlanId,
                IndicatorId = item.IndicatorId,
                MonitoringType = item.MonitoringType,
                DataSource = item.DataSource,
                CollectionMethod = item.CollectionMethod,
                Frequency = item.Frequency,
                ResponsibleUserId = item.ResponsibleUserId,
                ResponsibleUserName = item.ResponsibleUser?.FullName,
                ResponsibleEntityId = item.ResponsibleEntityId,
                ResponsibleEntityName = item.ResponsibleEntity?.Name,
                MeansOfVerification = item.MeansOfVerification,
                IsMandatory = item.IsMandatory,
                Notes = item.Notes,
                SortOrder = item.SortOrder,
                IndicatorCode = item.Indicator?.Code,
                IndicatorName = item.Indicator?.Name
            };
        }

        public static void Hydrate(MonitoringPlan plan, MonitoringPlanDTO dto)
        {
            plan.ProjectId = dto.ProjectId;
            plan.Name = dto.Name;
            plan.Description = dto.Description;
            plan.FromDateId = dto.FromDateId;
            plan.ToDateId = dto.ToDateId;
            plan.Status = dto.Status;
            plan.OwnerUserId = dto.OwnerUserId;
        }

        public static void Hydrate(MonitoringPlanItem item, MonitoringPlanItemDTO dto)
        {
            item.MonitoringPlanId = dto.MonitoringPlanId;
            item.IndicatorId = dto.IndicatorId;
            item.MonitoringType = dto.MonitoringType;
            item.DataSource = dto.DataSource;
            item.CollectionMethod = dto.CollectionMethod;
            item.Frequency = dto.Frequency;
            item.ResponsibleUserId = dto.ResponsibleUserId;
            item.ResponsibleEntityId = dto.ResponsibleEntityId;
            item.MeansOfVerification = dto.MeansOfVerification;
            item.IsMandatory = dto.IsMandatory;
            item.Notes = dto.Notes;
            item.SortOrder = dto.SortOrder;
        }

        public static RiskAssumptionUpdateDTO Create(RiskAssumptionUpdate update)
        {
            if (update == null) return null;

            return new RiskAssumptionUpdateDTO
            {
                RiskAssumptionUpdateId = update.RiskAssumptionUpdateId,
                RiskAssumptionId = update.RiskAssumptionId,
                DateId = update.DateId,
                DateName = update.Date?.Name,
                Likelihood = update.Likelihood,
                Impact = update.Impact,
                RiskScore = update.RiskScore,
                Status = update.Status,
                Comment = update.Comment,
                EvidenceDocumentId = update.EvidenceDocumentId,
                RecordedOnUtc = update.RecordedOnUtc
            };
        }

        public static RiskAssumptionDTO Create(RiskAssumption risk, bool includeUpdates = true)
        {
            if (risk == null) return null;

            var dto = new RiskAssumptionDTO
            {
                RiskAssumptionId = risk.RiskAssumptionId,
                ProjectId = risk.ProjectId,
                LogFrameNodeId = risk.LogFrameNodeId,
                LogFrameNodeTitle = risk.LogFrameNode?.Title,
                Type = risk.Type,
                TypeLabel = risk.Type.Label(),
                Title = risk.Title,
                Description = risk.Description,
                Likelihood = risk.Likelihood,
                Impact = risk.Impact,
                RiskScore = risk.RiskScore,
                Status = risk.Status,
                StatusLabel = risk.Status.Label(),
                OwnerUserId = risk.OwnerUserId,
                OwnerUserName = risk.OwnerUser?.FullName,
                Mitigation = risk.Mitigation,
                ReviewDate = risk.ReviewDate,
                CreatedOnUtc = risk.CreatedOnUtc
            };

            if (includeUpdates && risk.Updates != null)
            {
                dto.Updates = risk.Updates.OrderBy(o => o.RecordedOnUtc).Select(o => Create(o)).ToList();
            }

            return dto;
        }

        public static void Hydrate(RiskAssumption risk, RiskAssumptionDTO dto)
        {
            risk.ProjectId = dto.ProjectId;
            risk.LogFrameNodeId = dto.LogFrameNodeId;
            risk.Type = dto.Type;
            risk.Title = dto.Title;
            risk.Description = dto.Description;
            risk.Likelihood = dto.Likelihood;
            risk.Impact = dto.Impact;
            risk.Status = dto.Status;
            risk.OwnerUserId = dto.OwnerUserId;
            risk.Mitigation = dto.Mitigation;
            risk.ReviewDate = dto.ReviewDate;
        }

        public static void Hydrate(RiskAssumptionUpdate update, RiskAssumptionUpdateDTO dto)
        {
            update.RiskAssumptionId = dto.RiskAssumptionId;
            update.DateId = dto.DateId;
            update.Likelihood = dto.Likelihood;
            update.Impact = dto.Impact;
            update.Status = dto.Status;
            update.Comment = dto.Comment;
            update.EvidenceDocumentId = dto.EvidenceDocumentId;
        }

        public static MonitoringActionDTO Create(MonitoringAction action)
        {
            if (action == null) return null;

            return new MonitoringActionDTO
            {
                MonitoringActionId = action.MonitoringActionId,
                MonitoringEventId = action.MonitoringEventId,
                RiskAssumptionId = action.RiskAssumptionId,
                IndicatorId = action.IndicatorId,
                IndicatorCode = action.Indicator?.Code,
                IndicatorName = action.Indicator?.Name,
                ProjectId = action.ProjectId,
                Description = action.Description,
                OwnerUserId = action.OwnerUserId,
                OwnerUserName = action.OwnerUser?.FullName,
                DueDate = action.DueDate,
                Status = action.Status,
                CompletedDate = action.CompletedDate,
                Notes = action.Notes,
                LinkedTaskId = action.LinkedTaskId,
                LinkedTaskName = action.LinkedTask?.Name,
                CreatedOnUtc = action.CreatedOnUtc,
                TriggerSource = action.TriggerSource,
                DeviationPercent = action.DeviationPercent,
                TriggeredOnUtc = action.TriggeredOnUtc
            };
        }

        public static MonitoringEventDTO Create(MonitoringEvent monitoringEvent, bool includeActions = true)
        {
            if (monitoringEvent == null) return null;

            var dto = new MonitoringEventDTO
            {
                MonitoringEventId = monitoringEvent.MonitoringEventId,
                ProjectId = monitoringEvent.ProjectId,
                EntityId = monitoringEvent.EntityId,
                EntityName = monitoringEvent.Entity?.Name,
                Type = monitoringEvent.Type,
                TypeLabel = monitoringEvent.Type.Label(),
                Title = monitoringEvent.Title,
                EventDate = monitoringEvent.EventDate,
                PeriodDateId = monitoringEvent.PeriodDateId,
                PeriodDateName = monitoringEvent.PeriodDate?.Name,
                ConductedById = monitoringEvent.ConductedById,
                ConductedByName = monitoringEvent.ConductedBy?.FullName,
                Participants = monitoringEvent.Participants,
                Findings = monitoringEvent.Findings,
                Recommendations = monitoringEvent.Recommendations,
                Status = monitoringEvent.Status,
                MeReviewId = monitoringEvent.MeReviewId,
                DocumentId = monitoringEvent.DocumentId,
                CreatedOnUtc = monitoringEvent.CreatedOnUtc
            };

            if (includeActions && monitoringEvent.Actions != null)
            {
                dto.Actions = monitoringEvent.Actions.OrderBy(o => o.CreatedOnUtc).Select(o => Create(o)).ToList();
            }

            return dto;
        }

        public static void Hydrate(MonitoringEvent monitoringEvent, MonitoringEventDTO dto)
        {
            monitoringEvent.ProjectId = dto.ProjectId;
            monitoringEvent.EntityId = dto.EntityId;
            monitoringEvent.Type = dto.Type;
            monitoringEvent.Title = dto.Title;
            monitoringEvent.EventDate = dto.EventDate;
            monitoringEvent.PeriodDateId = dto.PeriodDateId;
            monitoringEvent.ConductedById = dto.ConductedById;
            monitoringEvent.Participants = dto.Participants;
            monitoringEvent.Findings = dto.Findings;
            monitoringEvent.Recommendations = dto.Recommendations;
            monitoringEvent.Status = dto.Status;
            monitoringEvent.MeReviewId = dto.MeReviewId;
            monitoringEvent.DocumentId = dto.DocumentId;
        }

        public static void Hydrate(MonitoringAction action, MonitoringActionDTO dto)
        {
            action.MonitoringEventId = dto.MonitoringEventId;
            action.RiskAssumptionId = dto.RiskAssumptionId;
            action.IndicatorId = dto.IndicatorId;
            action.ProjectId = dto.ProjectId;
            action.Description = dto.Description;
            action.OwnerUserId = dto.OwnerUserId;
            action.DueDate = dto.DueDate;
            action.Status = dto.Status;
            action.CompletedDate = dto.CompletedDate;
            action.Notes = dto.Notes;
            action.LinkedTaskId = dto.LinkedTaskId;
            action.TriggerSource = dto.TriggerSource;
            action.DeviationPercent = dto.DeviationPercent;
            action.TriggeredOnUtc = dto.TriggeredOnUtc;
        }

        public static MonitoringCheckDTO Create(MonitoringCheck check)
        {
            if (check == null) return null;

            return new MonitoringCheckDTO
            {
                MonitoringCheckId = check.MonitoringCheckId,
                ProjectId = check.ProjectId,
                EntityId = check.EntityId,
                EntityName = check.Entity?.Name,
                Type = check.Type,
                TypeLabel = check.Type.Label(),
                Title = check.Title,
                RequirementSource = check.RequirementSource,
                Description = check.Description,
                DueDate = check.DueDate,
                PeriodDateId = check.PeriodDateId,
                PeriodDateName = check.PeriodDate?.Name,
                Status = check.Status,
                EvidenceDocumentId = check.EvidenceDocumentId,
                Notes = check.Notes,
                CreatedOnUtc = check.CreatedOnUtc
            };
        }

        public static void Hydrate(MonitoringCheck check, MonitoringCheckDTO dto)
        {
            check.ProjectId = dto.ProjectId;
            check.EntityId = dto.EntityId;
            check.Type = dto.Type;
            check.Title = dto.Title;
            check.RequirementSource = dto.RequirementSource;
            check.Description = dto.Description;
            check.DueDate = dto.DueDate;
            check.PeriodDateId = dto.PeriodDateId;
            check.Status = dto.Status;
            check.EvidenceDocumentId = dto.EvidenceDocumentId;
            check.Notes = dto.Notes;
        }

        public static LessonLearnedDTO Create(LessonLearned lesson)
        {
            if (lesson == null) return null;

            return new LessonLearnedDTO
            {
                LessonLearnedId = lesson.LessonLearnedId,
                ProjectId = lesson.ProjectId,
                ComponentId = lesson.ComponentId,
                ComponentName = lesson.Component?.Name,
                LogFrameNodeId = lesson.LogFrameNodeId,
                LogFrameNodeTitle = lesson.LogFrameNode?.Title,
                MonitoringType = lesson.MonitoringType,
                MonitoringTypeLabel = lesson.MonitoringType.Label(),
                Title = lesson.Title,
                Description = lesson.Description,
                Recommendation = lesson.Recommendation,
                RecordedOnUtc = lesson.RecordedOnUtc
            };
        }

        public static void Hydrate(LessonLearned lesson, LessonLearnedDTO dto)
        {
            lesson.ProjectId = dto.ProjectId;
            lesson.ComponentId = dto.ComponentId;
            lesson.LogFrameNodeId = dto.LogFrameNodeId;
            lesson.MonitoringType = dto.MonitoringType;
            lesson.Title = dto.Title;
            lesson.Description = dto.Description;
            lesson.Recommendation = dto.Recommendation;
            lesson.RecordedOnUtc = dto.RecordedOnUtc;
        }
    }
}
