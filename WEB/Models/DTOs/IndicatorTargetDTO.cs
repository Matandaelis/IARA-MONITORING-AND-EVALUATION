using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class IndicatorTargetDTO
    {
        public Guid IndicatorTargetId { get; set; }

        [Required]
        public Guid IndicatorId { get; set; }

        [Required]
        public Guid DateId { get; set; }

        public Guid? EntityId { get; set; }

        public Guid? DisaggregationOptionId { get; set; }

        public string DisaggregationOptionName { get; set; }

        public string PeriodName { get; set; }

        public string EntityName { get; set; }

        [Required]
        public decimal TargetValue { get; set; }

        public decimal? BaselineValue { get; set; }

        [MaxLength(1000)]
        public string Notes { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class IndicatorVarianceDTO
    {
        public Guid IndicatorId { get; set; }

        public string IndicatorName { get; set; }

        public IndicatorDirection Direction { get; set; } = IndicatorDirection.HigherIsBetter;

        public List<PeriodVarianceDTO> Periods { get; set; } = new List<PeriodVarianceDTO>();
    }

    public class PeriodVarianceDTO
    {
        public Guid DateId { get; set; }

        public string PeriodName { get; set; }

        public decimal? TargetValue { get; set; }

        public decimal? BaselineValue { get; set; }

        public decimal? ActualValue { get; set; }

        public decimal? PercentageAchievement { get; set; }

        public string StatusBadge { get; set; } = "NotApplicable";

        public List<DisaggregationDatumDTO> Disaggregations { get; set; } = new List<DisaggregationDatumDTO>();
    }

    public static partial class ModelFactory
    {
        public static IndicatorTargetDTO Create(IndicatorTarget target)
        {
            if (target == null) return null;

            return new IndicatorTargetDTO
            {
                IndicatorTargetId = target.IndicatorTargetId,
                IndicatorId = target.IndicatorId,
                DateId = target.DateId,
                EntityId = target.EntityId,
                DisaggregationOptionId = target.DisaggregationOptionId,
                DisaggregationOptionName = target.DisaggregationOption?.Name,
                PeriodName = target.Date?.Name,
                EntityName = target.Entity?.Name,
                TargetValue = target.TargetValue,
                BaselineValue = target.BaselineValue,
                Notes = target.Notes,
                UpdatedAt = target.UpdatedAt
            };
        }

        public static void Hydrate(IndicatorTarget target, IndicatorTargetDTO dto)
        {
            target.IndicatorId = dto.IndicatorId;
            target.DateId = dto.DateId;
            target.EntityId = dto.EntityId;
            target.DisaggregationOptionId = dto.DisaggregationOptionId;
            target.TargetValue = dto.TargetValue;
            target.BaselineValue = dto.BaselineValue;
            target.Notes = dto.Notes;
            target.UpdatedAt = DateTime.UtcNow;
        }
    }
}
