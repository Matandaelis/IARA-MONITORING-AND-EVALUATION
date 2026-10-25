using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    /// <summary>Structured risk and assumption register with periodic re-assessments.</summary>
    [Route("api/[Controller]")]
    public class RiskAssumptionsController : BaseApiController
    {
        public RiskAssumptionsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        private IQueryable<RiskAssumption> RisksWithParents()
        {
            return db.RiskAssumptions
                .Include(o => o.OwnerUser)
                .Include(o => o.LogFrameNode)
                .Include(o => o.Updates).ThenInclude(o => o.Date);
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] Guid projectId,
            [FromQuery] RiskType? type,
            [FromQuery] RiskStatus? status)
        {
            if (projectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var query = RisksWithParents().AsNoTracking().Where(o => o.ProjectId == projectId);
            if (type.HasValue) query = query.Where(o => o.Type == type.Value);
            if (status.HasValue) query = query.Where(o => o.Status == status.Value);

            var risks = await query
                .OrderBy(o => o.Type)
                .ThenByDescending(o => o.Likelihood * o.Impact)
                .ThenBy(o => o.Title)
                .ToListAsync();

            return Ok(risks.Select(o => ModelFactory.Create(o, true)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var risk = await RisksWithParents().AsNoTracking().FirstOrDefaultAsync(o => o.RiskAssumptionId == id);
            if (risk == null) return NotFound();
            if (!await CanViewProjectAsync(risk.ProjectId)) return Forbid();
            return Ok(ModelFactory.Create(risk, true));
        }

        [HttpPost, AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Create([FromBody] RiskAssumptionDTO dto)
        {
            if (dto == null || dto.ProjectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            var risk = new RiskAssumption();
            ModelFactory.Hydrate(risk, dto);
            risk.CreatedById = CurrentUser.Id;
            risk.CreatedOnUtc = DateTime.UtcNow;

            db.RiskAssumptions.Add(risk);
            await db.SaveChangesAsync();

            var saved = await RisksWithParents().AsNoTracking()
                .FirstAsync(o => o.RiskAssumptionId == risk.RiskAssumptionId);

            return Ok(ModelFactory.Create(saved, true));
        }

        [HttpPut("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Update(Guid id, [FromBody] RiskAssumptionDTO dto)
        {
            var risk = await db.RiskAssumptions.FirstOrDefaultAsync(o => o.RiskAssumptionId == id);
            if (risk == null) return NotFound();
            if (!await CanViewProjectAsync(risk.ProjectId)) return Forbid();
            if (dto.ProjectId != Guid.Empty && !await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            ModelFactory.Hydrate(risk, dto);
            await db.SaveChangesAsync();

            var saved = await RisksWithParents().AsNoTracking()
                .FirstAsync(o => o.RiskAssumptionId == risk.RiskAssumptionId);

            return Ok(ModelFactory.Create(saved, true));
        }

        [HttpDelete("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var risk = await db.RiskAssumptions.FirstOrDefaultAsync(o => o.RiskAssumptionId == id);
            if (risk == null) return NotFound();
            if (!await CanViewProjectAsync(risk.ProjectId)) return Forbid();

            db.RiskAssumptions.Remove(risk);
            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("updates"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> CreateUpdate([FromBody] RiskAssumptionUpdateDTO dto)
        {
            if (dto == null || dto.RiskAssumptionId == Guid.Empty) return BadRequest("riskAssumptionId is required");

            var risk = await db.RiskAssumptions.FirstOrDefaultAsync(o => o.RiskAssumptionId == dto.RiskAssumptionId);
            if (risk == null) return BadRequest("Risk/assumption not found");
            if (!await CanViewProjectAsync(risk.ProjectId)) return Forbid();

            var update = new RiskAssumptionUpdate();
            ModelFactory.Hydrate(update, dto);
            update.RecordedById = CurrentUser.Id;
            update.RecordedOnUtc = DateTime.UtcNow;

            db.RiskAssumptionUpdates.Add(update);

            // keep the register's current rating in sync with the latest assessment
            risk.Likelihood = dto.Likelihood;
            risk.Impact = dto.Impact;
            risk.Status = dto.Status;

            await db.SaveChangesAsync();

            await db.Entry(update).Reference(o => o.Date).LoadAsync();
            return Ok(ModelFactory.Create(update));
        }

        [HttpDelete("updates/{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> DeleteUpdate(Guid id)
        {
            var update = await db.RiskAssumptionUpdates
                .Include(o => o.RiskAssumption)
                .FirstOrDefaultAsync(o => o.RiskAssumptionUpdateId == id);
            if (update == null) return NotFound();
            if (!await CanViewProjectAsync(update.RiskAssumption.ProjectId)) return Forbid();

            db.RiskAssumptionUpdates.Remove(update);
            await db.SaveChangesAsync();
            return Ok();
        }
    }
}
