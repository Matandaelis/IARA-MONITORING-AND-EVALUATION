using System;
using System.Collections.Generic;

namespace Monic.Web.Models
{
    public class ReviewDomainInfo
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Standard { get; set; }
        public string Category { get; set; }
        public List<string> Criteria { get; set; } = new List<string>();
        public bool SupportsEntityEvaluation { get; set; }
        public bool SupportsPdfUpload { get; set; }
    }

    public class SamplingPlanRequest
    {
        public string Title { get; set; }
        public int PopulationSize { get; set; }
        public double MarginOfError { get; set; } = 0.05; // 5%
        public double ConfidenceLevel { get; set; } = 0.95; // 95%
        public double EstimatedProportion { get; set; } = 0.5; // 50% max variability
        public double NonResponseBufferRate { get; set; } = 0.10; // 10%
        public int? ProposedSampleSize { get; set; }
        public string SamplingMethod { get; set; } // SimpleRandom, Stratified, Cluster, Systematic
        public string StrataDetails { get; set; }
    }

    public class ReviewDashboardStats
    {
        public int TotalReviews { get; set; }
        public double AverageScore { get; set; }
        public int CriticalFindingsCount { get; set; }
        public int HighFindingsCount { get; set; }
        public int OpenRecommendationsCount { get; set; }
        public int ImplementedRecommendationsCount { get; set; }
        public Dictionary<string, int> ReviewsByDomain { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, double> AverageScoreByDomain { get; set; } = new Dictionary<string, double>();
    }
}
