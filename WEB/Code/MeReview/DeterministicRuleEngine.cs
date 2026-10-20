using System;
using System.Collections.Generic;
using System.Linq;
using Monic.Web.Models;

namespace Monic.Web.Code.MeReview
{
    public static class DeterministicRuleEngine
    {
        #region 1. Theory of Change Evaluation
        public static void EvaluateTheoryOfChange(TheoryOfChange toc, Models.MeReview review)
        {
            review.Title = $"Theory of Change Review: {toc.Name}";
            review.Domain = "theory-of-change";
            review.TargetType = ReviewTargetType.TheoryOfChange;
            review.TargetEntityId = toc.TheoryOfChangeId;

            var sections = new List<MeReviewSection>
            {
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "causal_pathway", Title = "Causal Pathway & Hierarchy", Order = 1, Weight = 30 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "cycle_connectivity", Title = "Cycle Detection & Graph Connectivity", Order = 2, Weight = 30 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "assumptions_preconditions", Title = "Assumptions & Preconditions", Order = 3, Weight = 20 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "completeness_clarity", Title = "Component Completeness & Clarity", Order = 4, Weight = 20 }
            };

            foreach (var s in sections) review.Sections.Add(s);

            var sCausal = sections[0];
            var sCycle = sections[1];
            var sAssumptions = sections[2];
            var sCompleteness = sections[3];

            var components = toc.TheoryOfChangeComponents.Select(tc => tc.Component).Where(c => c != null).ToList();
            var relationships = toc.Relationships.ToList();

            if (!components.Any())
            {
                AddFinding(review, sCompleteness, "TOC_EMPTY", "Critical", "Completeness",
                    "The Theory of Change contains no components.",
                    "Add Inputs, Activities, Outputs, Outcomes, and Impacts to articulate the causal mechanism.", "ToC Root");
                AddRecommendation(review, "Immediate", "Architecture",
                    "Define core intervention components from input resources up to long-term impact.",
                    "1. Define inputs.\n2. Define activities.\n3. Link outputs to outcomes.");
                FinalizeScores(review);
                return;
            }

            // A. Graph Cycle Detection (DFS)
            var adj = new Dictionary<Guid, List<Guid>>();
            foreach (var c in components) adj[c.ComponentId] = new List<Guid>();
            foreach (var r in relationships)
            {
                if (adj.ContainsKey(r.SourceComponentId) && adj.ContainsKey(r.TargetComponentId))
                {
                    adj[r.SourceComponentId].Add(r.TargetComponentId);
                }
            }

            var visited = new Dictionary<Guid, int>(); // 0: unvisited, 1: visiting, 2: visited
            var parentTrack = new Dictionary<Guid, Guid>();
            bool cycleDetected = false;

            foreach (var c in components)
            {
                if (!visited.ContainsKey(c.ComponentId) || visited[c.ComponentId] == 0)
                {
                    if (DfsCheckCycle(c.ComponentId, adj, visited, parentTrack, out List<Guid> cyclePath))
                    {
                        cycleDetected = true;
                        var componentNames = cyclePath.Select(id => components.FirstOrDefault(comp => comp.ComponentId == id)?.Name ?? id.ToString());
                        var cycleStr = string.Join(" -> ", componentNames);

                        AddFinding(review, sCycle, "TOC_CIRCULAR_LOOP", "Critical", "Acyclic Logic",
                            $"Circular causal dependency detected in pathway: {cycleStr}",
                            "Theories of Change must be directed acyclic graphs (DAGs). Circular dependencies create invalid causal deadlocks.",
                            $"Path: {cycleStr}");

                        AddRecommendation(review, "Immediate", "Causal Realignment",
                            $"Break the circular feedback loop between: {cycleStr}",
                            "1. Clarify whether intermediate components represent feedback loops or recurring iterative cycles.\n2. In formal ToCs, represent iterative feedback with time-staged milestones or separate sub-components.");
                        break;
                    }
                }
            }

            if (!cycleDetected)
            {
                AddFinding(review, sCycle, "TOC_DAG_VALID", "Info", "Acyclic Logic",
                    "Causal pathway is validly directed and free of circular dependency deadlocks.",
                    "All pathways flow monotonically forward.", "Entire Graph");
            }

            // B. Orphan / Disconnected Nodes Check
            int orphansCount = 0;
            foreach (var c in components)
            {
                bool hasIncoming = relationships.Any(r => r.TargetComponentId == c.ComponentId);
                bool hasOutgoing = relationships.Any(r => r.SourceComponentId == c.ComponentId);

                if (!hasIncoming && !hasOutgoing && components.Count > 1)
                {
                    orphansCount++;
                    AddFinding(review, sCycle, "TOC_ORPHAN_NODE", "High", "Connectivity",
                        $"Component '{c.Name}' ({c.ComponentType}) is completely disconnected with neither incoming nor outgoing links.",
                        "Every component in a Theory of Change must causally bridge an intervention to a result or condition.",
                        $"Component: {c.Name}");

                    AddRecommendation(review, "ShortTerm", "Graph Connectivity",
                        $"Connect '{c.Name}' to its causal precursors or anticipated downstream results.",
                        $"Determine what inputs/activities feed '{c.Name}' or which higher-level outcome it enables.");
                }
            }

            // C. Causal Hierarchy Direction Check (Inputs -> Activities -> Outputs -> Outcomes -> Impacts)
            var componentTypeOrder = new Dictionary<ComponentType, int>
            {
                { ComponentType.Input, 1 },
                { ComponentType.Activity, 2 },
                { ComponentType.Output, 3 },
                { ComponentType.Outcome, 4 },
                { ComponentType.Impact, 5 }
            };

            int reverseOrderCount = 0;
            foreach (var r in relationships)
            {
                var parent = components.FirstOrDefault(c => c.ComponentId == r.SourceComponentId);
                var child = components.FirstOrDefault(c => c.ComponentId == r.TargetComponentId);
                if (parent != null && child != null)
                {
                    int pRank = componentTypeOrder[parent.ComponentType];
                    int cRank = componentTypeOrder[child.ComponentType];
                    if (pRank > cRank)
                    {
                        reverseOrderCount++;
                        AddFinding(review, sCausal, "TOC_REVERSE_CAUSALITY", "High", "Causal Hierarchy",
                            $"Reverse causal relationship detected: Higher-tier '{parent.Name}' ({parent.ComponentType}) drives lower-tier '{child.Name}' ({child.ComponentType}).",
                            "In standard Results-Based Management, inputs precede activities, which produce outputs, leading to outcomes and impacts.",
                            $"Relationship: {parent.Name} -> {child.Name}");
                    }
                }
            }

