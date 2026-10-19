using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Monic.Web.Models;

namespace Monic.Web.Services
{
    public interface IMeReviewService
    {
        Task<MeReview> EvaluateTheoryOfChangeAsync(Guid theoryOfChangeId, Guid? userId = null);
        Task<MeReview> EvaluateLogFrameAsync(Guid logFrameId, Guid? userId = null);
        Task<MeReview> EvaluateQuestionnaireAsync(Guid questionnaireId, Guid? userId = null);
        Task<MeReview> EvaluateDataQualityAsync(Guid? indicatorId = null, Guid? userId = null);
        Task<MeReview> EvaluateIndicatorsQualityAsync(List<Guid> indicatorIds = null, Guid? userId = null);
        Task<MeReview> EvaluateSamplingPlanAsync(SamplingPlanRequest request, Guid? userId = null);
        Task<MeReview> EvaluateDocumentPdfAsync(Stream pdfStream, string domain, string title, Guid? userId = null);
        Task<MeReview> EvaluateNarrativeAsync(string narrativeText, string domain, string title, Guid? userId = null);
        List<ReviewDomainInfo> GetSupportedDomains();
        Task<ReviewDashboardStats> GetSummaryStatsAsync();
    }
}
