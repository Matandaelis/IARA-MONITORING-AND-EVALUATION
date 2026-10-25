using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    /// <summary>Field visits, supervision, audits, reviews and their follow-up actions.</summary>
    [Route("api/[Controller]")]
    public class MonitoringEventsController : BaseApiController
    {
        public MonitoringEventsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        private IQueryable<MonitoringEvent> EventsWithParents()
        {
            return db.MonitoringEvents
                .Include(o => o.Entity)
                .Include(o => o.ConductedBy)
                .Include(o => o.PeriodDate)
                .Include(o => o.Actions).ThenInclude(o => o.OwnerUser)
                .Include(o => o.Actions).ThenInclude(o => o.LinkedTask);
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] Guid projectId,
            [FromQuery] MonitoringEventType? type,
            [FromQuery] MonitoringEventStatus? status)
        {
            if (projectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var query = EventsWithParents().AsNoTracking().Where(o => o.ProjectId == projectId);
            if (type.HasValue) query = query.Where(o => o.Type == type.Value);
            if (status.HasValue) query = query.Where(o => o.Status == status.Value);

            var events = await query
                .OrderByDescending(o => o.EventDate)
                .ThenBy(o => o.Title)
                .ToListAsync();

            return Ok(events.Select(o => ModelFactory.Create(o, true)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var monitoringEvent = await EventsWithParents().AsNoTracking()
                .FirstOrDefaultAsync(o => o.MonitoringEventId == id);

            if (monitoringEvent == null) return NotFound();
            if (!await CanViewProjectAsync(monitoringEvent.ProjectId)) return Forbid();
            return Ok(ModelFactory.Create(monitoringEvent, true));
        }

        [HttpPost, AuthorizeRoles(Roles.Manager, Roles.Oversight, Roles.DataEntry)]
        public async Task<IActionResult> Create([FromBody] MonitoringEventDTO dto)
        {
            if (dto == null || dto.ProjectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            var monitoringEvent = new MonitoringEvent();
            ModelFactory.Hydrate(monitoringEvent, dto);
            monitoringEvent.CreatedById = CurrentUser.Id;
            monitoringEvent.CreatedOnUtc = DateTime.UtcNow;

            db.MonitoringEvents.Add(monitoringEvent);
            await db.SaveChangesAsync();

            var saved = await EventsWithParents().AsNoTracking()
                .FirstAsync(o => o.MonitoringEventId == monitoringEvent.MonitoringEventId);

            return Ok(ModelFactory.Create(saved, true));
        }

        [HttpPut("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight, Roles.DataEntry)]
        public async Task<IActionResult> Update(Guid id, [FromBody] MonitoringEventDTO dto)
        {
            var monitoringEvent = await db.MonitoringEvents.FirstOrDefaultAsync(o => o.MonitoringEventId == id);
            if (monitoringEvent == null) return NotFound();
            if (!await CanViewProjectAsync(monitoringEvent.ProjectId)) return Forbid();
            if (dto.ProjectId != Guid.Empty && !await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            ModelFactory.Hydrate(monitoringEvent, dto);
            await db.SaveChangesAsync();

            var saved = await EventsWithParents().AsNoTracking()
                .FirstAsync(o => o.MonitoringEventId == monitoringEvent.MonitoringEventId);

            return Ok(ModelFactory.Create(saved, true));
        }

        [HttpDelete("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var monitoringEvent = await db.MonitoringEvents.FirstOrDefaultAsync(o => o.MonitoringEventId == id);
            if (monitoringEvent == null) return NotFound();
            if (!await CanViewProjectAsync(monitoringEvent.ProjectId)) return Forbid();

            db.MonitoringEvents.Remove(monitoringEvent);
            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("actions"), AuthorizeRoles(Roles.Manager, Roles.Oversight, Roles.DataEntry)]
        public async Task<IActionResult> CreateAction([FromBody] MonitoringActionDTO dto)
        {
            if (dto == null || dto.ProjectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            var action = new MonitoringAction();
            ModelFactory.Hydrate(action, dto);
            action.CreatedById = CurrentUser.Id;
            action.CreatedOnUtc = DateTime.UtcNow;

            db.MonitoringActions.Add(action);
            await db.SaveChangesAsync();

            await db.Entry(action).Reference(o => o.OwnerUser).LoadAsync();
            await db.Entry(action).Reference(o => o.LinkedTask).LoadAsync();

            return Ok(ModelFactory.Create(action));
        }

        [HttpPut("actions/{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight, Roles.DataEntry)]
        public async Task<IActionResult> UpdateAction(Guid id, [FromBody] MonitoringActionDTO dto)
        {
            var action = await db.MonitoringActions.FirstOrDefaultAsync(o => o.MonitoringActionId == id);
            if (action == null) return NotFound();
            if (!await CanViewProjectAsync(action.ProjectId)) return Forbid();

            ModelFactory.Hydrate(action, dto);
            if (action.Status == MonitoringActionStatus.Completed && !action.CompletedDate.HasValue)
                action.CompletedDate = DateTime.UtcNow.Date;

            await db.SaveChangesAsync();

            await db.Entry(action).Reference(o => o.OwnerUser).LoadAsync();
            await db.Entry(action).Reference(o => o.LinkedTask).LoadAsync();

            return Ok(ModelFactory.Create(action));
        }

        [HttpDelete("actions/{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> DeleteAction(Guid id)
        {
            var action = await db.MonitoringActions.FirstOrDefaultAsync(o => o.MonitoringActionId == id);
            if (action == null) return NotFound();
            if (!await CanViewProjectAsync(action.ProjectId)) return Forbid();

            db.MonitoringActions.Remove(action);
            await db.SaveChangesAsync();
            return Ok();
        }
    }
}
