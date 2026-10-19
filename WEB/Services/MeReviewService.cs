using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Monic.Web.Code.MeReview;
using Monic.Web.Models;

namespace Monic.Web.Services
{
    public class MeReviewService : IMeReviewService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IConfiguration _configuration;

        public MeReviewService(IDbContextFactory<ApplicationDbContext> dbFactory, IConfiguration configuration)
        {
            _dbFactory = dbFactory;
            _configuration = configuration;
        }

        public async Task<MeReview> EvaluateTheoryOfChangeAsync(Guid theoryOfChangeId, Guid? userId = null)
        {
            using var db = _dbFactory.CreateDbContext();

            var toc = await db.TheoriesOfChange
                .Include(t => t.TheoryOfChangeComponents)
                    .ThenInclude(tc => tc.Component)
                .Include(t => t.Relationships)
                .FirstOrDefaultAsync(t => t.TheoryOfChangeId == theoryOfChangeId);

            if (toc == null)
            {
                throw new ArgumentException($"Theory of Change {theoryOfChangeId} not found");
            }

            var review = new MeReview
            {
                CreatedById = userId
            };

            DeterministicRuleEngine.EvaluateTheoryOfChange(toc, review);

            db.MeReviews.Add(review);
            await db.SaveChangesAsync();

            return review;
        }

        public async Task<MeReview> EvaluateLogFrameAsync(Guid logFrameId, Guid? userId = null)
        {
            using var db = _dbFactory.CreateDbContext();

            var lf = await db.LogFrames
                .Include(l => l.LogFrameRows)
                    .ThenInclude(r => r.LogFrameRowIndicators)
                        .ThenInclude(ri => ri.Indicator)
                .Include(l => l.LogFrameRows)
                    .ThenInclude(r => r.LogFrameRowComponents)
                .FirstOrDefaultAsync(l => l.LogFrameId == logFrameId);

            if (lf == null)
            {
                throw new ArgumentException($"LogFrame {logFrameId} not found");
            }

            var review = new MeReview
            {
                CreatedById = userId
            };

            DeterministicRuleEngine.EvaluateLogFrame(lf, review);

            db.MeReviews.Add(review);
            await db.SaveChangesAsync();

            return review;
        }

        public async Task<MeReview> EvaluateQuestionnaireAsync(Guid questionnaireId, Guid? userId = null)
        {
            using var db = _dbFactory.CreateDbContext();

            var q = await db.Questionnaires
                .FirstOrDefaultAsync(item => item.QuestionnaireId == questionnaireId);

            if (q == null)
            {
                throw new ArgumentException($"Questionnaire {questionnaireId} not found");
            }

            var sections = await db.Sections
                .Where(s => s.QuestionnaireId == questionnaireId)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            var sectionIds = sections.Select(s => s.SectionId).ToList();

            var questions = await db.Questions
                .Include(quest => quest.QuestionOptionGroup)
                    .ThenInclude(g => g.QuestionOptions)
                .Where(quest => sectionIds.Contains(quest.SectionId))
                .OrderBy(quest => quest.SortOrder)
                .ToListAsync();

            var questionIds = questions.Select(quest => quest.QuestionId).ToList();

            var skipLogics = await db.SkipLogicOptions
                .Where(s => questionIds.Contains(s.QuestionId))
                .ToListAsync();

            var optionGroups = await db.QuestionOptionGroups
                .Include(g => g.QuestionOptions)
                .ToListAsync();

            var review = new MeReview
            {
                CreatedById = userId
            };

            DeterministicRuleEngine.EvaluateQuestionnaire(q, sections, questions, skipLogics, optionGroups, review);

            db.MeReviews.Add(review);
            await db.SaveChangesAsync();

            return review;
        }

        public async Task<MeReview> EvaluateDataQualityAsync(Guid? indicatorId = null, Guid? userId = null)
        {
            using var db = _dbFactory.CreateDbContext();

            IQueryable<Datum> dataQuery = db.Data;
            IQueryable<Indicator> indQuery = db.Indicators;

            if (indicatorId.HasValue && indicatorId.Value != Guid.Empty)
            {
                dataQuery = dataQuery.Where(d => d.IndicatorId == indicatorId.Value);
                indQuery = indQuery.Where(i => i.IndicatorId == indicatorId.Value);
            }

            var data = await dataQuery.Take(2000).ToListAsync();
            var indicators = await indQuery.ToListAsync();
            var reviews = await db.DataReviews.OrderByDescending(r => r.DateUtc).Take(50).ToListAsync();

            var review = new MeReview
            {
                CreatedById = userId,
                TargetEntityId = indicatorId
            };

            DeterministicRuleEngine.EvaluateDataQuality(data, indicators, reviews, review);

            db.MeReviews.Add(review);
            await db.SaveChangesAsync();

            return review;
        }

