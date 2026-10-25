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
    /// <summary>
    /// Corrective Action Plan (CAP) management.
    /// CAPs are follow-up actions auto-triggered by the feedback-loop deviation watcher
    /// (variance beyond ±15%) or created manually to track corrective interventions.
    /// </summary>
    [Route("api/[Controller]")]
    public class CAPsController : BaseApiController
    {
        public CAPsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        /// <summary>Search CAPs for a project. Optional status/triggerSource filters.</summary>
        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] Guid projectId,
            [FromQuery] MonitoringActionStatus? status = null,
            [FromQuery] string triggerSource = null)
        {
            if (projectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var query = db.MonitoringActions
                .Include(o => o.Indicator)
                .Include(o => o.OwnerUser)
                .Where(o => o.ProjectId == projectId);

            if (status.HasValue) query = query.Where(o => o.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(triggerSource)) query = query.Where(o => o.TriggerSource == triggerSource);

            var caps = await query
                .OrderByDescending(o => o.TriggeredOnUtc ?? o.CreatedOnUtc)
                .ThenByDescending(o => o.CreatedOnUtc)
                .ToListAsync();

            return Ok(caps.Select(o => ModelFactory.Create(o)));
        }

        /// <summary>Get a single CAP by id.</summary>
        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var cap = await db.MonitoringActions
                .Include(o => o.Indicator)
                .Include(o => o.OwnerUser)
                .FirstOrDefaultAsync(o => o.MonitoringActionId == id);

            if (cap == null) return NotFound();
            if (!await CanViewProjectAsync(cap.ProjectId)) return Forbid();

            return Ok(ModelFactory.Create(cap));
        }

        /// <summary>
        /// Run the deviation watcher for a project: auto-create CAPs for indicators whose
        /// achievement deviates beyond the threshold (±15% default) and lack an open CAP.
        /// Returns the CAPs that were created.
        /// </summary>
        [HttpPost("trigger")]
        public async Task<IActionResult> Trigger([FromBody] CapTriggerRequest req)
        {
            if (req == null || req.ProjectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(req.ProjectId)) return Forbid();

            var caps = await CorrectiveActionService.DetectAndTriggerCapsAsync(
                db,
                req.ProjectId,
                req.DateId,
                req.DeviationThresholdPercent,
                req.SlaBusinessDays);

            return Ok(caps.Select(o => ModelFactory.Create(o)));
        }

        /// <summary>Update a CAP (status, owner, notes, due date, description).</summary>
        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MonitoringActionDTO dto)
        {
            var cap = await db.MonitoringActions.FirstOrDefaultAsync(o => o.MonitoringActionId == id);
            if (cap == null) return NotFound();
            if (!await CanViewProjectAsync(cap.ProjectId)) return Forbid();

            ModelFactory.Hydrate(cap, dto);
            await db.SaveChangesAsync();

            var saved = await db.MonitoringActions
                .Include(o => o.Indicator)
                .Include(o => o.OwnerUser)
                .FirstAsync(o => o.MonitoringActionId == cap.MonitoringActionId);

            return Ok(ModelFactory.Create(saved));
        }

        /// <summary>Delete a CAP.</summary>
        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var cap = await db.MonitoringActions.FirstOrDefaultAsync(o => o.MonitoringActionId == id);
            if (cap == null) return NotFound();
            if (!await CanViewProjectAsync(cap.ProjectId)) return Forbid();

            db.MonitoringActions.Remove(cap);
            await db.SaveChangesAsync();
            return Ok();
        }
    }

    public class CapTriggerRequest
    {
        public Guid ProjectId { get; set; }
        public Guid? DateId { get; set; }
        public decimal? DeviationThresholdPercent { get; set; }
        public int? SlaBusinessDays { get; set; }
    }
}