            // D. Preconditions and Assumptions Check
            int componentsWithoutDesc = components.Count(c => string.IsNullOrWhiteSpace(c.Description));
            if (componentsWithoutDesc > 0)
            {
                AddFinding(review, sAssumptions, "TOC_MISSING_ASSUMPTIONS", "Medium", "Assumptions Rigor",
                    $"{componentsWithoutDesc} of {components.Count} components lack explicit assumption or narrative descriptions.",
                    "Theories of Change require articulate explanatory narrative detailing why the causal transformation holds.",
                    $"{componentsWithoutDesc} components affected");

                AddRecommendation(review, "MediumTerm", "Narrative Rigor",
                    "Document the underlying external risks and behavioral assumptions for each outcome transition.",
                    "Detail what enabling environmental, political, or social conditions must hold for activities to translate into sustainable impact.");
            }

            // Deduct scores based on findings
            if (cycleDetected) sCycle.Score = Math.Max(0, sCycle.Score - 60);
            if (orphansCount > 0) sCycle.Score = Math.Max(10, sCycle.Score - (orphansCount * 15));
            if (reverseOrderCount > 0) sCausal.Score = Math.Max(20, sCausal.Score - (reverseOrderCount * 20));
            if (componentsWithoutDesc > 0) sAssumptions.Score = Math.Max(30, sAssumptions.Score - (componentsWithoutDesc * 10));

            FinalizeScores(review);
        }

        private static bool DfsCheckCycle(Guid u, Dictionary<Guid, List<Guid>> adj, Dictionary<Guid, int> visited, Dictionary<Guid, Guid> parentTrack, out List<Guid> cyclePath)
        {
            visited[u] = 1; // visiting
            cyclePath = null;

            if (adj.ContainsKey(u))
            {
                foreach (var v in adj[u])
                {
                    if (visited.ContainsKey(v) && visited[v] == 1)
                    {
                        // Back-edge u -> v detected, forming a directed cycle!
                        // Reconstruct exact cycle path from u back to v using parentTrack: v -> ... -> u -> v
                        var path = new List<Guid>();
                        var curr = u;
                        path.Add(curr);
                        while (curr != v && parentTrack.ContainsKey(curr))
                        {
                            curr = parentTrack[curr];
                            path.Add(curr);
                        }
                        path.Reverse();
                        path.Add(v); // close the cycle: v -> ... -> u -> v
                        cyclePath = path;
                        return true;
                    }

                    if (!visited.ContainsKey(v) || visited[v] == 0)
                    {
                        parentTrack[v] = u;
                        if (DfsCheckCycle(v, adj, visited, parentTrack, out cyclePath))
                        {
                            return true;
                        }
                    }
                }
            }

            visited[u] = 2; // visited
            return false;
        }
        #endregion

        #region 2. LogFrame Evaluation
        public static void EvaluateLogFrame(LogFrame logframe, Models.MeReview review)
        {
            review.Title = $"Logical Framework Review: {logframe.Name}";
            review.Domain = "logframe";
            review.TargetType = ReviewTargetType.LogFrame;
            review.TargetEntityId = logframe.LogFrameId;

            var sections = new List<MeReviewSection>
            {
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "vertical_logic", Title = "Vertical Logic & Results Hierarchy", Order = 1, Weight = 30 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "indicator_linkages", Title = "Objectively Verifiable Indicators (OVIs)", Order = 2, Weight = 30 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "means_of_verification", Title = "Means of Verification (MoVs)", Order = 3, Weight = 20 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "assumptions_risks", Title = "Assumptions & Risk Matrix", Order = 4, Weight = 20 }
            };

            foreach (var s in sections) review.Sections.Add(s);

            var sVertical = sections[0];
            var sIndicators = sections[1];
            var sMoV = sections[2];
            var sAssumptions = sections[3];

            var rows = logframe.LogFrameRows.ToList();
            if (!rows.Any())
            {
                AddFinding(review, sVertical, "LF_EMPTY", "Critical", "Structure",
                    "Logical Framework has no rows defined.",
                    "A standard LogFrame matrix must define Goals, Outcomes, Outputs, and Activities.", "Root");
                AddRecommendation(review, "Immediate", "LogFrame Design",
                    "Populate the LogFrame matrix across all 4 standard RBM levels.",
                    "Create rows for Project Goal, Key Outcomes, Concrete Outputs, and Associated Activities.");
                FinalizeScores(review);
                return;
            }

            // A. Check for 4 Standard LogFrame Levels
            var levelTypes = rows.Select(r => r.RowType).Distinct().ToList();
            var requiredLevels = new[] { LogFrameRowType.Goals, LogFrameRowType.Outcomes, LogFrameRowType.Outputs, LogFrameRowType.Activities };
            var missingLevels = requiredLevels.Where(rl => !levelTypes.Contains(rl)).ToList();

            if (missingLevels.Any())
            {
                foreach (var ml in missingLevels)
                {
                    AddFinding(review, sVertical, "LF_MISSING_LEVEL", "High", "Vertical Hierarchy",
                        $"LogFrame is missing standard tier: {ml}",
                        $"Donors (USAID, EU, World Bank) require complete 4-level vertical logic: Goal, Outcome, Output, Activity.",
                        $"Tier: {ml}");

                    AddRecommendation(review, "ShortTerm", "Hierarchy Alignment",
                        $"Add {ml} row(s) to complete the vertical causal chain.",
                        $"Formulate clear narrative statements for {ml}.");
                }
                sVertical.Score = Math.Max(20, sVertical.Score - (missingLevels.Count * 25));
            }

