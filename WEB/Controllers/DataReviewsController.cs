using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Services;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class DataReviewsController : BaseApiController
    {
        private readonly NotificationDispatcher notificationDispatcher;

        public DataReviewsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings,
            NotificationDispatcher notificationDispatcher)
            : base(dbFactory, um, appSettings)
        {
            this.notificationDispatcher = notificationDispatcher;
        }

        [HttpGet, AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Search([FromQuery] DataReviewSearchOptions searchOptions)
        {
            IQueryable<DataReview> results = db.DataReviews;

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.User);
            }

            if (searchOptions.IncludeChildren)
            {
                results = results.Include(o => o.DataReviewLinks);
                results = results.Include(o => o.SubmittedData);
                results = results.Include(o => o.VerifiedData);
                results = results.Include(o => o.ApprovedData);
                results = results.Include(o => o.RejectedData);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.q))
                results = results.Where(o => o.Note.Contains(searchOptions.q));

            if (searchOptions.UserId.HasValue) results = results.Where(o => o.UserId == searchOptions.UserId);
            if (searchOptions.ReviewStatus.HasValue) results = results.Where(o => o.ReviewStatus == searchOptions.ReviewStatus);

            results = results.OrderByDescending(o => o.DateUtc);

            return Ok((await GetPaginatedResponse(results, searchOptions)).Select(o => ModelFactory.Create(o, searchOptions.IncludeParents, searchOptions.IncludeChildren)));
        }

        [HttpGet("{dataReviewId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Get(Guid dataReviewId)
        {
            var dataReview = await db.DataReviews
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.DataReviewId == dataReviewId);

            if (dataReview == null)
                return NotFound();

            return Ok(ModelFactory.Create(dataReview));
        }

        [HttpPost("{dataReviewId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Save(Guid dataReviewId, [FromBody] DataReviewDTO dataReviewDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dataReviewDTO.DataReviewId != dataReviewId) return BadRequest("Id mismatch");

            var isNew = dataReviewDTO.DataReviewId == Guid.Empty;

            DataReview dataReview;
            ReviewResult? previousResult;
            if (isNew)
            {
                dataReview = new DataReview();

                previousResult = null;

                db.Entry(dataReview).State = EntityState.Added;
            }
            else
            {
                dataReview = await db.DataReviews
                    .FirstOrDefaultAsync(o => o.DataReviewId == dataReviewDTO.DataReviewId);

                if (dataReview == null)
                    return NotFound();

                previousResult = dataReview.ReviewResult;

                db.Entry(dataReview).State = EntityState.Modified;
            }

            ModelFactory.Hydrate(dataReview, dataReviewDTO);

            var resultChanged = !isNew && previousResult.HasValue && dataReview.ReviewResult != previousResult.Value;

            await db.SaveChangesAsync();

            if (resultChanged)
                await CreateReviewOutcomeNotificationAsync(dataReview);

            return await Get(dataReview.DataReviewId);
        }

        [HttpDelete("{dataReviewId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Delete(Guid dataReviewId)
        {
            var dataReview = await db.DataReviews
                .FirstOrDefaultAsync(o => o.DataReviewId == dataReviewId);

            if (dataReview == null)
                return NotFound();

            if (await db.Data.AnyAsync(o => o.SubmitDataReviewId == dataReview.DataReviewId))
                return BadRequest("Unable to delete the data review as it has related submitted data");

            if (await db.Data.AnyAsync(o => o.VerifyDataReviewId == dataReview.DataReviewId))
                return BadRequest("Unable to delete the data review as it has related verified data");

            if (await db.Data.AnyAsync(o => o.ApproveDataReviewId == dataReview.DataReviewId))
                return BadRequest("Unable to delete the data review as it has related approved data");

            if (await db.Data.AnyAsync(o => o.RejectDataReviewId == dataReview.DataReviewId))
                return BadRequest("Unable to delete the data review as it has related rejected data");

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                await db.DataReviewLinks.Where(o => o.DataReviewId == dataReview.DataReviewId).ExecuteDeleteAsync();

                db.Entry(dataReview).State = EntityState.Deleted;

                await db.SaveChangesAsync();

                transactionScope.Complete();
            }

            return Ok();
        }

        /// <summary>Notifies the review's submitter when the outcome (accepted/rejected) changes, unless the reviewer is the submitter.</summary>
        private async System.Threading.Tasks.Task CreateReviewOutcomeNotificationAsync(DataReview dataReview)
        {
            if (dataReview.UserId == CurrentUser.Id) return;

            var outcome = dataReview.ReviewResult == ReviewResult.Accepted ? "approved" : "rejected";

            var submitter = await db.Users
                .FirstOrDefaultAsync(o => o.Id == dataReview.UserId);

            await notificationDispatcher.DispatchAsync(
                db,
                submitter,
                "DataReview",
                $"Your submitted data has been {outcome}",
                string.IsNullOrWhiteSpace(dataReview.Note) ? null : dataReview.Note.Substring(0, Math.Min(dataReview.Note.Length, 300)),
                "/dataentry",
                CurrentUser.Id);
        }

    }
}