        public async Task<MeReview> EvaluateIndicatorsQualityAsync(List<Guid> indicatorIds = null, Guid? userId = null)
        {
            using var db = _dbFactory.CreateDbContext();

            IQueryable<Indicator> query = db.Indicators;
            if (indicatorIds != null && indicatorIds.Any())
            {
                query = query.Where(i => indicatorIds.Contains(i.IndicatorId));
            }

            var indicators = await query.ToListAsync();

            var review = new MeReview
            {
                CreatedById = userId
            };

            DeterministicRuleEngine.EvaluateIndicatorsQuality(indicators, review);

            db.MeReviews.Add(review);
            await db.SaveChangesAsync();

            return review;
        }

        public async Task<MeReview> EvaluateSamplingPlanAsync(SamplingPlanRequest request, Guid? userId = null)
        {
            using var db = _dbFactory.CreateDbContext();

            var review = new MeReview
            {
                CreatedById = userId
            };

            DeterministicRuleEngine.EvaluateSamplingPlan(request, review);

            db.MeReviews.Add(review);
            await db.SaveChangesAsync();

            return review;
        }

        public async Task<MeReview> EvaluateDocumentPdfAsync(Stream pdfStream, string domain, string title, Guid? userId = null)
        {
            using var db = _dbFactory.CreateDbContext();

            string extractedText = PdfTextExtractorHelper.ExtractText(pdfStream);
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new InvalidOperationException("Could not extract readable text from the uploaded PDF document. Please verify the document contains an embedded text layer and is not a raster image or scanned bitmap.");
            }

            var review = new MeReview
            {
                CreatedById = userId,
                DocumentSource = title
            };

            DeterministicRuleEngine.EvaluateDocumentNarrative(domain, extractedText, title, review);

            db.MeReviews.Add(review);
            await db.SaveChangesAsync();

