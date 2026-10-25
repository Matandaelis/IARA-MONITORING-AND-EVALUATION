using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    /// <summary>Assigns shared indicators to a project's monitoring scope.</summary>
    [Route("api/[Controller]")]
    public class ProjectIndicatorsController : BaseApiController
    {
        public ProjectIndicatorsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] Guid projectId)
        {
            if (projectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var items = await db.ProjectIndicators.AsNoTracking()
                .Where(o => o.ProjectId == projectId)
                .Include(o => o.Indicator)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Indicator.Name)
                .ToListAsync();

            return Ok(items.Select(o => ModelFactory.Create(o, true)));
        }

        [HttpPost, AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Add([FromBody] ProjectIndicatorDTO dto)
        {
            if (dto == null || dto.ProjectId == Guid.Empty || dto.IndicatorId == Guid.Empty)
                return BadRequest("projectId and indicatorId are required");
            if (!await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            if (!await db.Projects.AnyAsync(o => o.ProjectId == dto.ProjectId))
                return BadRequest("Project not found");

            if (!await db.Indicators.AnyAsync(o => o.IndicatorId == dto.IndicatorId))
                return BadRequest("Indicator not found");

            var existing = await db.ProjectIndicators
                .FirstOrDefaultAsync(o => o.ProjectId == dto.ProjectId && o.IndicatorId == dto.IndicatorId);

            if (existing != null) return Ok(ModelFactory.Create(existing, true));

            var entity = new ProjectIndicator
            {
                ProjectId = dto.ProjectId,
                IndicatorId = dto.IndicatorId,
                IsMandatory = dto.IsMandatory,
                SortOrder = dto.SortOrder,
                CreatedById = CurrentUser.Id
            };

            db.ProjectIndicators.Add(entity);
            await db.SaveChangesAsync();

            await db.Entry(entity).Reference(o => o.Indicator).LoadAsync();
            return Ok(ModelFactory.Create(entity, true));
        }

        [HttpDelete("{projectId}/{indicatorId}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Remove(Guid projectId, Guid indicatorId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var entity = await db.ProjectIndicators
                .FirstOrDefaultAsync(o => o.ProjectId == projectId && o.IndicatorId == indicatorId);

            if (entity == null) return NotFound();

            db.ProjectIndicators.Remove(entity);
            await db.SaveChangesAsync();
            return Ok();
        }
    }
}
