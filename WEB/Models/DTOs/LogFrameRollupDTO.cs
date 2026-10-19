using System;
using System.Collections.Generic;

namespace Monic.Web.Models
{
    public class LogFrameRollupDTO
    {
        public Guid LogFrameId { get; set; }
        public string LogFrameName { get; set; }
        public int TotalRows { get; set; }
        public int TotalIndicators { get; set; }
        public decimal? OverallAchievement { get; set; }
        public string OverallStatus { get; set; } = "NotApplicable";
        public int TotalOnTrack { get; set; }
        public int TotalUnderperforming { get; set; }
        public int TotalExceeding { get; set; }
        public int TotalNoData { get; set; }
        public List<LogFrameRowRollupDTO> Rows { get; set; } = new List<LogFrameRowRollupDTO>();
    }

    public class LogFrameRowRollupDTO
    {
        public Guid LogFrameRowId { get; set; }
        public Guid LogFrameId { get; set; }
        public int RowNumber { get; set; }
        public LogFrameRowType RowType { get; set; }
        public string RowTypeName { get; set; }
        public string Description { get; set; }
        public string MeansOfVerification { get; set; }
        public string RisksAndAssumptions { get; set; }
        public int TotalIndicators { get; set; }
        public decimal? AverageAchievement { get; set; }
        public string RowStatus { get; set; } = "NotApplicable";
        public int TotalOnTrack { get; set; }
        public int TotalUnderperforming { get; set; }
        public int TotalExceeding { get; set; }
        public int TotalNoData { get; set; }
        public List<LogFrameIndicatorRollupDTO> Indicators { get; set; } = new List<LogFrameIndicatorRollupDTO>();
    }

    public class LogFrameIndicatorRollupDTO
    {
        public Guid IndicatorId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Units { get; set; }
        public IndicatorDirection Direction { get; set; }
        public decimal? Baseline { get; set; }
        public decimal? Target { get; set; }
        public decimal? Actual { get; set; }
        public decimal? PercentageAchievement { get; set; }
        public string StatusBadge { get; set; } = "NotApplicable";
        public decimal? AbsoluteVariance { get; set; }
    }
}
