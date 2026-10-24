using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Monic.Web.Models
{
    public class LogFrameRowRiskAssumptionDTO
    {
        public Guid Id { get; set; }
        public Guid LogFrameRowId { get; set; }
        public Guid RiskAssumptionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Likelihood { get; set; }
        public int Impact { get; set; }
        public string Status { get; set; }
    }

    public class LogFrameRowTaskDTO
    {
        public Guid Id { get; set; }
        public Guid LogFrameRowId { get; set; }
        public Guid TaskId { get; set; }
        public string Name { get; set; }
        public decimal PercentCompleted { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class LogFrameRowBudgetLineDTO
    {
        public Guid Id { get; set; }
        public Guid LogFrameRowId { get; set; }
        public Guid BudgetLineId { get; set; }
        public string Description { get; set; }
        public decimal PlannedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public string Currency { get; set; }
    }

    public class LogFrameLiveIndicatorMetricDTO
    {
        public Guid IndicatorId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? CurrentValue { get; set; }
        public decimal? PercentageAchievement { get; set; }
        public string StatusBadgeColor { get; set; } = "secondary";
    }

    public class LogFrameRowDTO
    {
        [Required]
        public Guid LogFrameRowId { get; set; }

        [Required]
        public Guid LogFrameId { get; set; }

        [Required]
        public int RowNumber { get; set; }

        [Required]
        public LogFrameRowType RowType { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Description { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Indicators { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string MeansOfVerification { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string RisksAndAssumptions { get; set; }

        public LogFrameDTO LogFrame { get; set; }

        public virtual List<LogFrameRowComponentDTO> LogFrameRowComponents { get; set; } = new List<LogFrameRowComponentDTO>();

        public virtual List<LogFrameRowIndicatorDTO> LogFrameRowIndicators { get; set; } = new List<LogFrameRowIndicatorDTO>();

        public virtual List<LogFrameRowRiskAssumptionDTO> LogFrameRowRiskAssumptions { get; set; } = new List<LogFrameRowRiskAssumptionDTO>();

        public virtual List<LogFrameRowTaskDTO> LogFrameRowTasks { get; set; } = new List<LogFrameRowTaskDTO>();

        public virtual List<LogFrameRowBudgetLineDTO> LogFrameRowBudgetLines { get; set; } = new List<LogFrameRowBudgetLineDTO>();

        public virtual List<LogFrameLiveIndicatorMetricDTO> LiveIndicatorMetrics { get; set; } = new List<LogFrameLiveIndicatorMetricDTO>();
    }

    public static partial class ModelFactory
    {
        public static LogFrameRowDTO Create(LogFrameRow logFrameRow, bool includeParents = true, bool includeChildren = false)
        {
            if (logFrameRow == null) return null;

            var logFrameRowDTO = new LogFrameRowDTO();

            logFrameRowDTO.LogFrameRowId = logFrameRow.LogFrameRowId;
            logFrameRowDTO.LogFrameId = logFrameRow.LogFrameId;
            logFrameRowDTO.RowNumber = logFrameRow.RowNumber;
            logFrameRowDTO.RowType = logFrameRow.RowType;
            logFrameRowDTO.Description = logFrameRow.Description;
            logFrameRowDTO.Indicators = logFrameRow.Indicators;
            logFrameRowDTO.MeansOfVerification = logFrameRow.MeansOfVerification;
            logFrameRowDTO.RisksAndAssumptions = logFrameRow.RisksAndAssumptions;

            if (includeParents)
            {
                logFrameRowDTO.LogFrame = Create(logFrameRow.LogFrame);
            }

            if (includeChildren)
            {
                if (logFrameRow.LogFrameRowComponents != null)
                {
                    foreach (var logFrameRowComponent in logFrameRow.LogFrameRowComponents)
                        logFrameRowDTO.LogFrameRowComponents.Add(Create(logFrameRowComponent));
                }

                if (logFrameRow.LogFrameRowIndicators != null)
                {
                    foreach (var logFrameRowIndicator in logFrameRow.LogFrameRowIndicators)
                        logFrameRowDTO.LogFrameRowIndicators.Add(Create(logFrameRowIndicator));
                }

                if (logFrameRow.LogFrameRowRiskAssumptions != null)
                {
                    foreach (var r in logFrameRow.LogFrameRowRiskAssumptions)
                    {
                        logFrameRowDTO.LogFrameRowRiskAssumptions.Add(new LogFrameRowRiskAssumptionDTO
                        {
                            Id = r.Id,
                            LogFrameRowId = r.LogFrameRowId,
                            RiskAssumptionId = r.RiskAssumptionId,
                            Title = r.RiskAssumption?.Title ?? "Risk",
                            Description = r.RiskAssumption?.Description,
                            Likelihood = r.RiskAssumption?.Likelihood ?? 1,
                            Impact = r.RiskAssumption?.Impact ?? 1,
                            Status = r.RiskAssumption?.Status.ToString()
                        });
                    }
                }

                if (logFrameRow.LogFrameRowTasks != null)
                {
                    foreach (var t in logFrameRow.LogFrameRowTasks)
                    {
                        logFrameRowDTO.LogFrameRowTasks.Add(new LogFrameRowTaskDTO
                        {
                            Id = t.Id,
                            LogFrameRowId = t.LogFrameRowId,
                            TaskId = t.TaskId,
                            Name = t.Task?.Name ?? "Task",
                            PercentCompleted = t.Task?.PercentCompleted ?? 0m,
                            StartDate = t.Task?.StartDate,
                            EndDate = t.Task?.EndDate
                        });
                    }
                }

                if (logFrameRow.LogFrameRowBudgetLines != null)
                {
                    foreach (var b in logFrameRow.LogFrameRowBudgetLines)
                    {
                        logFrameRowDTO.LogFrameRowBudgetLines.Add(new LogFrameRowBudgetLineDTO
                        {
                            Id = b.Id,
                            LogFrameRowId = b.LogFrameRowId,
                            BudgetLineId = b.BudgetLineId,
                            Description = b.BudgetLine?.Description,
                            PlannedAmount = b.BudgetLine?.PlannedAmount ?? 0m,
                            ActualAmount = b.BudgetLine?.ActualAmount ?? 0m,
                            Currency = b.BudgetLine?.Currency ?? "USD"
                        });
                    }
                }
            }

            return logFrameRowDTO;
        }

        public static void Hydrate(LogFrameRow logFrameRow, LogFrameRowDTO logFrameRowDTO)
        {
            logFrameRow.LogFrameId = logFrameRowDTO.LogFrameId;
            logFrameRow.RowNumber = logFrameRowDTO.RowNumber;
            logFrameRow.RowType = logFrameRowDTO.RowType;
            logFrameRow.Description = logFrameRowDTO.Description;
            logFrameRow.Indicators = logFrameRowDTO.Indicators;
            logFrameRow.MeansOfVerification = logFrameRowDTO.MeansOfVerification;
            logFrameRow.RisksAndAssumptions = logFrameRowDTO.RisksAndAssumptions;
        }
    }
}
