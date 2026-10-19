using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Monic.Web.Code.MeReview;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class DeterministicRuleEngineTests
    {
        [Fact]
        public void TheoryOfChange_ValidDag_PassesWithHighAcyclicScore()
        {
            var toc = new TheoryOfChange { Name = "Clean Water Project" };
            var input = new Component { ComponentId = Guid.NewGuid(), Name = "Funding", ComponentType = ComponentType.Input, Description = "Grant funding" };
            var act = new Component { ComponentId = Guid.NewGuid(), Name = "Drilling", ComponentType = ComponentType.Activity, Description = "Drill boreholes" };
            var output = new Component { ComponentId = Guid.NewGuid(), Name = "Wells", ComponentType = ComponentType.Output, Description = "10 wells completed" };
            var outcome = new Component { ComponentId = Guid.NewGuid(), Name = "WaterAccess", ComponentType = ComponentType.Outcome, Description = "Clean water access" };
            var impact = new Component { ComponentId = Guid.NewGuid(), Name = "Health", ComponentType = ComponentType.Impact, Description = "Reduced waterborne disease" };

            toc.TheoryOfChangeComponents = new List<TheoryOfChangeComponent>
            {
                new TheoryOfChangeComponent { Component = input },
                new TheoryOfChangeComponent { Component = act },
                new TheoryOfChangeComponent { Component = output },
                new TheoryOfChangeComponent { Component = outcome },
                new TheoryOfChangeComponent { Component = impact }
            };

            toc.Relationships = new List<Relationship>
            {
                new Relationship { SourceComponentId = input.ComponentId, TargetComponentId = act.ComponentId },
                new Relationship { SourceComponentId = act.ComponentId, TargetComponentId = output.ComponentId },
                new Relationship { SourceComponentId = output.ComponentId, TargetComponentId = outcome.ComponentId },
                new Relationship { SourceComponentId = outcome.ComponentId, TargetComponentId = impact.ComponentId }
            };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateTheoryOfChange(toc, review);

            Assert.Equal("Approved", review.Status);
            Assert.True(review.OverallScore >= 80);
            Assert.Contains(review.Findings, f => f.Code == "TOC_DAG_VALID");
            Assert.DoesNotContain(review.Findings, f => f.Code == "TOC_CIRCULAR_LOOP");
            Assert.DoesNotContain(review.Findings, f => f.Code == "TOC_ORPHAN_NODE");
        }

        [Fact]
        public void TheoryOfChange_CycleWithUpstreamNode_CyclePathExcludesUpstreamNode()
        {
            var toc = new TheoryOfChange { Name = "Cyclic Project" };
            var leadIn = new Component { ComponentId = Guid.NewGuid(), Name = "LeadIn", ComponentType = ComponentType.Input, Description = "Initial trigger" };
            var nodeA = new Component { ComponentId = Guid.NewGuid(), Name = "NodeA", ComponentType = ComponentType.Activity, Description = "Activity A" };
            var nodeB = new Component { ComponentId = Guid.NewGuid(), Name = "NodeB", ComponentType = ComponentType.Output, Description = "Output B" };
            var nodeC = new Component { ComponentId = Guid.NewGuid(), Name = "NodeC", ComponentType = ComponentType.Outcome, Description = "Outcome C" };

            toc.TheoryOfChangeComponents = new List<TheoryOfChangeComponent>
            {
                new TheoryOfChangeComponent { Component = leadIn },
                new TheoryOfChangeComponent { Component = nodeA },
                new TheoryOfChangeComponent { Component = nodeB },
                new TheoryOfChangeComponent { Component = nodeC }
            };

            // LeadIn -> NodeA -> NodeB -> NodeC -> NodeA (Cycle is A -> B -> C -> A)
            toc.Relationships = new List<Relationship>
            {
                new Relationship { SourceComponentId = leadIn.ComponentId, TargetComponentId = nodeA.ComponentId },
                new Relationship { SourceComponentId = nodeA.ComponentId, TargetComponentId = nodeB.ComponentId },
                new Relationship { SourceComponentId = nodeB.ComponentId, TargetComponentId = nodeC.ComponentId },
                new Relationship { SourceComponentId = nodeC.ComponentId, TargetComponentId = nodeA.ComponentId }
            };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateTheoryOfChange(toc, review);

            var cycleFinding = review.Findings.FirstOrDefault(f => f.Code == "TOC_CIRCULAR_LOOP");
            Assert.NotNull(cycleFinding);
            Assert.Equal("Critical", cycleFinding.Severity);

            // LeadIn is NOT part of the cycle, so it must not appear in the cycle finding details!
            Assert.DoesNotContain("LeadIn", cycleFinding.Message);
            Assert.DoesNotContain("LeadIn", cycleFinding.LocationRef);
            // Must contain the cycle sequence
            Assert.Contains("NodeA -> NodeB -> NodeC -> NodeA", cycleFinding.Message);
        }

        [Fact]
        public void TheoryOfChange_ReverseCausality_FlaggedAsHighSeverity()
        {
            var toc = new TheoryOfChange { Name = "Inverted Logic Project" };
            var act = new Component { ComponentId = Guid.NewGuid(), Name = "Training", ComponentType = ComponentType.Activity, Description = "Conduct training" };
            var impact = new Component { ComponentId = Guid.NewGuid(), Name = "EradicatePoverty", ComponentType = ComponentType.Impact, Description = "Poverty eradicated" };

            toc.TheoryOfChangeComponents = new List<TheoryOfChangeComponent>
            {
                new TheoryOfChangeComponent { Component = impact },
                new TheoryOfChangeComponent { Component = act }
            };

            // Inverted: Impact drives Activity
            toc.Relationships = new List<Relationship>
            {
                new Relationship { SourceComponentId = impact.ComponentId, TargetComponentId = act.ComponentId }
            };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateTheoryOfChange(toc, review);

            var revFinding = review.Findings.FirstOrDefault(f => f.Code == "TOC_REVERSE_CAUSALITY");
            Assert.NotNull(revFinding);
            Assert.Equal("High", revFinding.Severity);
        }

        [Fact]
        public void Survey_SkipLogicLoop_DetectedAccurately()
        {
            var questionnaire = new Questionnaire { QuestionnaireId = Guid.NewGuid(), Name = "Household Survey", PublicCode = "HH2026" };
            var sec = new Section { SectionId = Guid.NewGuid(), QuestionnaireId = questionnaire.QuestionnaireId, Name = "Demographics", SortOrder = 1 };

            var q1 = new Question { QuestionId = Guid.NewGuid(), SectionId = sec.SectionId, Code = "Q1", Text = "Do you have children?", QuestionType = QuestionType.OptionList, SortOrder = 1 };
            var q2 = new Question { QuestionId = Guid.NewGuid(), SectionId = sec.SectionId, Code = "Q2", Text = "How many school children?", QuestionType = QuestionType.Text, SortOrder = 2, CheckQuestionId = q1.QuestionId };
            // Q1 conditionally depends on Q2 -> circular skip loop!
            q1.CheckQuestionId = q2.QuestionId;

            var questions = new List<Question> { q1, q2 };
            var review = new MeReview();

            DeterministicRuleEngine.EvaluateQuestionnaire(questionnaire, new List<Section> { sec }, questions, new List<SkipLogicOption>(), new List<QuestionOptionGroup>(), review);

            var loopFinding = review.Findings.FirstOrDefault(f => f.Code == "SURVEY_SKIP_LOOP");
            Assert.NotNull(loopFinding);
            Assert.Equal("Critical", loopFinding.Severity);
        }

        [Fact]
        public void Survey_PublicSurveyWithoutConsent_FlaggedAsHighSeverity()
        {
            var questionnaire = new Questionnaire { QuestionnaireId = Guid.NewGuid(), Name = "Public Survey", PublicCode = "PUB123" };
            var sec = new Section { SectionId = Guid.NewGuid(), QuestionnaireId = questionnaire.QuestionnaireId, Name = "Main", SortOrder = 1 };
            var q1 = new Question { QuestionId = Guid.NewGuid(), SectionId = sec.SectionId, Code = "Q1", Text = "What is your income?", QuestionType = QuestionType.Text, SortOrder = 1 };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateQuestionnaire(questionnaire, new List<Section> { sec }, new List<Question> { q1 }, new List<SkipLogicOption>(), new List<QuestionOptionGroup>(), review);

            var consentFinding = review.Findings.FirstOrDefault(f => f.Code == "SURVEY_NO_CONSENT");
            Assert.NotNull(consentFinding);
            Assert.Equal("High", consentFinding.Severity);
        }

        [Fact]
        public void SamplingPlan_FinitePopulation_CochranAndYamaneCalculatedSafely()
        {
            var req = new SamplingPlanRequest
            {
                Title = "Beneficiary Survey",
                PopulationSize = 1000,
                MarginOfError = 0.05,
                ConfidenceLevel = 0.95,
                EstimatedProportion = 0.50,
                NonResponseBufferRate = 0.10,
                ProposedSampleSize = 350
            };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateSamplingPlan(req, review);

            Assert.Equal("sampling-plan", review.Domain);
            Assert.True(review.OverallScore >= 80);
            var calcFinding = review.Findings.FirstOrDefault(f => f.Code == "SAMPLE_SIZE_CALCULATION");
            Assert.NotNull(calcFinding);
            // Cochran n0 ~ 384.16, with N=1000 FPC -> ~278. With 10% buffer -> ~306. Proposed 350 is adequate.
            Assert.Contains("SAMPLE_ADEQUATE", review.Findings.Select(f => f.Code));
        }

        [Fact]
        public void SamplingPlan_PopulationSmallerThanFormula_SampleCappedAtPopulation()
        {
            var req = new SamplingPlanRequest
            {
                Title = "Small Pilot Group",
                PopulationSize = 50,
                MarginOfError = 0.05,
                ConfidenceLevel = 0.95,
                EstimatedProportion = 0.50,
                NonResponseBufferRate = 0.10,
                ProposedSampleSize = 50
            };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateSamplingPlan(req, review);

            // Proposed is 50, which is the entire population!
            Assert.DoesNotContain(review.Findings, f => f.Code == "SAMPLE_UNDERPOWERED");
        }

        [Fact]
        public void SamplingPlan_InvalidProportionOrNegative_HandledSafelyWithoutZeroDivision()
        {
            var req = new SamplingPlanRequest
            {
                Title = "Boundary Test",
                PopulationSize = 500,
                MarginOfError = 0.05,
                ConfidenceLevel = 0.95,
                EstimatedProportion = 1.0, // Invalid proportion (p=1 implies variance p*(1-p)=0)
                NonResponseBufferRate = 0.10,
                ProposedSampleSize = 200
            };

            var review = new MeReview();
            // Should not throw, should fall back to 0.5
            DeterministicRuleEngine.EvaluateSamplingPlan(req, review);

            Assert.True(review.OverallScore > 0);
        }

        [Fact]
        public void LogFrame_Standard4LevelsAndColumns_PassesWithHighScore()
        {
            var lf = new LogFrame { Name = "USAID Health LogFrame" };
            var goalRow = new LogFrameRow
            {
                RowType = LogFrameRowType.Goals,
                Description = "Reduce maternal mortality",
                Indicators = "Maternal mortality ratio per 100k",
                MeansOfVerification = "National health registry",
                RisksAndAssumptions = "Socio-political stability"
            };
            var outcomeRow = new LogFrameRow
            {
                RowType = LogFrameRowType.Outcomes,
                Description = "Increased antenatal care visits",
                Indicators = "% pregnant women attending 4+ visits",
                MeansOfVerification = "Clinic logs",
                RisksAndAssumptions = "Road infrastructure accessible"
            };
            var outputRow = new LogFrameRow
            {
                RowType = LogFrameRowType.Outputs,
                Description = "Community health workers trained",
                Indicators = "Number of health workers certified",
                MeansOfVerification = "Training certificates",
                RisksAndAssumptions = "Trainees complete full course"
            };
            var actRow = new LogFrameRow
            {
                RowType = LogFrameRowType.Activities,
                Description = "Deliver 5-day training workshops",
                Indicators = "5 training sessions delivered",
                MeansOfVerification = "Attendance rosters",
                RisksAndAssumptions = "Facilities available"
            };

            lf.LogFrameRows = new List<LogFrameRow> { goalRow, outcomeRow, outputRow, actRow };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateLogFrame(lf, review);

            Assert.Equal("Approved", review.Status);
            Assert.Equal(100, review.OverallScore);
            Assert.DoesNotContain(review.Findings, f => f.Code == "LF_MISSING_LEVEL");
            Assert.DoesNotContain(review.Findings, f => f.Code == "LF_MISSING_NARRATIVE");
            Assert.DoesNotContain(review.Findings, f => f.Code == "LF_MISSING_INDICATORS");
        }

        [Fact]
        public void LogFrame_MissingGoalLevel_FlaggedWithHighSeverity()
        {
            var lf = new LogFrame { Name = "Incomplete LogFrame" };
            var actRow = new LogFrameRow
            {
                RowType = LogFrameRowType.Activities,
                Description = "Deliver training",
                Indicators = "5 sessions",
                MeansOfVerification = "Rosters",
                RisksAndAssumptions = "Stable"
            };

            lf.LogFrameRows = new List<LogFrameRow> { actRow };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateLogFrame(lf, review);

            Assert.Contains(review.Findings, f => f.Code == "LF_MISSING_LEVEL" && f.Message.Contains("Goals"));
        }

        [Fact]
        public void DataQuality_OutlierDetection_FlagsThreeSigmaDeviations()
        {
            var indId = Guid.NewGuid();
            var ind = new Indicator { IndicatorId = indId, Name = "Monthly Vaccination Rate", IndicatorStatus = IndicatorStatus.Enabled };

            // Values: 100, 102, 98, 101, 99, 100, and an extreme outlier 950!
            var data = new List<Datum>
            {
                CreateTestDatum(indId, 100m, true, true),
                CreateTestDatum(indId, 102m, true, true),
                CreateTestDatum(indId, 98m, true, true),
                CreateTestDatum(indId, 101m, true, true),
                CreateTestDatum(indId, 99m, true, true),
                CreateTestDatum(indId, 100m, true, true),
                CreateTestDatum(indId, 950m, true, true)
            };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateDataQuality(data, new List<Indicator> { ind }, new List<DataReview>(), review);

            var outlierFinding = review.Findings.FirstOrDefault(f => f.Code == "DQA_STATISTICAL_OUTLIER");
            Assert.NotNull(outlierFinding);
            Assert.Equal("High", outlierFinding.Severity);
            Assert.Contains("950", outlierFinding.Message);
        }

        [Fact]
        public void DataQuality_VerificationBacklog_FlaggedWhenBelowThreshold()
        {
            var indId = Guid.NewGuid();
            var ind = new Indicator { IndicatorId = indId, Name = "Trained Teachers", IndicatorStatus = IndicatorStatus.Enabled };

            // 10 submitted, only 2 verified (20% < 70% threshold)
            var data = new List<Datum>();
            for (int i = 0; i < 10; i++)
            {
                data.Add(CreateTestDatum(indId, 25m, i < 2, false));
            }

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateDataQuality(data, new List<Indicator> { ind }, new List<DataReview>(), review);

            var backlogFinding = review.Findings.FirstOrDefault(f => f.Code == "DQA_VERIFY_BACKLOG");
            Assert.NotNull(backlogFinding);
            Assert.Equal("High", backlogFinding.Severity);
        }

        private static Datum CreateTestDatum(Guid indId, decimal val, bool verified, bool approved)
        {
            var d = new Datum
            {
                IndicatorId = indId,
                Value = val,
                VerifyDataReviewId = verified ? Guid.NewGuid() : null,
                ApproveDataReviewId = approved ? Guid.NewGuid() : null,
                SubmitDataReviewId = Guid.NewGuid()
            };
            typeof(Datum).GetProperty("Verified")?.SetValue(d, verified);
            typeof(Datum).GetProperty("Approved")?.SetValue(d, approved);
            typeof(Datum).GetProperty("Submitted")?.SetValue(d, true);
            return d;
        }

        [Fact]
        public void DocumentNarrative_TermsOfReference_EvaluatesOecdDacAndSections()
        {
            var torText = @"
                Terms of Reference for Final Evaluation.
                The objective is to evaluate project relevance, coherence, effectiveness, efficiency, impact, and sustainability.
                The evaluation questions and mixed methods methodology include probability sampling and qualitative focus groups.
                Respondent ethics, voluntary informed consent, and child safeguarding follow 'do no harm' principles.
                Key deliverables include an inception report, draft report, stakeholder validation, and final report with budget allocations.
            ";

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateDocumentNarrative("tor", torText, "WASH Project ToR", review);

            Assert.Equal("tor", review.Domain);
            Assert.Equal("Approved", review.Status);
            Assert.True(review.OverallScore >= 80);
            Assert.Equal(4, review.Sections.Count);
        }

        [Fact]
        public void Survey_ForwardDependency_FlaggedAsHighSeverity()
        {
            var questionnaire = new Questionnaire { QuestionnaireId = Guid.NewGuid(), Name = "Facility Survey" };
            var sec = new Section { SectionId = Guid.NewGuid(), QuestionnaireId = questionnaire.QuestionnaireId, Name = "General", SortOrder = 1 };

            // Question 1 (SortOrder 1) depends on Question 5 (SortOrder 5) -> Forward dependency!
            var q5 = new Question { QuestionId = Guid.NewGuid(), SectionId = sec.SectionId, Code = "Q5", Text = "Later Question", QuestionType = QuestionType.Text, SortOrder = 5 };
            var q1 = new Question { QuestionId = Guid.NewGuid(), SectionId = sec.SectionId, Code = "Q1", Text = "Early Question", QuestionType = QuestionType.Text, SortOrder = 1, CheckQuestionId = q5.QuestionId };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateQuestionnaire(questionnaire, new List<Section> { sec }, new List<Question> { q1, q5 }, new List<SkipLogicOption>(), new List<QuestionOptionGroup>(), review);

            var forwardFinding = review.Findings.FirstOrDefault(f => f.Code == "SURVEY_FORWARD_DEPENDENCY");
            Assert.NotNull(forwardFinding);
            Assert.Equal("High", forwardFinding.Severity);
            Assert.Contains("later in the survey", forwardFinding.Message);
        }

        [Fact]
        public void IndicatorQuality_SMARTCriteria_EvaluatesAllFourDimensions()
        {
            var subcatId = Guid.NewGuid();
            var validInd = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Name = "Number of Community Health Workers Certified",
                Code = "IND-01",
                Units = "Workers",
                Minimum = 0,
                Maximum = 500,
                SubcategoryId = subcatId,
                Frequency = DateType.Quarter,
                IndicatorStatus = IndicatorStatus.Enabled
            };

            var review = new MeReview();
            DeterministicRuleEngine.EvaluateIndicatorsQuality(new List<Indicator> { validInd }, review);

            Assert.Equal(4, review.Sections.Count);
            Assert.Equal(100, review.OverallScore);
            Assert.Contains(review.Findings, f => f.Code == "IND_RELEVANCE_VALID");
            Assert.Contains(review.Findings, f => f.Code == "IND_FREQUENCY_VALID");
        }

        [Fact]
        public void PdfTextExtractor_EmptyBytes_ReturnsEmptyString()
        {
            var res = PdfTextExtractorHelper.ExtractText((byte[])null);
            Assert.Equal(string.Empty, res);

            var res2 = PdfTextExtractorHelper.ExtractText(new byte[0]);
            Assert.Equal(string.Empty, res2);
        }

        [Fact]
        public void PdfTextExtractor_CorruptedBytes_ThrowsInvalidOperationException()
        {
            var invalidBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            Assert.Throws<InvalidOperationException>(() => PdfTextExtractorHelper.ExtractText(invalidBytes));
        }

        [Fact]
        public void PdfTextExtractor_ValidDocument_ExtractsReadableText()
        {
            // Generate a small valid PDF using iTextSharp and verify extraction
            byte[] pdfBytes;
            using (var ms = new MemoryStream())
            {
                var doc = new iTextSharp.text.Document();
                iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);
                doc.Open();
                doc.Add(new iTextSharp.text.Paragraph("MONIC WASH Monitoring and Evaluation Report 2026"));
                doc.Close();
                pdfBytes = ms.ToArray();
            }

            var text = PdfTextExtractorHelper.ExtractText(pdfBytes);
            Assert.NotEmpty(text);
            Assert.Contains("MONIC", text);
            Assert.Contains("Evaluation", text);
        }
    }
}