            return review;
        }

        public async Task<MeReview> EvaluateNarrativeAsync(string narrativeText, string domain, string title, Guid? userId = null)
        {
            using var db = _dbFactory.CreateDbContext();

            var review = new MeReview
            {
                CreatedById = userId
            };

            DeterministicRuleEngine.EvaluateDocumentNarrative(domain, narrativeText, title, review);

            db.MeReviews.Add(review);
            await db.SaveChangesAsync();

            return review;
        }

        public List<ReviewDomainInfo> GetSupportedDomains()
        {
            return new List<ReviewDomainInfo>
            {
                new ReviewDomainInfo
                {
                    Key = "theory-of-change",
                    Name = "Theory of Change (ToC)",
                    Category = "Strategic Frameworks",
                    Standard = "Results-Based Management (RBM) / DFID & USAID",
                    Description = "Evaluates causal pathway logic, acyclic connectivity (cycle detection), orphan nodes, hierarchy progression, and preconditions.",
                    Criteria = new List<string> { "Causal Pathway & Hierarchy", "Cycle Detection & Graph Connectivity", "Assumptions & Preconditions", "Component Completeness" },
                    SupportsEntityEvaluation = true,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "logframe",
                    Name = "Logical Framework (LogFrame)",
                    Category = "Strategic Frameworks",
                    Standard = "European Commission & World Bank 4x4 Matrix",
                    Description = "Verifies 4-column horizontal integrity (Narrative, OVIs, MoVs, Assumptions) and 4-tier vertical hierarchy (Goal, Outcome, Output, Activity).",
                    Criteria = new List<string> { "Vertical Logic & Hierarchy", "Objectively Verifiable Indicators", "Means of Verification", "Assumptions & Risks" },
                    SupportsEntityEvaluation = true,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "indicator-quality",
                    Name = "Indicator Quality (SMART)",
                    Category = "Measurement & Metrics",
                    Standard = "USAID SMART Criteria & UNEP RBM",
                    Description = "Validates indicators against Specific, Measurable, Achievable, Relevant, and Time-bound benchmarks, baselines, and targets.",
                    Criteria = new List<string> { "Specific & Measurable Standards", "Baselines & Targets", "Causal Relevance", "Time-Bound Frequency" },
                    SupportsEntityEvaluation = true,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "survey",
                    Name = "Survey & Questionnaire Quality",
                    Category = "Field Data Collection",
                    Standard = "UN Statistical Commission / AAPOR Standards",
                    Description = "Audits digital surveys for infinite skip logic loops, informed consent/ethics statements, question neutrality, and option exhaustion.",
                    Criteria = new List<string> { "Skip Logic & Branching Integrity", "Ethics & Informed Consent", "Question Design & Option Sets", "Respondent Flow" },
                    SupportsEntityEvaluation = true,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "data-quality",
                    Name = "Data Quality Assessment (DQA)",
                    Category = "Data Governance",
                    Standard = "USAID ADS 201 5 DQA Dimensions",
                    Description = "Evaluates Validity, Reliability, Timeliness, Precision, and Integrity via verification ratios, statistical outlier detection, and backlog audits.",
                    Criteria = new List<string> { "Validity & Verification Flow", "Reliability & Precision", "Timeliness & Completeness", "Integrity & Governance Audit" },
                    SupportsEntityEvaluation = true,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "sampling-plan",
                    Name = "Sampling Plan Rigor",
                    Category = "Field Data Collection",
                    Standard = "Cochran Formula / Yamane Finite Sampling",
                    Description = "Computes required sample size, statistical power at 95% confidence and 5% margin of error, and audits non-response buffers.",
                    Criteria = new List<string> { "Sample Size Adequacy & Power", "Confidence & Margin of Error", "Non-Response Buffer", "Sampling Methodology" },
                    SupportsEntityEvaluation = true,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "progress-report",
                    Name = "Progress & Milestone Reporting",
                    Category = "Project Reporting",
                    Standard = "PMI / OECD Progress Accountability Standard",
                    Description = "Evaluates actual vs target milestone delivery, variance explanations, burn rate alignment, and adaptive mitigation actions.",
                    Criteria = new List<string> { "Milestone & Target Achievement", "Variance Analysis", "Financial Utilization", "Adaptive Next Steps" },
                    SupportsEntityEvaluation = false,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "me-plan",
                    Name = "Monitoring & Evaluation Plan (PMP)",
                    Category = "Strategic Frameworks",
                    Standard = "USAID Performance Monitoring Plan (PMP)",
                    Description = "Checks comprehensive indicator matrix, governance roles and responsibilities, data flow, quality controls, and dissemination timeline.",
                    Criteria = new List<string> { "Performance Indicator Matrix", "Roles & Responsibilities", "Data Quality Assurance", "Reporting Schedule & Budget" },
                    SupportsEntityEvaluation = false,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "tor",
                    Name = "Terms of Reference (ToR) for Evaluation",
                    Category = "Evaluation & Research",
                    Standard = "OECD-DAC Evaluation Guidelines",
                    Description = "Ensures coverage of 6 OECD-DAC evaluation criteria (Relevance, Coherence, Effectiveness, Efficiency, Impact, Sustainability), questions, and ethics.",
                    Criteria = new List<string> { "OECD-DAC Criteria Coverage", "Evaluation Questions & Methods", "Ethics & Safeguarding", "Deliverables & Governance" },
                    SupportsEntityEvaluation = false,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "evaluation-report",
                    Name = "Evaluation Report Quality",
                    Category = "Evaluation & Research",
                    Standard = "UNEG / ALNAP Quality Proforma",
                    Description = "Assesses executive summary clarity, methodology disclosure, limitation transparency, evidence-grounded findings, and actionable recommendations.",
                    Criteria = new List<string> { "Executive Summary & Context", "Methodological Rigor & Limitations", "Evidence-Based Findings", "Actionable Recommendations" },
                    SupportsEntityEvaluation = false,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "proposal",
                    Name = "Project Proposal / Grant Design",
                    Category = "Project Design",
                    Standard = "FCDO / USAID Grant Proposal Standards",
                    Description = "Audits problem statement alignment, intervention causal links, GEDI (Gender, Equity, Disability & Inclusion), and risk mitigation plans.",
                    Criteria = new List<string> { "Problem Analysis & ToC", "Results Framework Alignment", "Gender & Equity Inclusion", "Sustainability & Risk Matrix" },
                    SupportsEntityEvaluation = false,
                    SupportsPdfUpload = true
                },
                new ReviewDomainInfo
                {
                    Key = "baseline-report",
                    Name = "Baseline Study Report",
                    Category = "Evaluation & Research",
                    Standard = "UNDP / World Bank Baseline Standards",
                    Description = "Reviews pre-intervention benchmark data, representative probability sampling, disaggregation by equity groups, and target realism.",
                    Criteria = new List<string> { "Indicator Benchmark Values", "Sampling Strategy", "Disaggregated Analysis", "Target Realism" },
                    SupportsEntityEvaluation = false,
                    SupportsPdfUpload = true
                }
            };
        }

        public async Task<ReviewDashboardStats> GetSummaryStatsAsync()
        {
            using var db = _dbFactory.CreateDbContext();

            var reviews = await db.MeReviews
                .Include(r => r.Findings)
                .Include(r => r.Recommendations)
                .ToListAsync();

            var stats = new ReviewDashboardStats
            {
                TotalReviews = reviews.Count,
                AverageScore = reviews.Any() ? Math.Round(reviews.Average(r => r.OverallScore), 1) : 0,
                CriticalFindingsCount = reviews.SelectMany(r => r.Findings).Count(f => f.Severity == "Critical"),
                HighFindingsCount = reviews.SelectMany(r => r.Findings).Count(f => f.Severity == "High"),
                OpenRecommendationsCount = reviews.SelectMany(r => r.Recommendations).Count(rec => !rec.IsImplemented),
                ImplementedRecommendationsCount = reviews.SelectMany(r => r.Recommendations).Count(rec => rec.IsImplemented)
            };

            foreach (var group in reviews.GroupBy(r => r.Domain))
            {
                stats.ReviewsByDomain[group.Key] = group.Count();
                stats.AverageScoreByDomain[group.Key] = Math.Round(group.Average(r => r.OverallScore), 1);
            }

            return stats;
        }
    }
}
