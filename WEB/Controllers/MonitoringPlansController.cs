using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    /// <summary>The project monitoring plan (matrix header) and its indicator rows.</summary>
    [Route("api/[Controller]")]
    public class MonitoringPlansController : BaseApiController
    {
        public MonitoringPlansController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        private IQueryable<MonitoringPlan> PlansWithParents()
        {
            return db.MonitoringPlans
                .Include(o => o.FromDate)
                .Include(o => o.ToDate)
                .Include(o => o.OwnerUser)
                .Include(o => o.Items).ThenInclude(o => o.Indicator)
                .Include(o => o.Items).ThenInclude(o => o.ResponsibleUser)
                .Include(o => o.Items).ThenInclude(o => o.ResponsibleEntity);
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] Guid projectId)
        {
            if (projectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var plans = await PlansWithParents()
                .AsNoTracking()
                .Where(o => o.ProjectId == projectId)
                .OrderByDescending(o => o.CreatedOnUtc)
                .ToListAsync();

            return Ok(plans.Select(o => ModelFactory.Create(o, true)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var plan = await PlansWithParents().AsNoTracking().FirstOrDefaultAsync(o => o.MonitoringPlanId == id);
            if (plan == null) return NotFound();
            if (!await CanViewProjectAsync(plan.ProjectId)) return Forbid();
            return Ok(ModelFactory.Create(plan, true));
        }

        [HttpPost, AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Create([FromBody] MonitoringPlanDTO dto)
        {
            if (dto == null || dto.ProjectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            var plan = new MonitoringPlan();
            ModelFactory.Hydrate(plan, dto);
            plan.CreatedById = CurrentUser.Id;
            plan.CreatedOnUtc = DateTime.UtcNow;

            db.MonitoringPlans.Add(plan);
            await db.SaveChangesAsync();

            var saved = await PlansWithParents().AsNoTracking()
                .FirstAsync(o => o.MonitoringPlanId == plan.MonitoringPlanId);

            return Ok(ModelFactory.Create(saved, true));
        }

        [HttpPut("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Update(Guid id, [FromBody] MonitoringPlanDTO dto)
        {
            var plan = await db.MonitoringPlans.FirstOrDefaultAsync(o => o.MonitoringPlanId == id);
            if (plan == null) return NotFound();
            if (!await CanViewProjectAsync(plan.ProjectId)) return Forbid();
            if (dto.ProjectId != Guid.Empty && !await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            ModelFactory.Hydrate(plan, dto);
            plan.ModifiedById = CurrentUser.Id;
            plan.ModifiedOnUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var saved = await PlansWithParents().AsNoTracking()
                .FirstAsync(o => o.MonitoringPlanId == plan.MonitoringPlanId);

            return Ok(ModelFactory.Create(saved, true));
        }

        [HttpDelete("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var plan = await db.MonitoringPlans.FirstOrDefaultAsync(o => o.MonitoringPlanId == id);
            if (plan == null) return NotFound();
            if (!await CanViewProjectAsync(plan.ProjectId)) return Forbid();

            db.MonitoringPlans.Remove(plan);
            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("items"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> CreateItem([FromBody] MonitoringPlanItemDTO dto)
        {
            if (dto == null || dto.MonitoringPlanId == Guid.Empty || dto.IndicatorId == Guid.Empty)
                return BadRequest("monitoringPlanId and indicatorId are required");

            var plan = await db.MonitoringPlans.FirstOrDefaultAsync(o => o.MonitoringPlanId == dto.MonitoringPlanId);
            if (plan == null) return BadRequest("Monitoring plan not found");
            if (!await CanViewProjectAsync(plan.ProjectId)) return Forbid();

            var duplicate = await db.MonitoringPlanItems
                .AnyAsync(o => o.MonitoringPlanId == dto.MonitoringPlanId && o.IndicatorId == dto.IndicatorId);
            if (duplicate) return BadRequest("Indicator is already in this plan");

            var item = new MonitoringPlanItem();
            ModelFactory.Hydrate(item, dto);

            db.MonitoringPlanItems.Add(item);
            await db.SaveChangesAsync();

            await db.Entry(item).Reference(o => o.Indicator).LoadAsync();
            await db.Entry(item).Reference(o => o.ResponsibleUser).LoadAsync();
            await db.Entry(item).Reference(o => o.ResponsibleEntity).LoadAsync();

            return Ok(ModelFactory.Create(item));
        }

        [HttpPut("items/{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> UpdateItem(Guid id, [FromBody] MonitoringPlanItemDTO dto)
        {
            var item = await db.MonitoringPlanItems
                .Include(o => o.MonitoringPlan)
                .FirstOrDefaultAsync(o => o.MonitoringPlanItemId == id);
            if (item == null) return NotFound();
            if (!await CanViewProjectAsync(item.MonitoringPlan.ProjectId)) return Forbid();

            if (dto.MonitoringPlanId != Guid.Empty && dto.MonitoringPlanId != item.MonitoringPlanId)
            {
                var targetPlan = await db.MonitoringPlans
                    .FirstOrDefaultAsync(o => o.MonitoringPlanId == dto.MonitoringPlanId);
                if (targetPlan == null) return BadRequest("Monitoring plan not found");
                if (!await CanViewProjectAsync(targetPlan.ProjectId)) return Forbid();
            }

            ModelFactory.Hydrate(item, dto);
            await db.SaveChangesAsync();

            await db.Entry(item).Reference(o => o.Indicator).LoadAsync();
            await db.Entry(item).Reference(o => o.ResponsibleUser).LoadAsync();
            await db.Entry(item).Reference(o => o.ResponsibleEntity).LoadAsync();

            return Ok(ModelFactory.Create(item));
        }

        [HttpDelete("items/{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            var item = await db.MonitoringPlanItems
                .Include(o => o.MonitoringPlan)
                .FirstOrDefaultAsync(o => o.MonitoringPlanItemId == id);
            if (item == null) return NotFound();
            if (!await CanViewProjectAsync(item.MonitoringPlan.ProjectId)) return Forbid();

            db.MonitoringPlanItems.Remove(item);
            await db.SaveChangesAsync();
            return Ok();
        }
    }
}
