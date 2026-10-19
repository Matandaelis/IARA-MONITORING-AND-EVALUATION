using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Services;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class MeReviewsController : BaseApiController
    {
        private readonly IMeReviewService _reviewService;

        public MeReviewsController(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            UserManager<User> um,
            AppSettings appSettings,
            IMeReviewService reviewService)
            : base(dbFactory, um, appSettings)
        {
            _reviewService = reviewService;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] MeReviewSearchOptions searchOptions)
        {
            IQueryable<MeReview> results = db.MeReviews;

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.CreatedBy);
            }

            if (searchOptions.IncludeChildren)
            {
                results = results
                    .Include(o => o.Sections)
                    .Include(o => o.Findings)
                    .Include(o => o.Recommendations);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.q))
            {
                results = results.Where(o => o.Title.Contains(searchOptions.q) || o.Summary.Contains(searchOptions.q));
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.Domain))
            {
                results = results.Where(o => o.Domain == searchOptions.Domain);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.Status))
            {
                results = results.Where(o => o.Status == searchOptions.Status);
            }

            if (searchOptions.TargetType.HasValue)
            {
                results = results.Where(o => o.TargetType == searchOptions.TargetType.Value);
            }

            if (searchOptions.TargetEntityId.HasValue)
            {
                results = results.Where(o => o.TargetEntityId == searchOptions.TargetEntityId.Value);
            }

            if (searchOptions.CreatedById.HasValue)
            {
                results = results.Where(o => o.CreatedById == searchOptions.CreatedById.Value);
            }

            if (searchOptions.MinScore.HasValue)
            {
                results = results.Where(o => o.OverallScore >= searchOptions.MinScore.Value);
            }

            if (searchOptions.MaxScore.HasValue)
            {
                results = results.Where(o => o.OverallScore <= searchOptions.MaxScore.Value);
            }

            results = results.OrderByDescending(o => o.CreatedDateUtc);

            return Ok((await GetPaginatedResponse(results, searchOptions))
                .Select(o => ModelFactory.Create(o, searchOptions.IncludeParents, searchOptions.IncludeChildren)));
        }

        [HttpGet("{meReviewId:Guid}")]
        public async Task<IActionResult> Get(Guid meReviewId)
        {
            var review = await db.MeReviews
                .Include(o => o.CreatedBy)
                .Include(o => o.Sections)
                .Include(o => o.Findings)
                .Include(o => o.Recommendations)
                .FirstOrDefaultAsync(o => o.MeReviewId == meReviewId);

            if (review == null)
            {
                return NotFound();
            }

            return Ok(ModelFactory.Create(review, includeParents: true, includeChildren: true));
        }

        [HttpGet("domains")]
        public IActionResult GetSupportedDomains()
        {
            return Ok(_reviewService.GetSupportedDomains());
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            return Ok(await _reviewService.GetSummaryStatsAsync());
        }

        [HttpPost("evaluate")]
        public async Task<IActionResult> Evaluate([FromBody] EvaluateReviewRequestDTO req)
        {
            if (req == null) return BadRequest("Request body cannot be empty");

            Guid? currentUserId = CurrentUser?.Id;
            MeReview reviewResult;

            switch (req.Domain?.ToLower())
            {
                case "theory-of-change":
                    if (!req.TargetEntityId.HasValue) return BadRequest("TargetEntityId (TheoryOfChangeId) is required for Theory of Change review");
                    reviewResult = await _reviewService.EvaluateTheoryOfChangeAsync(req.TargetEntityId.Value, currentUserId);
                    break;

                case "logframe":
                    if (!req.TargetEntityId.HasValue) return BadRequest("TargetEntityId (LogFrameId) is required for LogFrame review");
                    reviewResult = await _reviewService.EvaluateLogFrameAsync(req.TargetEntityId.Value, currentUserId);
                    break;

                case "survey":
                    if (!req.TargetEntityId.HasValue) return BadRequest("TargetEntityId (QuestionnaireId) is required for Survey review");
                    reviewResult = await _reviewService.EvaluateQuestionnaireAsync(req.TargetEntityId.Value, currentUserId);
                    break;

                case "data-quality":
                    reviewResult = await _reviewService.EvaluateDataQualityAsync(req.TargetEntityId, currentUserId);
                    break;

                case "indicator-quality":
                    reviewResult = await _reviewService.EvaluateIndicatorsQualityAsync(req.TargetEntityId.HasValue ? new System.Collections.Generic.List<Guid> { req.TargetEntityId.Value } : null, currentUserId);
                    break;

                case "sampling-plan":
                    var samplingReq = req.SamplingPlan ?? new SamplingPlanRequest { Title = req.Title };
                    reviewResult = await _reviewService.EvaluateSamplingPlanAsync(samplingReq, currentUserId);
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(req.NarrativeText))
                    {
                        return BadRequest($"NarrativeText is required for {req.Domain} evaluation without entity linkage");
                    }
                    reviewResult = await _reviewService.EvaluateNarrativeAsync(req.NarrativeText, req.Domain, req.Title ?? $"{req.Domain} Audit", currentUserId);
                    break;
            }

            return await Get(reviewResult.MeReviewId);
        }

        [HttpPost("evaluate-pdf")]
        public async Task<IActionResult> EvaluatePdf([FromForm] IFormFile file, [FromForm] string domain, [FromForm] string title)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded or file is empty");
            }

            if (string.IsNullOrWhiteSpace(domain))
            {
                domain = "evaluation-report";
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = file.FileName;
            }

            Guid? currentUserId = CurrentUser?.Id;

            try
            {
                using var stream = file.OpenReadStream();
                var review = await _reviewService.EvaluateDocumentPdfAsync(stream, domain, title, currentUserId);
                return await Get(review.MeReviewId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{meReviewId:Guid}")]
        public async Task<IActionResult> Save(Guid meReviewId, [FromBody] MeReviewDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.MeReviewId != meReviewId) return BadRequest("Id mismatch");

            var review = await db.MeReviews
                .Include(r => r.Sections)
                .Include(r => r.Findings)
                .Include(r => r.Recommendations)
                .FirstOrDefaultAsync(r => r.MeReviewId == meReviewId);

            if (review == null) return NotFound();

            ModelFactory.Hydrate(review, dto);

            // Also update any recommendation statuses if provided
            if (dto.Recommendations != null)
            {
                foreach (var recDto in dto.Recommendations)
                {
                    var existingRec = review.Recommendations.FirstOrDefault(r => r.MeReviewRecommendationId == recDto.MeReviewRecommendationId);
                    if (existingRec != null)
                    {
                        ModelFactory.Hydrate(existingRec, recDto);
                    }
                }
            }

            db.Entry(review).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return await Get(review.MeReviewId);
        }

        [HttpPost("recommendations/{recommendationId:Guid}/toggle")]
        public async Task<IActionResult> ToggleRecommendation(Guid recommendationId)
        {
            var rec = await db.MeReviewRecommendations.FirstOrDefaultAsync(r => r.MeReviewRecommendationId == recommendationId);
            if (rec == null) return NotFound();

            rec.IsImplemented = !rec.IsImplemented;
            db.Entry(rec).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return Ok(ModelFactory.Create(rec));
        }

        [HttpDelete("{meReviewId:Guid}")]
        public async Task<IActionResult> Delete(Guid meReviewId)
        {
            var review = await db.MeReviews
                .Include(r => r.Sections)
                .Include(r => r.Findings)
                .Include(r => r.Recommendations)
                .FirstOrDefaultAsync(r => r.MeReviewId == meReviewId);

            if (review == null) return NotFound();

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                db.MeReviewRecommendations.RemoveRange(review.Recommendations);
                db.MeReviewFindings.RemoveRange(review.Findings);
                db.MeReviewSections.RemoveRange(review.Sections);
                db.MeReviews.Remove(review);

                await db.SaveChangesAsync();
                transactionScope.Complete();
            }

            return Ok();
        }
    }
}