            // B. 4-Column Completeness per Row (Narrative, Indicators, MoVs, Assumptions)
            int rowsMissingNarrative = 0;
            int rowsMissingIndicators = 0;
            int rowsMissingMoVs = 0;
            int rowsMissingAssumptions = 0;

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Description)) rowsMissingNarrative++;
                if (!row.LogFrameRowIndicators.Any() && string.IsNullOrWhiteSpace(row.Indicators)) rowsMissingIndicators++;
                if (string.IsNullOrWhiteSpace(row.MeansOfVerification)) rowsMissingMoVs++;
                if (string.IsNullOrWhiteSpace(row.RisksAndAssumptions)) rowsMissingAssumptions++;
            }

            if (rowsMissingNarrative > 0)
            {
                AddFinding(review, sVertical, "LF_MISSING_NARRATIVE", "Critical", "Narrative Summary",
                    $"{rowsMissingNarrative} row(s) lack a Narrative Summary.",
                    "Every level in the LogFrame must have an unambiguous narrative statement.", "Matrix Column 1");
                sVertical.Score = Math.Max(20, sVertical.Score - 30);
            }

            if (rowsMissingIndicators > 0)
            {
                AddFinding(review, sIndicators, "LF_MISSING_INDICATORS", "High", "Objectively Verifiable Indicators",
                    $"{rowsMissingIndicators} row(s) have no attached Indicators.",
                    "Every LogFrame objective and result must be tracked by at least one measurable indicator.", "Matrix Column 2");
                AddRecommendation(review, "Immediate", "Indicator Assignment",
                    "Attach specific, measurable indicators to all unmonitored LogFrame rows.",
                    "Assign primary and secondary indicators with defined quantitative/qualitative baselines and targets.");
                sIndicators.Score = Math.Max(20, sIndicators.Score - (rowsMissingIndicators * 15));
            }

            if (rowsMissingMoVs > 0)
            {
                AddFinding(review, sMoV, "LF_MISSING_MOV", "Medium", "Means of Verification",
                    $"{rowsMissingMoVs} row(s) do not define Means of Verification.",
                    "Means of Verification identify source documents, audit records, or surveys where indicator progress can be verified.", "Matrix Column 3");
                sMoV.Score = Math.Max(30, sMoV.Score - (rowsMissingMoVs * 15));
            }

            if (rowsMissingAssumptions > 0)
            {
                AddFinding(review, sAssumptions, "LF_MISSING_ASSUMPTIONS", "Medium", "Assumptions & Risks",
                    $"{rowsMissingAssumptions} row(s) omit external assumptions and risks.",
                    "Assumptions articulate the external factors that must hold true for vertical advancement between tiers.", "Matrix Column 4");
                sAssumptions.Score = Math.Max(30, sAssumptions.Score - (rowsMissingAssumptions * 15));
            }

            FinalizeScores(review);
        }
        #endregion

        #region 3. Survey & Questionnaire Quality Evaluation
        public static void EvaluateQuestionnaire(Questionnaire questionnaire, List<Section> sections, List<Question> questions, List<SkipLogicOption> skipLogics, List<QuestionOptionGroup> optionGroups, Models.MeReview review)
        {
            review.Title = $"Survey Quality Audit: {questionnaire.Name}";
            review.Domain = "survey";
            review.TargetType = ReviewTargetType.SurveyQuestionnaire;
            review.TargetEntityId = questionnaire.QuestionnaireId;

            var sSections = new List<MeReviewSection>
            {
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "skip_logic", Title = "Skip Logic & Branching Integrity", Order = 1, Weight = 35 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "ethics_consent", Title = "Ethics, Consent & Data Privacy", Order = 2, Weight = 25 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "question_design", Title = "Question Design & Option Sets", Order = 3, Weight = 25 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "survey_flow", Title = "Section Structure & Respondent Flow", Order = 4, Weight = 15 }
            };

            foreach (var s in sSections) review.Sections.Add(s);

            var sSkip = sSections[0];
            var sEthics = sSections[1];
            var sDesign = sSections[2];
            var sFlow = sSections[3];

            if (!questions.Any())
            {
                AddFinding(review, sDesign, "SURVEY_EMPTY", "Critical", "Content",
                    "Questionnaire has no questions configured.",
                    "Field surveys must configure clear thematic questions.", "Root");
                FinalizeScores(review);
                return;
            }

            // A. Skip Logic Infinite Loop / Cycle Detection
            // In MONIC, questions configure CheckQuestionId: Question Q depends on CheckQuestion
            var skipAdj = new Dictionary<Guid, List<Guid>>();
            foreach (var q in questions) skipAdj[q.QuestionId] = new List<Guid>();

            foreach (var q in questions)
            {
                if (q.CheckQuestionId.HasValue && q.CheckQuestionId.Value != Guid.Empty)
                {
                    if (skipAdj.ContainsKey(q.CheckQuestionId.Value))
                    {
                        skipAdj[q.CheckQuestionId.Value].Add(q.QuestionId);
                    }
                }
            }

            var skipVisited = new Dictionary<Guid, int>();
            var skipParent = new Dictionary<Guid, Guid>();
            bool skipCycleFound = false;

            foreach (var q in questions)
            {
                if (!skipVisited.ContainsKey(q.QuestionId) || skipVisited[q.QuestionId] == 0)
                {
                    if (DfsCheckCycle(q.QuestionId, skipAdj, skipVisited, skipParent, out List<Guid> cycle))
                    {
                        skipCycleFound = true;
                        var qCodes = cycle.Select(id => questions.FirstOrDefault(quest => quest.QuestionId == id)?.Code ?? id.ToString());
                        var loopStr = string.Join(" <-> ", qCodes);

                        AddFinding(review, sSkip, "SURVEY_SKIP_LOOP", "Critical", "Skip Logic Safety",
                            $"Circular skip-logic loop detected between questions: {loopStr}",
                            "Conditional branching loops cause questionnaire forms to freeze or trap respondents in non-terminating loops.",
                            $"Loop: {loopStr}");

                        AddRecommendation(review, "Immediate", "Branching Fix",
                            $"Break the skip-logic loop: {loopStr}",
                            "Ensure skip logic conditions always branch strictly downstream toward higher order question indices.");
                        break;
                    }
                }
            }

            int forwardDependencyCount = 0;
            foreach (var q in questions)
            {
                if (q.CheckQuestionId.HasValue && q.CheckQuestionId.Value != Guid.Empty)
                {
                    var checkQ = questions.FirstOrDefault(quest => quest.QuestionId == q.CheckQuestionId.Value);
                    if (checkQ != null && checkQ.SortOrder > q.SortOrder)
                    {
                        forwardDependencyCount++;
                        AddFinding(review, sSkip, "SURVEY_FORWARD_DEPENDENCY", "High", "Skip Logic Flow",
                            $"Question '{q.Code ?? q.Text}' depends on Question '{checkQ.Code ?? checkQ.Text}' which appears later in the survey sequence (Order {q.SortOrder} depends on Order {checkQ.SortOrder}).",
                            "Conditional skip logic must reference questions answered previously in the survey order to avoid unanswerable dependencies.",
                            $"Question: {q.Code ?? q.Text}");
                    }
                }
            }

            if (forwardDependencyCount > 0)
            {
                sSkip.Score = Math.Max(30, sSkip.Score - (forwardDependencyCount * 20));
            }
            else if (!skipCycleFound)
            {
                AddFinding(review, sSkip, "SURVEY_SKIP_CLEAN", "Info", "Skip Logic Safety",
                    "Conditional skip logic is acyclic and forward-directed.",
                    "No circular branching loops or inverted dependencies detected.", "Skip Engine");
            }

            // B. Ethics & Informed Consent Notice Check
            bool hasConsent = false;
            var consentTerms = new[] { "consent", "voluntary", "confidential", "privacy", "anonymous", "permission", "agree" };
            foreach (var q in questions)
            {
                var text = (q.Text ?? "").ToLower();
                var explanation = (q.Explanation ?? "").ToLower();
                if (consentTerms.Any(term => text.Contains(term) || explanation.Contains(term)))
                {
                    hasConsent = true;
                    break;
                }
            }

            if (!hasConsent && !string.IsNullOrWhiteSpace(questionnaire.PublicCode))
            {
                AddFinding(review, sEthics, "SURVEY_NO_CONSENT", "High", "Informed Consent",
                    "Survey lacks an explicit informed consent or privacy disclaimer.",
                    "Field surveys (especially anonymous public surveys) must include introductory statements guaranteeing informed voluntary consent, privacy rights, and purpose of data collection.",
                    "Questionnaire Introductory Block");

                AddRecommendation(review, "Immediate", "Ethical Compliance",
                    "Add an informed consent statement at the start of the questionnaire.",
                    "Include: 1) Purpose of survey, 2) Voluntary participation notice, 3) Confidentiality assurance, 4) Contact details for inquiries.");
                sEthics.Score = Math.Max(30, sEthics.Score - 50);
            }

            // C. Question Design & Option Sets
            int missingOptions = 0;
            foreach (var q in questions)
            {
                if (q.QuestionType == QuestionType.OptionList)
                {
                    bool hasGroup = q.QuestionOptionGroupId.HasValue && q.QuestionOptionGroupId.Value != Guid.Empty;
                    if (!hasGroup)
                    {
                        missingOptions++;
                        AddFinding(review, sDesign, "SURVEY_EMPTY_OPTIONS", "High", "Option Group Validity",
                            $"Question '{q.Code ?? q.Text}' is configured as OptionList but has no option group assigned.",
                            "OptionList questions without option items cannot be answered by field enumerators.",
                            $"Question: {q.Code ?? q.Text}");
                    }
                }
            }

            if (missingOptions > 0)
            {
                sDesign.Score = Math.Max(20, sDesign.Score - (missingOptions * 20));
                AddRecommendation(review, "Immediate", "Option Group Configuration",
                    "Attach reusable QuestionOptionGroup for all choice questions.",
                    "Assign valid Likert, categorical, or binary option sets.");
            }

            FinalizeScores(review);
        }
        #endregion

        #region 4. Data Quality Assessment (DQA)
        public static void EvaluateDataQuality(List<Datum> data, List<Indicator> indicators, List<DataReview> dataReviews, Models.MeReview review)
        {
            review.Title = "Data Quality Assessment (USAID DQA Standard)";
            review.Domain = "data-quality";
            review.TargetType = ReviewTargetType.DataQuality;

            var sections = new List<MeReviewSection>
            {
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "validity_verification", Title = "Validity & Verification Flow", Order = 1, Weight = 30 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "reliability_precision", Title = "Reliability & Precision (Anomaly Detection)", Order = 2, Weight = 30 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "timeliness_completeness", Title = "Timeliness & Completeness", Order = 3, Weight = 25 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "integrity_governance", Title = "Integrity & Governance Audit", Order = 4, Weight = 15 }
            };

            foreach (var s in sections) review.Sections.Add(s);

            var sValidity = sections[0];
            var sReliability = sections[1];
            var sTimeliness = sections[2];
            var sIntegrity = sections[3];

            if (!data.Any())
            {
                AddFinding(review, sTimeliness, "DQA_NO_DATA", "High", "Completeness",
                    "No monitoring data entries found in database.",
                    "Data quality assessment requires submitted indicator data points.", "Data Table");
                FinalizeScores(review);
                return;
            }

            // A. Verification & Approval Ratios
            int totalSubmitted = data.Count;
            int totalVerified = data.Count(d => d.Verified || d.VerifyDataReviewId.HasValue);
            int totalApproved = data.Count(d => d.Approved || d.ApproveDataReviewId.HasValue);

            double verifyRatio = totalSubmitted > 0 ? (double)totalVerified / totalSubmitted : 0;
            double approveRatio = totalVerified > 0 ? (double)totalApproved / totalVerified : 0;

            if (verifyRatio < 0.70)
            {
                AddFinding(review, sValidity, "DQA_VERIFY_BACKLOG", "High", "Verification Ratio",
                    $"Verification ratio is only {verifyRatio:P1} ({totalVerified}/{totalSubmitted} entries verified).",
                    "USAID DQA requires rigorous second-tier review of submitted monitoring data prior to reporting.",
                    $"Backlog: {totalSubmitted - totalVerified} unverified entries");

                AddRecommendation(review, "Immediate", "Workflow Governance",
                    "Assign M&E Officers/Managers to verify pending data entry submissions.",
                    "Process pending submissions in Data Review pipeline.");
                sValidity.Score = Math.Max(20, sValidity.Score - 35);
            }

            if (approveRatio < 0.70 && totalVerified > 0)
            {
                AddFinding(review, sValidity, "DQA_APPROVAL_BACKLOG", "Medium", "Approval Ratio",
                    $"Approval ratio is {approveRatio:P1} ({totalApproved}/{totalVerified} verified entries approved).",
                    "Verified data requires executive approval to finalize donor reporting datasets.",
                    $"Pending Approval: {totalVerified - totalApproved} entries");
                sValidity.Score = Math.Max(30, sValidity.Score - 20);
            }

            // B. Outlier Detection with Adaptive Sample-Size Threshold
            // For N < 20, max possible Z-score is mathematically <= (N-1)/sqrt(N). Standard quality control uses 2.0 stdDev for N < 20, 2.5 for N < 50, and 3.0 for N >= 50.
            var numericDataByIndicator = data.Where(d => d.Value.HasValue)
                .GroupBy(d => d.IndicatorId);

            int outlierCount = 0;
            foreach (var grp in numericDataByIndicator)
            {
                var values = grp.Select(d => (double)d.Value.Value).ToList();
                if (values.Count >= 5)
                {
                    double avg = values.Average();
                    double sumSq = values.Sum(v => Math.Pow(v - avg, 2));
                    double stdDev = Math.Sqrt(sumSq / values.Count);

                    if (stdDev > 0)
                    {
                        double thresholdMultiplier = values.Count < 20 ? 2.0 : (values.Count < 50 ? 2.5 : 3.0);
                        var outliers = values.Where(v => Math.Abs(v - avg) > (thresholdMultiplier * stdDev)).ToList();
                        var indName = indicators.FirstOrDefault(i => i.IndicatorId == grp.Key)?.Name ?? grp.Key.ToString();

                        if (outliers.Any())
                        {
                            outlierCount += outliers.Count;
                            AddFinding(review, sReliability, "DQA_STATISTICAL_OUTLIER", "High", "Precision & Outliers",
                                $"Indicator '{indName}' has {outliers.Count} statistical outlier(s) exceeding {thresholdMultiplier:F1} standard deviations (Mean: {avg:F1}, StdDev: {stdDev:F1}, Outliers: {string.Join(", ", outliers.Take(3))}).",
                                $"Values deviating > {thresholdMultiplier:F1} standard deviations indicate data entry transcription errors, unit scale shifts, or anomalies.",
                                $"Indicator: {indName}");
                        }
                        else
                        {
                            AddFinding(review, sReliability, "DQA_NO_OUTLIERS", "Info", "Precision & Outliers",
                                $"Statistical scan across {values.Count} submitted values for indicator '{indName}' detected 0 outliers exceeding {thresholdMultiplier:F1} standard deviations.",
                                "Data distribution is within expected variance limits.", $"Indicator: {indName}");
                        }
                    }
                }
            }

            if (outlierCount > 0)
            {
                sReliability.Score = Math.Max(25, sReliability.Score - (outlierCount * 15));
                AddRecommendation(review, "Immediate", "Data Cleaning",
                    "Audit identified statistical outlier values against original source documents.",
                    "Verify decimal places, unit scales, and transcription accuracy.");
            }

            // C. Inactive Indicators with 0 Data Points
            var indicatorsWithNoData = indicators.Where(i => i.IndicatorStatus == IndicatorStatus.Enabled && !data.Any(d => d.IndicatorId == i.IndicatorId)).ToList();
            if (indicatorsWithNoData.Any())
            {
                AddFinding(review, sTimeliness, "DQA_INDICATOR_UNREPORTED", "Medium", "Completeness",
                    $"{indicatorsWithNoData.Count} enabled indicators have zero reported data points.",
                    "Enabled active indicators must have ongoing periodic data entry.",
                    $"{indicatorsWithNoData.Count} indicators");
                sTimeliness.Score = Math.Max(30, sTimeliness.Score - (indicatorsWithNoData.Count * 5));
            }

            FinalizeScores(review);
        }
        #endregion

        #region 5. Sampling Plan Evaluation
        public static void EvaluateSamplingPlan(SamplingPlanRequest req, Models.MeReview review)
        {
            review.Title = $"Sampling Plan Evaluation: {req.Title ?? "Field Study"}";
            review.Domain = "sampling-plan";
            review.TargetType = ReviewTargetType.SamplingPlan;

            var sections = new List<MeReviewSection>
            {
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "sample_size_power", Title = "Sample Size Adequacy & Statistical Power", Order = 1, Weight = 40 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "confidence_precision", Title = "Confidence & Margin of Error Rigor", Order = 2, Weight = 25 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "non_response_buffer", Title = "Non-Response Attrition Buffer", Order = 3, Weight = 20 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "stratification_method", Title = "Sampling Methodology & Stratification", Order = 4, Weight = 15 }
            };

            foreach (var s in sections) review.Sections.Add(s);

            var sSize = sections[0];
            var sConfidence = sections[1];
            var sBuffer = sections[2];
            var sMethod = sections[3];

            // Statistical Formulas:
            double z = 1.96; // 95% default
            if (req.ConfidenceLevel >= 0.99) z = 2.576;
            else if (req.ConfidenceLevel >= 0.95) z = 1.96;
            else if (req.ConfidenceLevel >= 0.90) z = 1.645;

            double p = (req.EstimatedProportion > 0 && req.EstimatedProportion < 1.0) ? req.EstimatedProportion : 0.50;
            double e = (req.MarginOfError > 0 && req.MarginOfError < 0.50) ? req.MarginOfError : 0.05;

            // Cochran's Infinite Population Formula
            double n0 = (Math.Pow(z, 2) * p * (1 - p)) / Math.Pow(e, 2);

            // Finite Population Correction (if N is provided)
            double requiredSampleSize = n0;
            if (req.PopulationSize > 0)
            {
                requiredSampleSize = n0 / (1 + ((n0 - 1) / req.PopulationSize));
                if (requiredSampleSize > req.PopulationSize) requiredSampleSize = req.PopulationSize;
            }
            int requiredN = (int)Math.Ceiling(requiredSampleSize);
            if (req.PopulationSize > 0 && requiredN > req.PopulationSize) requiredN = req.PopulationSize;

            // Add non-response buffer
            double bufferRate = (req.NonResponseBufferRate >= 0 && req.NonResponseBufferRate < 1.0) ? req.NonResponseBufferRate : 0.10;
            int targetWithBuffer = (int)Math.Ceiling(requiredN * (1 + bufferRate));
            if (req.PopulationSize > 0 && targetWithBuffer > req.PopulationSize) targetWithBuffer = req.PopulationSize;

            // Yamane formula comparison for finite populations
            int yamaneN = 0;
            if (req.PopulationSize > 0)
            {
                yamaneN = (int)Math.Ceiling((double)req.PopulationSize / (1 + (req.PopulationSize * Math.Pow(e, 2))));
                if (yamaneN > req.PopulationSize) yamaneN = req.PopulationSize;
            }

            AddFinding(review, sSize, "SAMPLE_SIZE_CALCULATION", "Info", "Sample Size Formula",
                $"Calculated required sample size: {requiredN} (Cochran with FPC, Z={z}, e={e:P1}). Target with {bufferRate:P0} non-response buffer: {targetWithBuffer} respondents (Yamane reference: {yamaneN}).",
                $"Calculation parameters: Population N={req.PopulationSize}, Margin of error={e:P1}, Confidence={req.ConfidenceLevel:P0}.",
                "Statistical Formula Engine");

            // Check proposed sample size vs required
            if (req.ProposedSampleSize.HasValue && req.ProposedSampleSize.Value > 0)
            {
                int proposed = req.ProposedSampleSize.Value;
                if (proposed < requiredN)
                {
                    int deficit = requiredN - proposed;
                    AddFinding(review, sSize, "SAMPLE_UNDERPOWERED", "Critical", "Statistical Power",
                        $"Proposed sample size ({proposed}) is underpowered! Required minimum is {requiredN} (deficit: {deficit} respondents).",
                        "Underpowered surveys suffer from inflated Type II errors (false negatives) and cannot reliably detect significant programmatic differences.",
                        $"Proposed: {proposed} vs Required: {requiredN}");

                    AddRecommendation(review, "Immediate", "Sample Size Adjustment",
                        $"Increase sample size to at least {targetWithBuffer} respondents.",
                        $"Recruit {targetWithBuffer - proposed} additional respondents to achieve {req.ConfidenceLevel:P0} confidence level at {e:P1} margin of error with non-response buffer.");
                    sSize.Score = Math.Max(20, (int)(100.0 * proposed / requiredN));
                }
                else if (proposed < targetWithBuffer)
                {
                    AddFinding(review, sBuffer, "SAMPLE_BUFFER_MARGINAL", "Medium", "Attrition Buffer",
                        $"Proposed sample size ({proposed}) meets theoretical minimum ({requiredN}) but lacks recommended {bufferRate:P0} non-response buffer (Target: {targetWithBuffer}).",
                        "Field surveys in development contexts typically encounter 10-15% non-response, refusal, or unlocatable respondents.",
                        $"Buffer gap: {targetWithBuffer - proposed}");
                    sBuffer.Score = 70;
                }
                else
                {
                    AddFinding(review, sSize, "SAMPLE_ADEQUATE", "Info", "Statistical Power",
                        $"Proposed sample size ({proposed}) adequately meets or exceeds required power ({targetWithBuffer} with buffer).",
                        "Sample size provides robust statistical significance.", "Sampling Design");
                }
            }

            // Check margin of error sanity
            if (e > 0.07)
            {
                AddFinding(review, sConfidence, "SAMPLE_HIGH_MARGIN_ERROR", "High", "Precision",
                    $"Margin of error of {e:P1} is unusually wide for rigorous evaluation.",
                    "International standards recommend margin of error between 3% (0.03) and 5% (0.05).",
                    $"Margin of error: {e:P1}");
                sConfidence.Score = Math.Max(40, sConfidence.Score - 30);
            }

            FinalizeScores(review);
        }
        #endregion

        #region 6. Indicators Quality Evaluation
        public static void EvaluateIndicatorsQuality(List<Indicator> indicators, Models.MeReview review)
        {
            review.Title = $"Indicator Portfolio SMART Quality Audit ({indicators.Count} indicators)";
            review.Domain = "indicator-quality";
            review.TargetType = ReviewTargetType.IndicatorQuality;

            var sections = new List<MeReviewSection>
            {
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "smart_specific", Title = "Specific & Measurable Standards", Order = 1, Weight = 30 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "achievable_targets", Title = "Baselines, Targets & Milestones", Order = 2, Weight = 30 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "relevance_linkages", Title = "Causal Relevance & LogFrame Links", Order = 3, Weight = 20 },
                new MeReviewSection { MeReviewId = review.MeReviewId, SectionKey = "time_bound_frequency", Title = "Time-Bound Frequency & Roll-Up", Order = 4, Weight = 20 }
            };

            foreach (var s in sections) review.Sections.Add(s);

            var sSpecific = sections[0];
            var sTargets = sections[1];
            var sRelevance = sections[2];
            var sTime = sections[3];

            if (!indicators.Any())
            {
                AddFinding(review, sSpecific, "IND_EMPTY", "High", "Completeness",
                    "No indicators provided for evaluation.", "Indicator List is empty.", "Root");
                FinalizeScores(review);
                return;
            }

            int missingUnits = 0;
            int missingMinMax = 0;
            int vagueNames = 0;

            foreach (var ind in indicators)
            {
                if (string.IsNullOrWhiteSpace(ind.Name) || ind.Name.Length < 10)
                {
                    vagueNames++;
                    AddFinding(review, sSpecific, "IND_VAGUE_NAME", "Medium", "Specificity",
                        $"Indicator '{ind.Code ?? ind.Name}' has a vague or overly short title.",
                        "Indicator titles must unambiguously state what is measured, the unit, and direction of progress.",
                        $"Indicator: {ind.Code ?? ind.Name}");
                }

                if (string.IsNullOrWhiteSpace(ind.Units) && string.IsNullOrWhiteSpace(ind.Prefix) && string.IsNullOrWhiteSpace(ind.Suffix))
                {
                    missingUnits++;
                }

                if (!ind.Minimum.HasValue && !ind.Maximum.HasValue)
                {
                    missingMinMax++;
                }
            }

            if (missingUnits > 0)
            {
                AddFinding(review, sSpecific, "IND_NO_UNITS", "Medium", "Measurement Units",
                    $"{missingUnits} of {indicators.Count} indicators lack explicit measurement units, prefix, or suffix.",
                    "Indicators must specify exact units of measurement (e.g. '%', 'USD', 'Households', 'Hectares') to avoid data entry ambiguity.",
                    $"{missingUnits} indicators affected");
                sSpecific.Score = Math.Max(30, sSpecific.Score - (missingUnits * 5));
            }

            if (missingMinMax > 0)
            {
                AddFinding(review, sTargets, "IND_NO_BOUNDS", "Medium", "Feasibility Bounds",
                    $"{missingMinMax} of {indicators.Count} indicators lack minimum and maximum sanity thresholds.",
                    "Configuring minimum and maximum bounds prevents typographical outliers during field data collection.",
                    $"{missingMinMax} indicators affected");
                sTargets.Score = Math.Max(40, sTargets.Score - (missingMinMax * 5));
            }

            if (vagueNames > 0)
            {
                sSpecific.Score = Math.Max(40, sSpecific.Score - (vagueNames * 10));
            }

            // C. Causal Relevance & Taxonomy
            int unclassifiedCount = indicators.Count(i => i.SubcategoryId == Guid.Empty);
            if (unclassifiedCount > 0)
            {
                AddFinding(review, sRelevance, "IND_UNCLASSIFIED", "Medium", "Thematic Relevance",
                    $"{unclassifiedCount} of {indicators.Count} indicators lack a defined thematic subcategory classification.",
                    "Indicators must be organized into logical subcategories aligned with strategic results.",
                    $"{unclassifiedCount} indicators affected");
                sRelevance.Score = Math.Max(40, sRelevance.Score - (unclassifiedCount * 15));
            }
            else
            {
                AddFinding(review, sRelevance, "IND_RELEVANCE_VALID", "Info", "Thematic Relevance",
                    "All indicators are structured within recognized thematic subcategory classifications.",
                    "Proper taxonomical alignment with results framework.", "Entire Portfolio");
            }

            // D. Time-Bound Frequency & Roll-Up Standards
            int missingFrequencyCount = indicators.Count(i => !Enum.IsDefined(typeof(DateType), i.Frequency) || !Enum.IsDefined(typeof(AggregationType), i.DateAggregationType));
            if (missingFrequencyCount > 0)
            {
                AddFinding(review, sTime, "IND_NO_FREQUENCY", "High", "Time-Bound Cadence",
                    $"{missingFrequencyCount} indicators do not specify a valid periodic reporting frequency or aggregation rule.",
                    "Indicators must have an explicit collection cadence (e.g., Monthly, Quarterly, Annually) and date aggregation rule (Sum, MostRecent).",
                    $"{missingFrequencyCount} indicators affected");
                sTime.Score = Math.Max(30, sTime.Score - (missingFrequencyCount * 20));
            }
            else
            {
                AddFinding(review, sTime, "IND_FREQUENCY_VALID", "Info", "Time-Bound Cadence",
                    "All indicators specify standard temporal collection cadences and aggregation rules.",
                    "Clear reporting schedule defined across the indicator portfolio.", "Temporal Schedule");
            }

            FinalizeScores(review);
        }
        #endregion

        #region 7. Document & Narrative Rubric Evaluation
        public static void EvaluateDocumentNarrative(string domain, string text, string title, Models.MeReview review)
        {
            review.Title = $"{GetDomainDisplayName(domain)} Audit: {title}";
            review.Domain = domain;
            review.TargetType = GetTargetTypeFromDomain(domain);
            review.Summary = $"Automated systematic evaluation against {GetDomainDisplayName(domain)} donor and international M&E standards.";

            var rubrics = GetDomainRubrics(domain);
            var sections = new List<MeReviewSection>();
            int order = 1;
            foreach (var r in rubrics)
            {
                sections.Add(new MeReviewSection
                {
                    MeReviewId = review.MeReviewId,
                    SectionKey = r.Key,
                    Title = r.Title,
                    Order = order++,
                    Weight = r.Weight,
                    Score = 100
                });
            }

            foreach (var s in sections) review.Sections.Add(s);

            var lower = (text ?? "").ToLower();

            for (int i = 0; i < rubrics.Count; i++)
            {
                var rubric = rubrics[i];
                var section = sections[i];

                int hits = 0;
                var missingKeywords = new List<string>();

                foreach (var kw in rubric.RequiredKeywords)
                {
                    if (lower.Contains(kw.ToLower()))
                    {
                        hits++;
                    }
                    else
                    {
                        missingKeywords.Add(kw);
                    }
                }

                double hitRate = rubric.RequiredKeywords.Any() ? (double)hits / rubric.RequiredKeywords.Count : 1.0;
                section.Score = (int)Math.Round(hitRate * 100);

                if (hitRate < 0.5)
                {
                    section.Status = "Fail";
                    AddFinding(review, section, $"{domain.ToUpper()}_{rubric.Key.ToUpper()}_DEFICIENT", "High", rubric.Title,
                        $"{rubric.Title} is insufficiently articulated (Compliance: {hitRate:P0}). Missing core elements: {string.Join(", ", missingKeywords.Take(4))}.",
                        rubric.Guidance, rubric.Title);

                    AddRecommendation(review, "Immediate", rubric.Title,
                        $"Incorporate standard {rubric.Title} provisions into {title}.",
                        $"Include dedicated subsections addressing: {string.Join(", ", missingKeywords)}.");
                }
                else if (hitRate < 0.8)
                {
                    section.Status = "Warning";
                    AddFinding(review, section, $"{domain.ToUpper()}_{rubric.Key.ToUpper()}_PARTIAL", "Medium", rubric.Title,
                        $"{rubric.Title} meets basic criteria but lacks key components: {string.Join(", ", missingKeywords.Take(3))}.",
                        rubric.Guidance, rubric.Title);
                }
                else
                {
                    section.Status = "Pass";
                    AddFinding(review, section, $"{domain.ToUpper()}_{rubric.Key.ToUpper()}_PASS", "Info", rubric.Title,
                        $"{rubric.Title} thoroughly meets international quality standards ({hits}/{rubric.RequiredKeywords.Count} indicators present).",
                        "Well articulated across all core criteria.", rubric.Title);
                }
            }

            FinalizeScores(review);
        }

        private class DomainRubricDef
        {
            public string Key { get; set; }
            public string Title { get; set; }
            public int Weight { get; set; }
            public List<string> RequiredKeywords { get; set; }
            public string Guidance { get; set; }
        }

        private static List<DomainRubricDef> GetDomainRubrics(string domain)
        {
            switch (domain?.ToLower())
            {
                case "tor":
                    return new List<DomainRubricDef>
                    {
                        new DomainRubricDef { Key = "oecd_dac", Title = "OECD-DAC Evaluation Criteria", Weight = 30, RequiredKeywords = new List<string> { "relevance", "coherence", "effectiveness", "efficiency", "impact", "sustainability" }, Guidance = "OECD-DAC mandates evaluating relevance, coherence, effectiveness, efficiency, impact, and sustainability." },
                        new DomainRubricDef { Key = "questions_methodology", Title = "Evaluation Questions & Methodology", Weight = 30, RequiredKeywords = new List<string> { "methodology", "questions", "sampling", "data collection", "mixed methods" }, Guidance = "ToRs must specify primary evaluation questions and proposed qualitative/quantitative methods." },
                        new DomainRubricDef { Key = "ethics_safeguarding", Title = "Ethics & Safeguarding", Weight = 20, RequiredKeywords = new List<string> { "ethics", "consent", "confidentiality", "safeguarding", "do no harm" }, Guidance = "Must explicitly declare ethical clearance, respondent safeguarding, and confidentiality procedures." },
                        new DomainRubricDef { Key = "deliverables_timeline", Title = "Deliverables, Timeline & Governance", Weight = 20, RequiredKeywords = new List<string> { "deliverables", "inception report", "draft report", "timeline", "budget" }, Guidance = "Must define inception report, draft report, validation workshop, and final report milestones." }
                    };

                case "evaluation-report":
                    return new List<DomainRubricDef>
                    {
                        new DomainRubricDef { Key = "exec_summary", Title = "Executive Summary & Context", Weight = 20, RequiredKeywords = new List<string> { "executive summary", "background", "context", "objective" }, Guidance = "Reports must include a comprehensive standalone executive summary." },
                        new DomainRubricDef { Key = "methodology_limitations", Title = "Methodological Rigor & Limitations", Weight = 30, RequiredKeywords = new List<string> { "methodology", "sampling", "limitations", "triangulation", "bias" }, Guidance = "Must detail sampling size, data collection methods, and explicitly disclose evaluation limitations." },
                        new DomainRubricDef { Key = "evidence_findings", Title = "Evidence-Based Findings", Weight = 30, RequiredKeywords = new List<string> { "findings", "evidence", "results", "analysis", "data" }, Guidance = "All findings must be empirically grounded in primary or secondary data." },
                        new DomainRubricDef { Key = "actionable_recommendations", Title = "Actionable Recommendations & Lessons", Weight = 20, RequiredKeywords = new List<string> { "recommendations", "lessons learned", "actionable", "prioritized" }, Guidance = "Recommendations must be prioritized, targeted to specific stakeholders, and feasible." }
                    };

                case "me-plan":
                    return new List<DomainRubricDef>
                    {
                        new DomainRubricDef { Key = "pmp_matrix", Title = "Performance Indicator Matrix", Weight = 35, RequiredKeywords = new List<string> { "indicator", "baseline", "target", "frequency", "unit" }, Guidance = "Performance Monitoring Plan must specify indicator definition, baseline, target, and measurement unit." },
                        new DomainRubricDef { Key = "roles_responsibilities", Title = "Roles, Responsibilities & Governance", Weight = 25, RequiredKeywords = new List<string> { "responsible", "role", "collection", "focal point", "data management" }, Guidance = "Specify designated entity or officer responsible for collecting, verifying, and reporting each indicator." },
                        new DomainRubricDef { Key = "dqa_quality_control", Title = "Data Quality Assurance Procedures", Weight = 20, RequiredKeywords = new List<string> { "quality", "dqa", "verification", "audit", "validation" }, Guidance = "Document data flow, audit cadence, and verification protocols." },
                        new DomainRubricDef { Key = "reporting_budget", Title = "Reporting Schedule & Resource Budget", Weight = 20, RequiredKeywords = new List<string> { "schedule", "quarterly", "annual", "budget", "dissemination" }, Guidance = "Establish clear reporting timelines aligned with donor requirements." }
                    };

                case "progress-report":
                    return new List<DomainRubricDef>
                    {
                        new DomainRubricDef { Key = "target_achievement", Title = "Milestone & Target Achievement", Weight = 35, RequiredKeywords = new List<string> { "actual", "target", "achievement", "progress", "milestone" }, Guidance = "Contrast actual quarterly/annual achievements against planned milestone targets." },
                        new DomainRubricDef { Key = "variance_explanation", Title = "Variance Analysis & Bottlenecks", Weight = 25, RequiredKeywords = new List<string> { "variance", "delay", "challenge", "bottleneck", "reasons" }, Guidance = "Provide transparent analytical explanation for any deviations (>10%) from planned targets." },
                        new DomainRubricDef { Key = "financial_burn_rate", Title = "Financial & Resource Utilization", Weight = 20, RequiredKeywords = new List<string> { "budget", "expenditure", "burn rate", "disbursement" }, Guidance = "Align financial burn rate with physical milestone delivery." },
                        new DomainRubricDef { Key = "mitigation_next_steps", Title = "Adaptive Management & Next Steps", Weight = 20, RequiredKeywords = new List<string> { "mitigation", "adaptive", "next steps", "priorities", "work plan" }, Guidance = "Outline adaptive corrective actions for upcoming reporting periods." }
                    };

                case "proposal":
                    return new List<DomainRubricDef>
                    {
                        new DomainRubricDef { Key = "problem_statement", Title = "Problem Analysis & Theory of Change", Weight = 30, RequiredKeywords = new List<string> { "problem", "theory of change", "context", "target group", "causal" }, Guidance = "Articulate clear evidence-based needs assessment and causal solution pathway." },
                        new DomainRubricDef { Key = "logframe_alignment", Title = "Results Framework & LogFrame", Weight = 30, RequiredKeywords = new List<string> { "logframe", "outcome", "output", "indicator", "activities" }, Guidance = "Align project activities directly to measurable outputs and outcomes." },
                        new DomainRubricDef { Key = "gender_inclusion", Title = "Gender, Equity & Inclusion (GEDI)", Weight = 20, RequiredKeywords = new List<string> { "gender", "inclusion", "vulnerable", "disability", "equity" }, Guidance = "Integrate gender equality, disability, and social inclusion across activities and indicators." },
                        new DomainRubricDef { Key = "sustainability_risk", Title = "Sustainability Plan & Risk Matrix", Weight = 20, RequiredKeywords = new List<string> { "sustainability", "risk matrix", "mitigation", "exit strategy", "ownership" }, Guidance = "Provide realistic institutional sustainability and risk mitigation plans." }
                    };

                case "baseline-report":
                default:
                    return new List<DomainRubricDef>
                    {
                        new DomainRubricDef { Key = "benchmark_values", Title = "Indicator Benchmark Values", Weight = 35, RequiredKeywords = new List<string> { "baseline", "benchmark", "indicator", "value", "pre-intervention" }, Guidance = "Establish definitive pre-intervention numerical baseline values for all project indicators." },
                        new DomainRubricDef { Key = "sampling_methodology", Title = "Sampling Strategy & Representation", Weight = 25, RequiredKeywords = new List<string> { "sample", "sampling", "representative", "methodology", "survey" }, Guidance = "Document rigorous probability sampling design and response rates." },
                        new DomainRubricDef { Key = "disaggregated_findings", Title = "Disaggregated Analysis", Weight = 20, RequiredKeywords = new List<string> { "disaggregation", "gender", "age", "geographic", "region" }, Guidance = "Disaggregate findings by gender, age cohorts, geography, and socio-economic status." },
                        new DomainRubricDef { Key = "implications_targets", Title = "Target Realism & Programming Implications", Weight = 20, RequiredKeywords = new List<string> { "target", "recommendations", "implications", "programming", "adjustments" }, Guidance = "Assess whether initial project targets remain realistic in light of baseline evidence." }
                    };
            }
        }
        #endregion

        #region Helpers
        private static void AddFinding(Models.MeReview review, MeReviewSection section, string code, string severity, string criterion, string message, string details, string locationRef)
        {
            var finding = new MeReviewFinding
            {
                MeReviewId = review.MeReviewId,
                MeReviewSectionId = section?.MeReviewSectionId,
                Code = code,
                Severity = severity,
                Criterion = criterion,
                Message = message,
                Details = details,
                LocationRef = locationRef
            };
            review.Findings.Add(finding);
            section?.Findings.Add(finding);
        }

        private static void AddRecommendation(Models.MeReview review, string priority, string category, string text, string actionSteps)
        {
            var rec = new MeReviewRecommendation
            {
                MeReviewId = review.MeReviewId,
                Priority = priority,
                Category = category,
                RecommendationText = text,
                ActionSteps = actionSteps,
                IsImplemented = false
            };
            review.Recommendations.Add(rec);
        }

        private static void FinalizeScores(Models.MeReview review)
        {
            if (!review.Sections.Any())
            {
                review.OverallScore = 0;
                review.Status = "Completed";
                return;
            }

            double totalWeightedScore = 0;
            double totalWeight = 0;

            foreach (var s in review.Sections)
            {
                totalWeightedScore += s.Score * s.Weight;
                totalWeight += s.Weight;

                if (s.Score >= 80) s.Status = "Pass";
                else if (s.Score >= 50) s.Status = "Warning";
                else s.Status = "Fail";
            }

            review.OverallScore = totalWeight > 0 ? (int)Math.Round(totalWeightedScore / totalWeight) : 0;
            if (review.OverallScore >= 80) review.Status = "Approved";
            else if (review.OverallScore >= 50) review.Status = "RequiresRevision";
            else review.Status = "Draft";

            var criticalCount = review.Findings.Count(f => f.Severity == "Critical");
            var highCount = review.Findings.Count(f => f.Severity == "High");
            var mediumCount = review.Findings.Count(f => f.Severity == "Medium");

            review.Summary = $"Evaluation Score: {review.OverallScore}/100 ({review.Status}). Detected {criticalCount} Critical, {highCount} High, and {mediumCount} Medium findings across {review.Sections.Count} evaluation criteria.";
        }

        private static string GetDomainDisplayName(string domain)
        {
            switch (domain?.ToLower())
            {
                case "logframe": return "Logical Framework";
                case "theory-of-change": return "Theory of Change";
                case "indicator-quality": return "Indicator Quality";
                case "survey": return "Survey & Questionnaire";
                case "data-quality": return "Data Quality Assessment (DQA)";
                case "progress-report": return "Progress Report";
                case "sampling-plan": return "Sampling Plan";
                case "me-plan": return "Monitoring & Evaluation Plan (PMP)";
                case "tor": return "Terms of Reference (ToR)";
                case "evaluation-report": return "Evaluation Report";
                case "proposal": return "Project Proposal / Grant Design";
                case "baseline-report": return "Baseline Study Report";
                default: return domain;
            }
        }

        private static ReviewTargetType GetTargetTypeFromDomain(string domain)
        {
            switch (domain?.ToLower())
            {
                case "logframe": return ReviewTargetType.LogFrame;
                case "theory-of-change": return ReviewTargetType.TheoryOfChange;
                case "indicator-quality": return ReviewTargetType.IndicatorQuality;
                case "survey": return ReviewTargetType.SurveyQuestionnaire;
                case "data-quality": return ReviewTargetType.DataQuality;
                case "progress-report": return ReviewTargetType.ProgressReport;
                case "sampling-plan": return ReviewTargetType.SamplingPlan;
                case "me-plan": return ReviewTargetType.MePlan;
                case "tor": return ReviewTargetType.Tor;
                case "evaluation-report": return ReviewTargetType.EvaluationReport;
                case "proposal": return ReviewTargetType.ProjectProposal;
                case "baseline-report": return ReviewTargetType.BaselineReport;
                default: return ReviewTargetType.General;
            }
        }
        #endregion
    }
}
