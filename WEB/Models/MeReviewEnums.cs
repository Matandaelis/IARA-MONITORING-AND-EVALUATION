namespace Monic.Web.Models
{
    public enum ReviewTargetType
    {
        TheoryOfChange,
        LogFrame,
        SurveyQuestionnaire,
        DataQuality,
        IndicatorQuality,
        ProgressReport,
        SamplingPlan,
        MePlan,
        Tor,
        EvaluationReport,
        ProjectProposal,
        BaselineReport,
        DocumentPdf,
        General
    }

    public enum ReviewFindingSeverity
    {
        Critical,
        High,
        Medium,
        Low,
        Info
    }

    public enum ReviewRecommendationPriority
    {
        Immediate,
        ShortTerm,
        MediumTerm,
        Optional
    }

    public enum ReviewStatusType
    {
        Draft,
        InReview,
        Completed,
        RequiresRevision,
        Approved
    }
}
