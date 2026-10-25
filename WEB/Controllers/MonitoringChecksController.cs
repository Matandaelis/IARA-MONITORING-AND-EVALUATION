using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    /// <summary>Compliance and administrative / logistics checklists.</summary>
    [Route("api/[Controller]")]
    public class MonitoringChecksController : BaseApiController
    {
        public MonitoringChecksController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        private IQueryable<MonitoringCheck> ChecksWithParents()
        {
            return db.MonitoringChecks
                .Include(o => o.Entity)
                .Include(o => o.PeriodDate);
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] Guid projectId,
            [FromQuery] MonitoringCheckType? type,
            [FromQuery] MonitoringCheckStatus? status)
        {
            if (projectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var query = ChecksWithParents().AsNoTracking().Where(o => o.ProjectId == projectId);
            if (type.HasValue) query = query.Where(o => o.Type == type.Value);
            if (status.HasValue) query = query.Where(o => o.Status == status.Value);

            var checks = await query
                .OrderBy(o => o.Type)
                .ThenBy(o => o.Status)
                .ThenBy(o => o.Title)
                .ToListAsync();

            return Ok(checks.Select(o => ModelFactory.Create(o)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var check = await ChecksWithParents().AsNoTracking().FirstOrDefaultAsync(o => o.MonitoringCheckId == id);
            if (check == null) return NotFound();
            if (!await CanViewProjectAsync(check.ProjectId)) return Forbid();
            return Ok(ModelFactory.Create(check));
        }

        [HttpPost, AuthorizeRoles(Roles.Manager, Roles.Oversight, Roles.DataEntry)]
        public async Task<IActionResult> Create([FromBody] MonitoringCheckDTO dto)
        {
            if (dto == null || dto.ProjectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            var check = new MonitoringCheck();
            ModelFactory.Hydrate(check, dto);
            check.CreatedById = CurrentUser.Id;
            check.CreatedOnUtc = DateTime.UtcNow;

            db.MonitoringChecks.Add(check);
            await db.SaveChangesAsync();

            var saved = await ChecksWithParents().AsNoTracking()
                .FirstAsync(o => o.MonitoringCheckId == check.MonitoringCheckId);
            return Ok(ModelFactory.Create(saved));
        }

        [HttpPut("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight, Roles.DataEntry)]
        public async Task<IActionResult> Update(Guid id, [FromBody] MonitoringCheckDTO dto)
        {
            var check = await db.MonitoringChecks.FirstOrDefaultAsync(o => o.MonitoringCheckId == id);
            if (check == null) return NotFound();
            if (!await CanViewProjectAsync(check.ProjectId)) return Forbid();
            if (dto.ProjectId != Guid.Empty && !await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            ModelFactory.Hydrate(check, dto);
            await db.SaveChangesAsync();

            var saved = await ChecksWithParents().AsNoTracking()
                .FirstAsync(o => o.MonitoringCheckId == check.MonitoringCheckId);
            return Ok(ModelFactory.Create(saved));
        }

        [HttpDelete("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var check = await db.MonitoringChecks.FirstOrDefaultAsync(o => o.MonitoringCheckId == id);
            if (check == null) return NotFound();
            if (!await CanViewProjectAsync(check.ProjectId)) return Forbid();

            db.MonitoringChecks.Remove(check);
            await db.SaveChangesAsync();
            return Ok();
        }
    }
}
