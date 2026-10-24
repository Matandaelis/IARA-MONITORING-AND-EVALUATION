using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]")]
    public class BeneficiaryFeedbackController : BaseApiController
    {
        public BeneficiaryFeedbackController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        private IQueryable<BeneficiaryFeedback> FeedbackWithParents()
        {
            return db.BeneficiaryFeedbacks
                .Include(o => o.Project)
                .Include(o => o.AssignedToUser);
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] Guid? projectId = null,
            [FromQuery] FeedbackStatus? status = null,
            [FromQuery] FeedbackSeverity? severity = null,
            [FromQuery] string channel = null,
            [FromQuery] string q = null,
            [FromQuery] int page = 0,
            [FromQuery] int max = 20)
        {
            if (projectId.HasValue && !await CanViewProjectAsync(projectId.Value)) return Forbid();

            var query = FeedbackWithParents().AsNoTracking();

            if (projectId.HasValue && projectId.Value != Guid.Empty)
            {
                query = query.Where(o => o.ProjectId == projectId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            if (severity.HasValue)
            {
                query = query.Where(o => o.Severity == severity.Value);
            }

            if (!string.IsNullOrWhiteSpace(channel))
            {
                query = query.Where(o => o.Channel.ToLower() == channel.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var lower = q.ToLower();
                query = query.Where(o => o.TrackingCode.ToLower().Contains(lower) ||
                                         o.Description.ToLower().Contains(lower) ||
                                         (o.ContactDetails != null && o.ContactDetails.ToLower().Contains(lower)) ||
                                         o.Category.ToLower().Contains(lower));
            }

            var items = await GetPaginatedResponse(
                query.OrderByDescending(o => o.DateReceivedUtc),
                new SearchOptions { PageIndex = page, PageSize = max });

            return Ok(items.Select(o => ModelFactory.Create(o)));
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var feedback = await FeedbackWithParents().AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            if (feedback == null) return NotFound();
            if (!await CanViewProjectAsync(feedback.ProjectId)) return Forbid();
            return Ok(ModelFactory.Create(feedback));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BeneficiaryFeedbackDTO dto)
        {
            if (dto == null) return BadRequest("Feedback data is required");
            if (dto.ProjectId == Guid.Empty) return BadRequest("ProjectId is required");
            if (!await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            var entity = new BeneficiaryFeedback
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                TrackingCode = string.IsNullOrWhiteSpace(dto.TrackingCode)
                    ? $"CFM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}"
                    : dto.TrackingCode,
                DateReceivedUtc = dto.DateReceivedUtc == default ? DateTime.UtcNow : dto.DateReceivedUtc,
                Channel = string.IsNullOrWhiteSpace(dto.Channel) ? "Community Meeting" : dto.Channel,
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category,
                Severity = dto.Severity,
                Description = dto.Description ?? "",
                IsAnonymous = dto.IsAnonymous,
                ContactDetails = dto.ContactDetails,
                Status = dto.Status,
                ResolutionNotes = dto.ResolutionNotes,
                AssignedToUserId = dto.AssignedToUserId
            };

            db.BeneficiaryFeedbacks.Add(entity);

            // Auto-trigger Corrective Action Plan (CAP) for High or Critical severity
            if (entity.Severity == FeedbackSeverity.Critical || entity.Severity == FeedbackSeverity.High)
            {
                var cap = new MonitoringAction
                {
                    MonitoringActionId = Guid.NewGuid(),
                    ProjectId = entity.ProjectId,
                    Description = $"[CFM - {entity.TrackingCode}] {entity.Severity} Beneficiary Complaint: {entity.Description}",
                    Status = MonitoringActionStatus.Open,
                    DueDate = DateTime.UtcNow.AddDays(entity.Severity == FeedbackSeverity.Critical ? 3 : 7),
                    TriggerSource = "CFM",
                    TriggeredOnUtc = DateTime.UtcNow,
                    CreatedOnUtc = DateTime.UtcNow,
                    OwnerUserId = entity.AssignedToUserId
                };
                db.MonitoringActions.Add(cap);
            }

            await db.SaveChangesAsync();

            var saved = await FeedbackWithParents().AsNoTracking().FirstAsync(o => o.Id == entity.Id);
            return StatusCode(201, ModelFactory.Create(saved));
        }

        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] BeneficiaryFeedbackDTO dto)
        {
            if (dto == null) return BadRequest();
            var feedback = await db.BeneficiaryFeedbacks.FirstOrDefaultAsync(o => o.Id == id);
            if (feedback == null) return NotFound();
            if (!await CanViewProjectAsync(feedback.ProjectId)) return Forbid();

            var previousStatus = feedback.Status;
            ModelFactory.Hydrate(feedback, dto);

            if (feedback.Status == FeedbackStatus.Resolved && previousStatus != FeedbackStatus.Resolved && feedback.ResolvedAtUtc == null)
            {
                feedback.ResolvedAtUtc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            return Ok(ModelFactory.Create(feedback));
        }

        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var feedback = await db.BeneficiaryFeedbacks.FirstOrDefaultAsync(o => o.Id == id);
            if (feedback == null) return NotFound();
            if (!await CanViewProjectAsync(feedback.ProjectId)) return Forbid();

            db.BeneficiaryFeedbacks.Remove(feedback);
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] Guid? projectId = null)
        {
            if (projectId.HasValue && !await CanViewProjectAsync(projectId.Value)) return Forbid();

            var query = db.BeneficiaryFeedbacks.AsNoTracking();
            if (projectId.HasValue && projectId.Value != Guid.Empty)
            {
                query = query.Where(o => o.ProjectId == projectId.Value);
            }

            var list = await query.ToListAsync();

            var stats = new
            {
                Total = list.Count,
                Received = list.Count(o => o.Status == FeedbackStatus.Received),
                Investigating = list.Count(o => o.Status == FeedbackStatus.Investigating),
                Resolved = list.Count(o => o.Status == FeedbackStatus.Resolved),
                Closed = list.Count(o => o.Status == FeedbackStatus.Closed),
                Critical = list.Count(o => o.Severity == FeedbackSeverity.Critical),
                High = list.Count(o => o.Severity == FeedbackSeverity.High),
                Medium = list.Count(o => o.Severity == FeedbackSeverity.Medium),
                Low = list.Count(o => o.Severity == FeedbackSeverity.Low)
            };

            return Ok(stats);
        }
    }
}
