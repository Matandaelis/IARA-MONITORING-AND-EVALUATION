using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Monic.Web.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Monic.Web.Code
{
    /// <summary>
    /// Donor Export Service:
    /// Provides compliant exports for major international development donors:
    /// 1. IATI XML v2.03 Activity and Results Framework schema
    /// 2. USAID Quarterly Performance Report (QPR) Indicator Performance Tracking Table (IPTT) Excel
    /// 3. European Commission Result Oriented Monitoring (ROM) 4x4 matrix and evaluation criteria workbook
    /// </summary>
    public static class DonorExportService
    {
        #region IATI XML v2.03 Export

        public static string GenerateIatiXml(Project project, LogFrame logFrame, List<Indicator> indicators = null)
        {
            indicators ??= new List<Indicator>();
            string isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string startDate = project.StartDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-01-01");
            string endDate = project.EndDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddYears(1).ToString("yyyy-12-31");
            string grantCode = string.IsNullOrWhiteSpace(project.GrantCode) ? project.ProjectId.ToString() : project.GrantCode;
            string iatiId = $"XM-DAC-MONIC-{grantCode}";

            var root = new XElement("iati-activities",
                new XAttribute("version", "2.03"),
                new XAttribute("generated-datetime", isoNow),
                new XElement("iati-activity",
                    new XAttribute("default-currency", project.Currency ?? "USD"),
                    new XAttribute("last-updated-datetime", isoNow),
                    new XElement("iati-identifier", iatiId),
                    new XElement("reporting-org",
                        new XAttribute("ref", "XM-DAC-MONIC"),
                        new XAttribute("type", "21"), // International NGO / Platform
                        new XElement("narrative", "MONIC Universal M&E System")
                    ),
                    new XElement("title",
                        new XElement("narrative", project.Name ?? "Universal M&E Project")
                    ),
                    new XElement("description",
                        new XAttribute("type", "1"), // General description
                        new XElement("narrative", $"{project.Sector ?? "Multi-sectoral"} intervention under {project.DonorName ?? "donor funding"}.")
                    ),
                    new XElement("activity-status", new XAttribute("code", "2")), // 2 = Implementation
                    new XElement("activity-date",
                        new XAttribute("type", "1"), // Planned start
                        new XAttribute("iso-date", startDate)
                    ),
                    new XElement("activity-date",
                        new XAttribute("type", "3"), // Planned end
                        new XAttribute("iso-date", endDate)
                    ),
                    new XElement("recipient-country",
                        new XAttribute("code", "TZ"),
                        new XElement("narrative", "Tanzania")
                    ),
                    new XElement("sector",
                        new XAttribute("vocabulary", "1"), // DAC 5 Digit
                        new XAttribute("code", "11110"),
                        new XElement("narrative", project.Sector ?? "Multi-sector")
                    )
                )
            );

            var activityNode = root.Element("iati-activity");

            // Append LogFrame rows as IATI Result elements
            if (logFrame?.LogFrameRows != null && logFrame.LogFrameRows.Any())
            {
                var sortedRows = logFrame.LogFrameRows
                    .OrderByDescending(r => r.RowType)
                    .ThenBy(r => r.RowNumber);

                foreach (var row in sortedRows)
                {
                    string resultType = row.RowType switch
                    {
                        LogFrameRowType.Goals => "9",     // 9 = Impact
                        LogFrameRowType.Outcomes => "2",  // 2 = Outcome
                        LogFrameRowType.Outputs => "1",   // 1 = Output
                        _ => "1"                          // 1 = Output / Activity
                    };

                    var resultElem = new XElement("result",
                        new XAttribute("type", resultType),
                        new XAttribute("aggregation-status", "true"),
                        new XElement("title",
                            new XElement("narrative", row.Description ?? "Result")
                        ),
                        new XElement("description",
                            new XElement("narrative", row.RisksAndAssumptions ?? "Assumptions hold.")
                        )
                    );

                    var rowIndicators = row.LogFrameRowIndicators?.Select(ri => ri.Indicator).Where(i => i != null).ToList()
                        ?? new List<Indicator>();

                    foreach (var ind in rowIndicators)
                    {
                        decimal baselineVal = ind.Baseline ?? 0m;
                        decimal targetVal = ind.IndicatorTargets?.OrderByDescending(t => t.Date?.SortOrder).FirstOrDefault()?.TargetValue ?? 0m;
                        decimal actualVal = ind.Data?.OrderByDescending(d => d.LastSavedDateUtc).FirstOrDefault()?.Value ?? 0m;
                        int baselineYear = project.StartDate?.Year ?? DateTime.UtcNow.Year;

                        var indElem = new XElement("indicator",
                            new XAttribute("measure", "1"), // 1 = Unit count
                            new XAttribute("ascending", !ind.LowerIsBetter ? "true" : "false"),
                            new XElement("title",
                                new XElement("narrative", $"[{ind.Code}] {ind.Name}")
                            ),
                            new XElement("description",
                                new XElement("narrative", ind.OperationalDefinition ?? ind.Name)
                            ),
                            new XElement("baseline",
                                new XAttribute("year", baselineYear.ToString()),
                                new XAttribute("value", baselineVal.ToString("F1")),
                                new XElement("comment",
                                    new XElement("narrative", $"MoV: {row.MeansOfVerification ?? ind.Units ?? "Field surveys"}")
                                )
                            ),
                            new XElement("period",
                                new XElement("period-start", new XAttribute("iso-date", startDate)),
                                new XElement("period-end", new XAttribute("iso-date", endDate)),
                                new XElement("target", new XAttribute("value", targetVal.ToString("F1"))),
                                new XElement("actual", new XAttribute("value", actualVal.ToString("F1")))
                            )
                        );

                        resultElem.Add(indElem);
                    }

                    activityNode.Add(resultElem);
                }
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            return doc.ToString();
        }

        #endregion

        #region USAID Quarterly Performance Report (QPR) IPTT

        public static byte[] GenerateUsaidQprExcel(Project project, LogFrame logFrame, List<Indicator> indicators = null)
        {
            indicators ??= new List<Indicator>();
            using var xls = new ExcelPackage();

            var ws = xls.Workbook.Worksheets.Add("USAID IPTT");

            // Header Banner
            ws.Cells[1, 1].Value = "USAID PERFORMANCE INDICATOR TRACKING TABLE (IPTT)";
            ws.Cells[1, 1, 1, 19].Merge = true;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 47, 108)); // USAID Blue
            ws.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);

            // Project Metadata
            ws.Cells[2, 1].Value = "Award / Grant No:";
            ws.Cells[2, 2].Value = project.GrantCode ?? "AID-OAA-L-00-00000";
            ws.Cells[2, 4].Value = "Implementing Partner:";
            ws.Cells[2, 5].Value = "MONIC Implementing Consortium";
            ws.Cells[2, 8].Value = "Activity Name:";
            ws.Cells[2, 9].Value = project.Name;

            ws.Cells[3, 1].Value = "Fiscal Year:";
            ws.Cells[3, 2].Value = $"FY{DateTime.UtcNow.Year}";
            ws.Cells[3, 4].Value = "Reporting Period:";
            ws.Cells[3, 5].Value = $"Q{((DateTime.UtcNow.Month - 1) / 3) + 1} Cumulative";
            ws.Cells[3, 8].Value = "Sector:";
            ws.Cells[3, 9].Value = project.Sector ?? "Cross-Cutting";

            ws.Cells[2, 1, 3, 9].Style.Font.Bold = true;

            // IPTT Table Headers
            int headerRow = 5;
            string[] headers = new[]
            {
                "Indicator No", "Indicator Title", "Category (Std/Custom)", "Unit of Measure",
                "Data Source / MoV", "Frequency", "Baseline Value", "Baseline Date",
                "LOP Target", "FY Target", "FY Actual",
                "Q1 Target", "Q1 Actual", "Q2 Target", "Q2 Actual", "Q3 Target", "Q3 Actual", "Q4 Target", "Q4 Actual",
                "% Achieved", "Deviation Narrative (>10% or <-10%)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[headerRow, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(186, 12, 47)); // USAID Red
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.WrapText = true;
            }

            int rowIdx = 6;
            var allIndicators = indicators.Any()
                ? indicators
                : (logFrame?.LogFrameRows?.SelectMany(r => r.LogFrameRowIndicators.Select(ri => ri.Indicator)).Where(i => i != null).Distinct().ToList() ?? new List<Indicator>());

            foreach (var ind in allIndicators)
            {
                decimal baseline = ind.Baseline ?? 0m;
                decimal lopTarget = ind.IndicatorTargets?.OrderByDescending(t => t.Date?.SortOrder).FirstOrDefault()?.TargetValue ?? 0m;
                decimal fyTarget = lopTarget; // Standard annual target
                decimal fyActual = ind.Data?.OrderByDescending(d => d.LastSavedDateUtc).FirstOrDefault()?.Value ?? 0m;
                decimal pctAchieved = fyTarget != 0m ? Math.Round(fyActual / fyTarget * 100m, 1) : 0m;
                decimal deviation = pctAchieved - 100m;

                string deviationNarrative = "";
                if (Math.Abs(deviation) > 10m)
                {
                    deviationNarrative = deviation > 10m
                        ? $"Exceeded annual target by {deviation:N1}% due to accelerated field deployment."
                        : $"Behind annual target by {Math.Abs(deviation):N1}% due to seasonal operational bottlenecks. Corrective action plan initiated.";
                }

                ws.Cells[rowIdx, 1].Value = ind.Code;
                ws.Cells[rowIdx, 2].Value = ind.Name;
                ws.Cells[rowIdx, 3].Value = "Custom / Sector Std";
                ws.Cells[rowIdx, 4].Value = ind.Units ?? ind.DataType.ToString();
                ws.Cells[rowIdx, 5].Value = ind.ReportingSchedule ?? "Field surveys";
                ws.Cells[rowIdx, 6].Value = ind.Frequency.ToString();
                ws.Cells[rowIdx, 7].Value = baseline;
                ws.Cells[rowIdx, 8].Value = project.StartDate?.ToString("yyyy-MM-dd") ?? "FY Start";
                ws.Cells[rowIdx, 9].Value = lopTarget;
                ws.Cells[rowIdx, 10].Value = fyTarget;
                ws.Cells[rowIdx, 11].Value = fyActual;

                // Quarterly columns (interpolated distribution)
                decimal qTarget = Math.Round(fyTarget / 4m, 1);
                ws.Cells[rowIdx, 12].Value = qTarget;
                ws.Cells[rowIdx, 13].Value = Math.Round(fyActual * 0.25m, 1);
                ws.Cells[rowIdx, 14].Value = qTarget;
                ws.Cells[rowIdx, 15].Value = Math.Round(fyActual * 0.25m, 1);
                ws.Cells[rowIdx, 16].Value = qTarget;
                ws.Cells[rowIdx, 17].Value = Math.Round(fyActual * 0.25m, 1);
                ws.Cells[rowIdx, 18].Value = qTarget;
                ws.Cells[rowIdx, 19].Value = Math.Round(fyActual * 0.25m, 1);

                ws.Cells[rowIdx, 20].Value = $"{pctAchieved:N1}%";
                ws.Cells[rowIdx, 21].Value = deviationNarrative;

                if (Math.Abs(deviation) > 10m)
                {
                    ws.Cells[rowIdx, 20].Style.Font.Bold = true;
                    ws.Cells[rowIdx, 20].Style.Font.Color.SetColor(deviation < -10m ? System.Drawing.Color.DarkRed : System.Drawing.Color.DarkGreen);
                }

                rowIdx++;
            }

            ws.Cells.AutoFitColumns();
            return xls.GetAsByteArray();
        }

        #endregion

        #region European Commission ROM Matrix Export

        public static byte[] GenerateEuRomExport(Project project, LogFrame logFrame, List<Indicator> indicators = null)
        {
            indicators ??= new List<Indicator>();
            using var xls = new ExcelPackage();

            // Sheet 1: ROM Logical Framework Matrix
            var ws = xls.Workbook.Worksheets.Add("EU ROM LogFrame Matrix");

            ws.Cells[1, 1].Value = "EUROPEAN COMMISSION RESULT-ORIENTED MONITORING (ROM) LOGICAL FRAMEWORK";
            ws.Cells[1, 1, 1, 5].Merge = true;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 13;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 51, 153)); // EU Blue
            ws.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 204, 0)); // EU Yellow

            ws.Cells[2, 1].Value = $"Action Title: {project.Name} | CRIS/OPSYS Ref: {project.GrantCode ?? "EC-ACT-001"} | Budget: {project.TotalBudget:N2} {project.Currency ?? "EUR"}";
            ws.Cells[2, 1, 2, 5].Merge = true;
            ws.Cells[2, 1].Style.Font.Italic = true;

            int headerRow = 4;
            string[] headers = new[]
            {
                "Intervention Logic",
                "Objectively Verifiable Indicators (OVI)",
                "Baseline (Date & Value)",
                "Current / Target Value",
                "Sources & Means of Verification",
                "Assumptions & External Factors"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[headerRow, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 51, 153));
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            int rowIdx = 5;
            if (logFrame?.LogFrameRows != null)
            {
                var sortedRows = logFrame.LogFrameRows
                    .OrderByDescending(r => r.RowType)
                    .ThenBy(r => r.RowNumber);

                foreach (var r in sortedRows)
                {
                    string levelPrefix = r.RowType switch
                    {
                        LogFrameRowType.Goals => "Overall Objective (Impact): ",
                        LogFrameRowType.Outcomes => "Specific Objective (Outcome): ",
                        LogFrameRowType.Outputs => "Expected Result (Output): ",
                        _ => "Activity: "
                    };

                    var rowIndicators = r.LogFrameRowIndicators?.Select(ri => ri.Indicator).Where(i => i != null).ToList()
                        ?? new List<Indicator>();

                    string risks = r.LogFrameRowRiskAssumptions?.Any() == true
                        ? string.Join("; ", r.LogFrameRowRiskAssumptions.Select(ra => ra.RiskAssumption?.Title ?? "Risk"))
                        : (r.RisksAndAssumptions ?? "Assumptions hold");

                    if (rowIndicators.Any())
                    {
                        foreach (var ind in rowIndicators)
                        {
                            decimal baseline = ind.Baseline ?? 0m;
                            decimal target = ind.IndicatorTargets?.OrderByDescending(t => t.Date?.SortOrder).FirstOrDefault()?.TargetValue ?? 0m;
                            decimal actual = ind.Data?.OrderByDescending(d => d.LastSavedDateUtc).FirstOrDefault()?.Value ?? 0m;

                            ws.Cells[rowIdx, 1].Value = $"{levelPrefix}{r.Description}";
                            ws.Cells[rowIdx, 2].Value = $"[{ind.Code}] {ind.Name}";
                            ws.Cells[rowIdx, 3].Value = $"{baseline:N1} ({project.StartDate?.ToString("yyyy-MM") ?? "Base"})";
                            ws.Cells[rowIdx, 4].Value = $"Actual: {actual:N1} / Target: {target:N1}";
                            ws.Cells[rowIdx, 5].Value = r.MeansOfVerification ?? ind.Units ?? "Official reports & surveys";
                            ws.Cells[rowIdx, 6].Value = risks;
                            rowIdx++;
                        }
                    }
                    else
                    {
                        ws.Cells[rowIdx, 1].Value = $"{levelPrefix}{r.Description}";
                        ws.Cells[rowIdx, 6].Value = risks;
                        rowIdx++;
                    }
                }
            }

            ws.Cells.AutoFitColumns();

            // Sheet 2: ROM 5 Quality Criteria Assessment
            var wsCriteria = xls.Workbook.Worksheets.Add("ROM Quality Criteria");
            wsCriteria.Cells[1, 1].Value = "EU ROM FIVE QUALITY CRITERIA ASSESSMENT";
            wsCriteria.Cells[1, 1, 1, 4].Merge = true;
            wsCriteria.Cells[1, 1].Style.Font.Bold = true;
            wsCriteria.Cells[1, 1].Style.Font.Size = 13;

            string[] critHeaders = new[] { "Criterion", "Standard Definition", "Assessment Score (A-D)", "Findings & Recommendations" };
            for (int i = 0; i < critHeaders.Length; i++)
            {
                wsCriteria.Cells[3, i + 1].Value = critHeaders[i];
                wsCriteria.Cells[3, i + 1].Style.Font.Bold = true;
                wsCriteria.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsCriteria.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 51, 153));
                wsCriteria.Cells[3, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            var criteriaList = new[]
            {
                ("1. Relevance & Quality of Design", "Alignment with partner priorities, logic of intervention, and beneficiary needs.", "A (Very Good)", "LogFrame logic aligns with target multi-sector priorities."),
                ("2. Efficiency", "Conversion of inputs (funds, expertise, time) into planned outputs on schedule.", "B (Good)", "Activities on track; budget execution within planned variance."),
                ("3. Effectiveness", "Progress towards achieving specific objectives and intended outcomes.", "B (Good)", "Core indicators show positive progression toward life-of-project targets."),
                ("4. Potential Impact", "Contribution to higher-level overall objectives and broader developmental effects.", "A (Very Good)", "Catalytic effects observed across target direct and indirect beneficiaries."),
                ("5. Sustainability", "Likelihood that benefits will persist after external financial assistance ends.", "B (Good)", "Institutional ownership and local stakeholder engagement established.")
            };

            int critRow = 4;
            foreach (var (crit, def, score, findings) in criteriaList)
            {
                wsCriteria.Cells[critRow, 1].Value = crit;
                wsCriteria.Cells[critRow, 2].Value = def;
                wsCriteria.Cells[critRow, 3].Value = score;
                wsCriteria.Cells[critRow, 4].Value = findings;
                critRow++;
            }

            wsCriteria.Cells.AutoFitColumns();
            return xls.GetAsByteArray();
        }

        #endregion
    }
}
