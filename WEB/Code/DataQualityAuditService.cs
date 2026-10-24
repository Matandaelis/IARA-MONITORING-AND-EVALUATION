using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code
{
    public class DqaResult
    {
        public decimal ReportedValue { get; set; }
        public decimal RecountedValue { get; set; }
        public decimal VerificationFactor { get; set; }
        public decimal DiscrepancyPercent { get; set; }
        public string Grade { get; set; }
        public string Interpretation { get; set; }
        public bool TriggerCap { get; set; }
    }

    public class DqaAuditSummary
    {
        public DqaResult Result { get; set; }
        public MonitoringAction GeneratedCap { get; set; }
    }

    /// <summary>
    /// Routine Data Quality Assessment (RDQA / DQA) Service:
    /// Calculates verification factor (VF) and recount discrepancy percentages for field spot-checks,
    /// assigns international standard grades (Grade A &lt; 5%, Grade B 5-10%, Grade C &gt; 10%),
    /// and auto-triggers Corrective Action Plans (CAP) with 7 business day SLAs for Grade C findings.
    /// </summary>
    public static class DataQualityAuditService
    {
        public static DqaResult AssessSpotCheck(decimal reportedValue, decimal recountedValue)
        {
            decimal vf;
            if (reportedValue != 0m)
            {
                vf = recountedValue / reportedValue;
            }
            else
            {
                vf = recountedValue == 0m ? 1m : 0m;
            }

            decimal denom = reportedValue != 0m ? Math.Abs(reportedValue) : 1m;
            decimal discrepancy = Math.Abs(recountedValue - reportedValue) / denom * 100m;

            string grade;
            string interpretation;
            bool triggerCap = false;

            if (discrepancy < 5.0m)
            {
                grade = "A";
                interpretation = "Acceptable: High quality verified data within standard 5% tolerance threshold.";
            }
            else if (discrepancy <= 10.0m)
            {
                grade = "B";
                interpretation = "Minor Discrepancy: Discrepancy between 5% and 10%. Requires supervisor review and recount validation.";
            }
            else
            {
                grade = "C";
                interpretation = "Significant Discrepancy: Discrepancy exceeds 10%. Requires immediate field audit and Corrective Action Plan.";
                triggerCap = true;
            }

            return new DqaResult
            {
                ReportedValue = reportedValue,
                RecountedValue = recountedValue,
                VerificationFactor = Math.Round(vf, 4),
                DiscrepancyPercent = Math.Round(discrepancy, 2),
                Grade = grade,
                Interpretation = interpretation,
                TriggerCap = triggerCap
            };
        }

        public static async Task<DqaAuditSummary> PerformDqaAndTriggerCapAsync(
            ApplicationDbContext db,
            Guid projectId,
            Guid indicatorId,
            decimal reportedValue,
            decimal recountedValue,
            string siteName,
            Guid? auditorUserId)
        {
            var result = AssessSpotCheck(reportedValue, recountedValue);
            MonitoringAction cap = null;

            if (result.TriggerCap)
            {
                var existingCap = await db.MonitoringActions
                    .AnyAsync(o => o.ProjectId == projectId
                                && o.IndicatorId == indicatorId
                                && o.TriggerSource == "DataQualityAudit"
                                && o.Status != MonitoringActionStatus.Completed
                                && o.Status != MonitoringActionStatus.Cancelled);

                if (!existingCap)
                {
                    var indicator = await db.Indicators.FirstOrDefaultAsync(i => i.IndicatorId == indicatorId);
                    var indLabel = indicator != null ? (indicator.Code ?? indicator.Name) : indicatorId.ToString();

                    cap = new MonitoringAction
                    {
                        MonitoringActionId = Guid.NewGuid(),
                        ProjectId = projectId,
                        IndicatorId = indicatorId,
                        Description = $"DQA Spot-Check audit at site '{siteName}' found Grade C discrepancy on indicator '{indLabel}': " +
                                      $"Reported {reportedValue}, Recounted {recountedValue} ({result.DiscrepancyPercent:F1}% discrepancy). " +
                                      $"Field investigation and corrective intervention required within 7 business days.",
                        TriggerSource = "DataQualityAudit",
                        DeviationPercent = result.DiscrepancyPercent,
                        TriggeredOnUtc = DateTime.UtcNow.Date,
                        DueDate = AddBusinessDays(DateTime.UtcNow.Date, 7),
                        Status = MonitoringActionStatus.Open,
                        CreatedOnUtc = DateTime.UtcNow,
                        CreatedById = auditorUserId
                    };

                    db.MonitoringActions.Add(cap);
                    await db.SaveChangesAsync();
                }
            }

            return new DqaAuditSummary
            {
                Result = result,
                GeneratedCap = cap
            };
        }

        private static DateTime AddBusinessDays(DateTime startDate, int businessDays)
        {
            var date = startDate;
            var added = 0;
            while (added < businessDays)
            {
                date = date.AddDays(1);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    added++;
                }
            }
            return date;
        }
    }
}
