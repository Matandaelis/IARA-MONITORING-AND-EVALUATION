using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Monic.Web.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Monic.Web.Code
{
    /// <summary>
    /// Universal M&E Plan Document Builder:
    /// Generates standard, professional donor-compliant M&E Plan documents in PDF (via iTextSharp)
    /// and multi-tab Excel workbooks (via EPPlus), compiling:
    /// 1. Project Context, Typology & Beneficiary Demographics
    /// 2. Theory of Change Causal Logic Narrative
    /// 3. Standard 4x4 Logical Framework (Intervention Logic, OVIs, MoVs, Risks/Assumptions)
    /// 4. Performance Indicator Reference Sheet (PIRS) Catalog
    /// 5. Routine Data Quality Assessment (RDQA / DQA) Protocol & Active CAPs
    /// 6. Complaints & Feedback Mechanism (CFM) Register
    /// </summary>
    public static class MePlanDocumentBuilder
    {
        #region PDF Generation (iTextSharp)

        public static byte[] GeneratePdf(
            Project project,
            LogFrame logFrame,
            List<Indicator> indicators,
            List<MonitoringAction> caps = null,
            List<BeneficiaryFeedback> feedbacks = null)
        {
            caps ??= new List<MonitoringAction>();
            feedbacks ??= new List<BeneficiaryFeedback>();

            using var ms = new MemoryStream();
            using (var doc = new iTextSharp.text.Document(PageSize.A4, 36f, 36f, 40f, 40f))
            {
                var writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // Colors
                var primaryColor = new BaseColor(24, 76, 120);     // #184C78
                var secondaryColor = new BaseColor(70, 130, 180); // Steel Blue
                var lightBg = new BaseColor(245, 247, 250);
                var darkText = new BaseColor(40, 40, 40);

                // Fonts
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, primaryColor);
                var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, primaryColor);
                var subSectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, secondaryColor);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, darkText);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, darkText);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.DarkGray);
                var headerCellFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.White);

                // Title Banner
                var titlePara = new Paragraph("MONITORING & EVALUATION PLAN (MEAL)", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 4f
                };
                doc.Add(titlePara);

                var subtitlePara = new Paragraph($"Project: {project.Name ?? "Universal M&E Framework"}", subSectionFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 14f
                };
                doc.Add(subtitlePara);

                // Section 1: Project Metadata & Administrative Profile
                doc.Add(new Paragraph("1. PROJECT PROFILE & CONTEXT", sectionFont) { SpacingAfter = 6f });

                var metaTable = new PdfPTable(4) { WidthPercentage = 100f };
                metaTable.SetWidths(new float[] { 22f, 28f, 22f, 28f });

                AddMetaCell(metaTable, "Sector / Typology:", project.Sector ?? "Multi-sectoral", boldFont, normalFont, lightBg);
                AddMetaCell(metaTable, "Primary Donor:", project.DonorName ?? "Not Specified", boldFont, normalFont, lightBg);
                AddMetaCell(metaTable, "Grant / Award Code:", project.GrantCode ?? "N/A", boldFont, normalFont, lightBg);
                AddMetaCell(metaTable, "Total Budget:", $"{(project.TotalBudget.HasValue ? project.TotalBudget.Value.ToString("N2") : "0.00")} {project.Currency ?? "USD"}", boldFont, normalFont, lightBg);
                AddMetaCell(metaTable, "Implementation Period:", $"{(project.StartDate.HasValue ? project.StartDate.Value.ToString("yyyy-MM-dd") : "TBD")} to {(project.EndDate.HasValue ? project.EndDate.Value.ToString("yyyy-MM-dd") : "TBD")}", boldFont, normalFont, lightBg);
                AddMetaCell(metaTable, "Current Phase:", project.CurrentPhase ?? "Implementation", boldFont, normalFont, lightBg);
                AddMetaCell(metaTable, "Target Beneficiaries (Direct):", project.TargetDirectBeneficiaries?.ToString("N0") ?? "0", boldFont, normalFont, lightBg);
                AddMetaCell(metaTable, "Target Beneficiaries (Indirect):", project.TargetIndirectBeneficiaries?.ToString("N0") ?? "0", boldFont, normalFont, lightBg);

                doc.Add(metaTable);
                doc.Add(new Paragraph(" ", normalFont) { SpacingAfter = 8f });

                // Section 2: Theory of Change Causal Logic Narrative
                doc.Add(new Paragraph("2. THEORY OF CHANGE & CAUSAL PATHWAY", sectionFont) { SpacingAfter = 6f });
                string tocNarrative = logFrame?.TheoryOfChange != null
                    ? $"The Theory of Change '{logFrame.TheoryOfChange.Name}' delineates the systemic causal chain whereby project inputs and activities produce deliverables (Outputs), enabling behavioral and institutional transformations (Outcomes), culminating in long-term societal impact (Goal). Key assumptions and risk mitigation actions are anchored to each intermediate result to safeguard intervention fidelity."
                    : "The intervention logic operates across four cascading levels: Activities translate resources into concrete Outputs; Outputs catalyze medium-term Outcomes; and Outcomes contribute to high-level strategic Impact. Critical assumptions and external risk factors are routinely monitored to ensure pathway validity.";

                var tocPara = new Paragraph(tocNarrative, normalFont) { SpacingAfter = 12f, Leading = 13f };
                doc.Add(tocPara);

                // Section 3: Standard 4x4 Logical Framework Matrix
                doc.Add(new Paragraph("3. LOGICAL FRAMEWORK MATRIX (4x4)", sectionFont) { SpacingAfter = 6f });

                var lfTable = new PdfPTable(6) { WidthPercentage = 100f };
                lfTable.SetWidths(new float[] { 14f, 24f, 22f, 13f, 13f, 14f });

                string[] headers = new[] { "Level", "Intervention Logic", "Indicators", "Target / Actual", "MoV", "Assumptions" };
                foreach (var h in headers)
                {
                    var cell = new PdfPCell(new Phrase(h, headerCellFont))
                    {
                        BackgroundColor = primaryColor,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5f
                    };
                    lfTable.AddCell(cell);
                }

                if (logFrame?.LogFrameRows != null && logFrame.LogFrameRows.Any())
                {
                    var sortedRows = logFrame.LogFrameRows
                        .OrderByDescending(r => r.RowType)
                        .ThenBy(r => r.RowNumber);

                    foreach (var row in sortedRows)
                    {
                        string levelLabel = row.RowType switch
                        {
                            LogFrameRowType.Goals => project.Level4Label ?? "Goal / Impact",
                            LogFrameRowType.Outcomes => project.Level3Label ?? "Outcome",
                            LogFrameRowType.Outputs => project.Level2Label ?? "Output",
                            _ => project.Level1Label ?? "Activity"
                        };

                        var rowIndicators = row.LogFrameRowIndicators?.Select(ri => ri.Indicator).Where(i => i != null).ToList() ?? new List<Indicator>();
                        string indicatorSummary = rowIndicators.Any()
                            ? string.Join("\n", rowIndicators.Select(i => $"• [{i.Code}] {i.Name}"))
                            : "None assigned";

                        string progressSummary = rowIndicators.Any()
                            ? string.Join("\n", rowIndicators.Select(i => $"T: {i.IndicatorTargets?.OrderByDescending(t => t.Date?.SortOrder).FirstOrDefault()?.TargetValue ?? 0:N1} | A: {i.Data?.OrderByDescending(d => d.LastSavedDateUtc).FirstOrDefault()?.Value ?? 0:N1}"))
                            : "-";

                        string movSummary = rowIndicators.Any()
                            ? string.Join("\n", rowIndicators.Select(i => $"• {row.MeansOfVerification ?? i.Units ?? "Field Surveys"}"))
                            : (row.MeansOfVerification ?? "Reports");

                        string risksSummary = row.LogFrameRowRiskAssumptions?.Any() == true
                            ? string.Join("\n", row.LogFrameRowRiskAssumptions.Select(ra => $"• {ra.RiskAssumption?.Title ?? "Risk"}"))
                            : (row.RisksAndAssumptions ?? "Standard environmental assumptions hold.");

                        lfTable.AddCell(CreateLfCell(levelLabel, boldFont, lightBg));
                        lfTable.AddCell(CreateLfCell(row.Description ?? "", normalFont));
                        lfTable.AddCell(CreateLfCell(indicatorSummary, smallFont));
                        lfTable.AddCell(CreateLfCell(progressSummary, smallFont));
                        lfTable.AddCell(CreateLfCell(movSummary, smallFont));
                        lfTable.AddCell(CreateLfCell(risksSummary, smallFont));
                    }
                }
                else
                {
                    var emptyCell = new PdfPCell(new Phrase("No LogFrame rows defined for this project.", normalFont))
                    {
                        Colspan = 6,
                        Padding = 8f,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    };
                    lfTable.AddCell(emptyCell);
                }

                doc.Add(lfTable);
                doc.Add(new Paragraph(" ", normalFont) { SpacingAfter = 8f });

                // Section 4: Performance Indicator Reference Sheet (PIRS) Catalog
                doc.Add(new Paragraph("4. PERFORMANCE INDICATOR REFERENCE SHEETS (PIRS)", sectionFont) { SpacingAfter = 6f });

                if (indicators != null && indicators.Any())
                {
                    foreach (var ind in indicators.Take(15))
                    {
                        var pirsTable = new PdfPTable(4) { WidthPercentage = 100f, SpacingAfter = 8f };
                        pirsTable.SetWidths(new float[] { 22f, 28f, 22f, 28f });

                        var pirsHeader = new PdfPCell(new Phrase($"PIRS: [{ind.Code}] {ind.Name}", headerCellFont))
                        {
                            Colspan = 4,
                            BackgroundColor = secondaryColor,
                            Padding = 4f
                        };
                        pirsTable.AddCell(pirsHeader);

                        AddMetaCell(pirsTable, "Operational Definition:", ind.OperationalDefinition ?? ind.Name, boldFont, smallFont, lightBg);
                        AddMetaCell(pirsTable, "Purpose & Rationale:", ind.PurposeRationale ?? "Measures intervention progress toward target outcomes.", boldFont, smallFont, lightBg);
                        AddMetaCell(pirsTable, "Data Source / Method:", ind.ReportingSchedule ?? ind.Units ?? "Field surveys & Administrative registers", boldFont, smallFont, lightBg);
                        AddMetaCell(pirsTable, "Collection Frequency:", ind.Frequency.ToString(), boldFont, smallFont, lightBg);
                        AddMetaCell(pirsTable, "Calculation Formula:", ind.CalculationFormulaNarrative ?? (ind.IndicatorType == IndicatorType.Calculated ? "Automated aggregate" : "Direct count sum"), boldFont, smallFont, lightBg);
                        AddMetaCell(pirsTable, "Data Quality Protocol:", ind.DataQualityProtocol ?? "Routine spot-checks with recount verification threshold < 5%", boldFont, smallFont, lightBg);

                        doc.Add(pirsTable);
                    }
                }
                else
                {
                    doc.Add(new Paragraph("No indicators registered in the PIRS catalog.", normalFont) { SpacingAfter = 6f });
                }

                // Section 5: Data Quality Assurance Protocol & Active CAPs
                doc.Add(new Paragraph("5. DATA QUALITY ASSURANCE (DQA) & CORRECTIVE ACTIONS", sectionFont) { SpacingAfter = 6f });
                string dqaText = "Routine Data Quality Audits (RDQA) calculate recount verification factors (VF = Recounted / Reported). Discrepancies under 5% achieve Grade A (Acceptable); 5-10% achieve Grade B; and discrepancies exceeding 10% receive Grade C (Unacceptable) and automatically trigger a Corrective Action Plan (CAP) with a mandatory 7-business-day resolution SLA.";
                doc.Add(new Paragraph(dqaText, normalFont) { SpacingAfter = 8f, Leading = 12f });

                if (caps.Any())
                {
                    var capTable = new PdfPTable(5) { WidthPercentage = 100f, SpacingAfter = 10f };
                    capTable.SetWidths(new float[] { 15f, 35f, 20f, 15f, 15f });

                    string[] capHeaders = new[] { "Action ID", "Description / Root Cause", "Notes", "Due Date", "Status" };
                    foreach (var ch in capHeaders)
                    {
                        var cell = new PdfPCell(new Phrase(ch, headerCellFont))
                        {
                            BackgroundColor = primaryColor,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 4f
                        };
                        capTable.AddCell(cell);
                    }

                    foreach (var cap in caps.Take(10))
                    {
                        string idSnippet = cap.MonitoringActionId.ToString().Substring(0, 8);
                        capTable.AddCell(CreateLfCell(idSnippet, smallFont));
                        capTable.AddCell(CreateLfCell(cap.Description ?? "", smallFont));
                        capTable.AddCell(CreateLfCell(cap.Notes ?? "", smallFont));
                        capTable.AddCell(CreateLfCell(cap.DueDate?.ToString("yyyy-MM-dd") ?? "-", smallFont));
                        capTable.AddCell(CreateLfCell(cap.Status.ToString(), smallFont));
                    }
                    doc.Add(capTable);
                }

                // Section 6: Complaints & Feedback Mechanism (CFM)
                doc.Add(new Paragraph("6. COMPLAINTS & FEEDBACK MECHANISM (CFM)", sectionFont) { SpacingAfter = 6f });
                string cfmText = "Accountability to Affected Populations (AAP) is maintained via accessible multi-channel feedback (Hotline, Community Help Desks, SMS, Suggestion Boxes). Sensitive complaints (SEAH / Child Protection / Fraud) trigger immediate auto-escalation to project oversight within 24 hours.";
                doc.Add(new Paragraph(cfmText, normalFont) { SpacingAfter = 8f, Leading = 12f });

                if (feedbacks.Any())
                {
                    var cfmTable = new PdfPTable(5) { WidthPercentage = 100f };
                    cfmTable.SetWidths(new float[] { 18f, 15f, 20f, 32f, 15f });

                    string[] cfmHeaders = new[] { "Tracking Code", "Channel", "Category", "Description", "Status" };
                    foreach (var cfmh in cfmHeaders)
                    {
                        var cell = new PdfPCell(new Phrase(cfmh, headerCellFont))
                        {
                            BackgroundColor = secondaryColor,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 4f
                        };
                        cfmTable.AddCell(cell);
                    }

                    foreach (var fb in feedbacks.Take(10))
                    {
                        cfmTable.AddCell(CreateLfCell(fb.TrackingCode ?? "", smallFont));
                        cfmTable.AddCell(CreateLfCell(fb.Channel ?? "", smallFont));
                        cfmTable.AddCell(CreateLfCell(fb.Category ?? "", smallFont));
                        cfmTable.AddCell(CreateLfCell(fb.Description ?? "", smallFont));
                        cfmTable.AddCell(CreateLfCell(fb.Status.ToString(), smallFont));
                    }
                    doc.Add(cfmTable);
                }

                // Footer note
                doc.Add(new Paragraph($"Generated by MONIC Universal M&E System on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC", smallFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 14f
                });

                doc.Close();
            }

            return ms.ToArray();
        }

        private static void AddMetaCell(PdfPTable table, string label, string value, Font boldFont, Font normalFont, BaseColor bg)
        {
            var labelCell = new PdfPCell(new Phrase(label, boldFont))
            {
                BackgroundColor = bg,
                Padding = 4f,
                BorderColor = BaseColor.LightGray
            };
            var valCell = new PdfPCell(new Phrase(value, normalFont))
            {
                Padding = 4f,
                BorderColor = BaseColor.LightGray
            };
            table.AddCell(labelCell);
            table.AddCell(valCell);
        }

        private static PdfPCell CreateLfCell(string text, Font font, BaseColor bg = null)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 4f,
                BorderColor = BaseColor.LightGray
            };
            if (bg != null) cell.BackgroundColor = bg;
            return cell;
        }

        #endregion

        #region Excel Multi-Tab Generation (EPPlus)

        public static byte[] GenerateExcel(
            Project project,
            LogFrame logFrame,
            List<Indicator> indicators,
            List<MonitoringAction> caps = null,
            List<BeneficiaryFeedback> feedbacks = null)
        {
            caps ??= new List<MonitoringAction>();
            feedbacks ??= new List<BeneficiaryFeedback>();

            using var xls = new ExcelPackage();

            // Sheet 1: Project Overview
            var wsOverview = xls.Workbook.Worksheets.Add("Project Overview");
            wsOverview.Cells[1, 1].Value = "MONIC Universal M&E Plan - Project Overview";
            wsOverview.Cells[1, 1, 1, 4].Merge = true;
            wsOverview.Cells[1, 1].Style.Font.Bold = true;
            wsOverview.Cells[1, 1].Style.Font.Size = 14;

            int rowIdx = 3;
            AddExcelMetaRow(wsOverview, ref rowIdx, "Project Name", project.Name);
            AddExcelMetaRow(wsOverview, ref rowIdx, "Sector / Typology", project.Sector ?? "Multi-sectoral");
            AddExcelMetaRow(wsOverview, ref rowIdx, "Primary Donor", project.DonorName ?? "Not specified");
            AddExcelMetaRow(wsOverview, ref rowIdx, "Grant / Award Code", project.GrantCode ?? "N/A");
            AddExcelMetaRow(wsOverview, ref rowIdx, "Total Budget", $"{(project.TotalBudget.HasValue ? project.TotalBudget.Value.ToString("N2") : "0.00")} {project.Currency ?? "USD"}");
            AddExcelMetaRow(wsOverview, ref rowIdx, "Start Date", project.StartDate?.ToString("yyyy-MM-dd") ?? "TBD");
            AddExcelMetaRow(wsOverview, ref rowIdx, "End Date", project.EndDate?.ToString("yyyy-MM-dd") ?? "TBD");
            AddExcelMetaRow(wsOverview, ref rowIdx, "Target Direct Beneficiaries", project.TargetDirectBeneficiaries?.ToString("N0") ?? "0");
            AddExcelMetaRow(wsOverview, ref rowIdx, "Target Indirect Beneficiaries", project.TargetIndirectBeneficiaries?.ToString("N0") ?? "0");
            AddExcelMetaRow(wsOverview, ref rowIdx, "Current Phase", project.CurrentPhase ?? "Implementation");
            AddExcelMetaRow(wsOverview, ref rowIdx, "Theory of Change", logFrame?.TheoryOfChange?.Name ?? "Standard Causal Pathway");
            wsOverview.Cells.AutoFitColumns();

            // Sheet 2: 4x4 LogFrame Matrix
            var wsLf = xls.Workbook.Worksheets.Add("4x4 LogFrame Matrix");
            string[] lfHeaders = new[] { "Level", "Intervention Logic", "Indicator Code", "Indicator Name", "Baseline", "Target", "Current Actual", "Progress %", "Means of Verification", "Risks & Assumptions", "Linked Tasks", "Budget Lines" };
            for (int i = 0; i < lfHeaders.Length; i++)
            {
                wsLf.Cells[1, i + 1].Value = lfHeaders[i];
                wsLf.Cells[1, i + 1].Style.Font.Bold = true;
                wsLf.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsLf.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(24, 76, 120));
                wsLf.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            int lfRow = 2;
            if (logFrame?.LogFrameRows != null)
            {
                var sortedRows = logFrame.LogFrameRows
                    .OrderByDescending(r => r.RowType)
                    .ThenBy(r => r.RowNumber);

                foreach (var r in sortedRows)
                {
                    string levelLabel = r.RowType switch
                    {
                        LogFrameRowType.Goals => project.Level4Label ?? "Goal / Impact",
                        LogFrameRowType.Outcomes => project.Level3Label ?? "Outcome",
                        LogFrameRowType.Outputs => project.Level2Label ?? "Output",
                        _ => project.Level1Label ?? "Activity"
                    };

                    var rowIndicators = r.LogFrameRowIndicators?.Select(ri => ri.Indicator).Where(i => i != null).ToList() ?? new List<Indicator>();
                    string risks = r.LogFrameRowRiskAssumptions?.Any() == true
                        ? string.Join("; ", r.LogFrameRowRiskAssumptions.Select(ra => ra.RiskAssumption?.Title ?? "Risk"))
                        : (r.RisksAndAssumptions ?? "");

                    string tasks = r.LogFrameRowTasks?.Any() == true
                        ? string.Join("; ", r.LogFrameRowTasks.Select(rt => rt.Task?.Name ?? "Task"))
                        : "";

                    string budgets = r.LogFrameRowBudgetLines?.Any() == true
                        ? string.Join("; ", r.LogFrameRowBudgetLines.Select(rb => rb.BudgetLine?.Description ?? "Budget Line"))
                        : "";

                    if (rowIndicators.Any())
                    {
                        foreach (var ind in rowIndicators)
                        {
                            decimal baseline = ind.Baseline ?? 0m;
                            decimal target = ind.IndicatorTargets?.OrderByDescending(t => t.Date?.SortOrder).FirstOrDefault()?.TargetValue ?? 0m;
                            decimal actual = ind.Data?.OrderByDescending(d => d.LastSavedDateUtc).FirstOrDefault()?.Value ?? 0m;
                            decimal progress = target != 0m ? Math.Round(actual / target * 100m, 1) : 0m;

                            wsLf.Cells[lfRow, 1].Value = levelLabel;
                            wsLf.Cells[lfRow, 2].Value = r.Description;
                            wsLf.Cells[lfRow, 3].Value = ind.Code;
                            wsLf.Cells[lfRow, 4].Value = ind.Name;
                            wsLf.Cells[lfRow, 5].Value = baseline;
                            wsLf.Cells[lfRow, 6].Value = target;
                            wsLf.Cells[lfRow, 7].Value = actual;
                            wsLf.Cells[lfRow, 8].Value = progress;
                            wsLf.Cells[lfRow, 9].Value = r.MeansOfVerification ?? ind.Units ?? "Field surveys";
                            wsLf.Cells[lfRow, 10].Value = risks;
                            wsLf.Cells[lfRow, 11].Value = tasks;
                            wsLf.Cells[lfRow, 12].Value = budgets;
                            lfRow++;
                        }
                    }
                    else
                    {
                        wsLf.Cells[lfRow, 1].Value = levelLabel;
                        wsLf.Cells[lfRow, 2].Value = r.Description;
                        wsLf.Cells[lfRow, 10].Value = risks;
                        wsLf.Cells[lfRow, 11].Value = tasks;
                        wsLf.Cells[lfRow, 12].Value = budgets;
                        lfRow++;
                    }
                }
            }
            wsLf.Cells.AutoFitColumns();

            // Sheet 3: PIRS Catalog
            var wsPirs = xls.Workbook.Worksheets.Add("PIRS Catalog");
            string[] pirsHeaders = new[] { "Code", "Indicator Name", "Operational Definition", "Purpose & Rationale", "Unit of Measure", "Direction", "Collection Frequency", "Means of Verification", "Method of Calculation", "Data Quality Protocol" };
            for (int i = 0; i < pirsHeaders.Length; i++)
            {
                wsPirs.Cells[1, i + 1].Value = pirsHeaders[i];
                wsPirs.Cells[1, i + 1].Style.Font.Bold = true;
                wsPirs.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsPirs.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(70, 130, 180));
                wsPirs.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            int pirsRow = 2;
            foreach (var ind in indicators)
            {
                wsPirs.Cells[pirsRow, 1].Value = ind.Code;
                wsPirs.Cells[pirsRow, 2].Value = ind.Name;
                wsPirs.Cells[pirsRow, 3].Value = ind.OperationalDefinition ?? ind.Name;
                wsPirs.Cells[pirsRow, 4].Value = ind.PurposeRationale;
                wsPirs.Cells[pirsRow, 5].Value = ind.Units ?? ind.DataType.ToString();
                wsPirs.Cells[pirsRow, 6].Value = ind.LowerIsBetter ? "Lower is better" : "Higher is better";
                wsPirs.Cells[pirsRow, 7].Value = ind.Frequency.ToString();
                wsPirs.Cells[pirsRow, 8].Value = ind.ReportingSchedule ?? "Field surveys";
                wsPirs.Cells[pirsRow, 9].Value = ind.CalculationFormulaNarrative;
                wsPirs.Cells[pirsRow, 10].Value = ind.DataQualityProtocol;
                pirsRow++;
            }
            wsPirs.Cells.AutoFitColumns();

            // Sheet 4: DQA Protocol & Active CAPs
            var wsCaps = xls.Workbook.Worksheets.Add("DQA & CAPs Tracker");
            string[] capHeaders = new[] { "Action ID", "Description / Root Cause", "Notes", "Due Date", "Status" };
            for (int i = 0; i < capHeaders.Length; i++)
            {
                wsCaps.Cells[1, i + 1].Value = capHeaders[i];
                wsCaps.Cells[1, i + 1].Style.Font.Bold = true;
                wsCaps.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsCaps.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(24, 76, 120));
                wsCaps.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            int capRow = 2;
            foreach (var cap in caps)
            {
                wsCaps.Cells[capRow, 1].Value = cap.MonitoringActionId.ToString().Substring(0, 8);
                wsCaps.Cells[capRow, 2].Value = cap.Description;
                wsCaps.Cells[capRow, 3].Value = cap.Notes;
                wsCaps.Cells[capRow, 4].Value = cap.DueDate?.ToString("yyyy-MM-dd");
                wsCaps.Cells[capRow, 5].Value = cap.Status.ToString();
                capRow++;
            }
            wsCaps.Cells.AutoFitColumns();

            // Sheet 5: Beneficiary Feedback CFM Register
            var wsCfm = xls.Workbook.Worksheets.Add("Beneficiary Feedback Register");
            string[] cfmHeaders = new[] { "Tracking Code", "Date Received", "Channel", "Category", "Severity", "Status", "Description", "Resolution Notes", "Resolved Date" };
            for (int i = 0; i < cfmHeaders.Length; i++)
            {
                wsCfm.Cells[1, i + 1].Value = cfmHeaders[i];
                wsCfm.Cells[1, i + 1].Style.Font.Bold = true;
                wsCfm.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsCfm.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(70, 130, 180));
                wsCfm.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            int cfmRow = 2;
            foreach (var fb in feedbacks)
            {
                wsCfm.Cells[cfmRow, 1].Value = fb.TrackingCode;
                wsCfm.Cells[cfmRow, 2].Value = fb.DateReceivedUtc.ToString("yyyy-MM-dd HH:mm");
                wsCfm.Cells[cfmRow, 3].Value = fb.Channel;
                wsCfm.Cells[cfmRow, 4].Value = fb.Category;
                wsCfm.Cells[cfmRow, 5].Value = fb.Severity.ToString();
                wsCfm.Cells[cfmRow, 6].Value = fb.Status.ToString();
                wsCfm.Cells[cfmRow, 7].Value = fb.Description;
                wsCfm.Cells[cfmRow, 8].Value = fb.ResolutionNotes;
                wsCfm.Cells[cfmRow, 9].Value = fb.ResolvedAtUtc?.ToString("yyyy-MM-dd HH:mm");
                cfmRow++;
            }
            wsCfm.Cells.AutoFitColumns();

            return xls.GetAsByteArray();
        }

        private static void AddExcelMetaRow(ExcelWorksheet ws, ref int rowIdx, string label, string value)
        {
            ws.Cells[rowIdx, 1].Value = label;
            ws.Cells[rowIdx, 1].Style.Font.Bold = true;
            ws.Cells[rowIdx, 2].Value = value;
            rowIdx++;
        }

        #endregion
    }
}
