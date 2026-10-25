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
    /// Computed monitoring endpoints: dashboard summary, monitoring matrix, indicator performance,
    /// results-tree roll-up, financial monitoring and data coverage/timeliness.
    /// </summary>
    [Route("api/[Controller]")]
    public class MonitoringController : BaseApiController
    {
        public MonitoringController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        private async Task<List<Guid>> GetPermittedProjectIdsAsync()
        {
            if (CurrentUserIsInRole(Roles.Manager) || CurrentUserIsInRole(Roles.Oversight)) return null;

            var permittedEntityIds = CurrentUser.GetPermittedEntityIds();
            return await db.Projects.AsNoTracking()
                .Where(o => o.EntityId == null || permittedEntityIds.Contains(o.EntityId.Value))
                .Select(o => o.ProjectId)
                .ToListAsync();
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] Guid projectId, [FromQuery] Guid? dateId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();
            return Ok(await MonitoringAnalytics.ComputeSummaryAsync(db, projectId, dateId));
        }

        [HttpGet("matrix")]
        public async Task<IActionResult> Matrix([FromQuery] Guid projectId, [FromQuery] Guid? dateId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();
            return Ok(await MonitoringAnalytics.ComputeMatrixAsync(db, projectId, dateId));
        }

        [HttpGet("indicators")]
        public async Task<IActionResult> Indicators(
            [FromQuery] Guid projectId,
            [FromQuery] Guid? dateId,
            [FromQuery] MonitoringType? monitoringType)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var indicators = await MonitoringAnalytics.LoadProjectIndicatorsAsync(db, projectId);
            var dates = await db.Dates.AsNoTracking().ToListAsync();

            var rows = indicators
                .Select(o => MonitoringAnalytics.ComputePerformance(o, dateId, dates))
                .Where(o => o != null);

            if (monitoringType.HasValue)
                rows = rows.Where(o => o.MonitoringType == monitoringType.Value);

            return Ok(rows.OrderBy(o => o.MonitoringType).ThenBy(o => o.Code).ToList());
        }

        [HttpGet("results")]
        public async Task<IActionResult> Results([FromQuery] Guid logFrameId, [FromQuery] Guid? dateId)
        {
            var projectId = await db.LogFrames.AsNoTracking()
                .Where(o => o.LogFrameId == logFrameId)
                .Select(o => o.ProjectId)
                .FirstOrDefaultAsync();

            if (projectId.HasValue && !await CanViewProjectAsync(projectId.Value)) return Forbid();

            var result = await MonitoringAnalytics.ComputeResultsAsync(db, logFrameId, dateId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("financial")]
        public async Task<IActionResult> Financial([FromQuery] Guid projectId, [FromQuery] Guid? dateId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();
            return Ok(await MonitoringAnalytics.ComputeFinancialAsync(db, projectId, dateId));
        }

        [HttpGet("coverage")]
        public async Task<IActionResult> Coverage([FromQuery] Guid projectId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();
            return Ok(await MonitoringAnalytics.ComputeCoverageAsync(db, projectId));
        }

        /// <summary>Logframes available for the project results-tree picker.</summary>
        [HttpGet("logframes")]
        public async Task<IActionResult> LogFrames([FromQuery] Guid projectId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var logFrames = await db.LogFrames.AsNoTracking()
                .Where(o => o.ProjectId == projectId || o.ProjectId == null)
                .OrderBy(o => o.Name)
                .Select(o => new { o.LogFrameId, o.Name, o.ProjectId })
                .ToListAsync();

            return Ok(logFrames);
        }

        /// <summary>Active logframe results available for linking budget lines to results.</summary>
        [HttpGet("logframenodes")]
        public async Task<IActionResult> LogFrameNodes([FromQuery] Guid projectId)
        {
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var nodes = await db.LogFrameNodes.AsNoTracking()
                .Where(o => o.IsActive && (o.LogFrame.ProjectId == projectId || o.LogFrame.ProjectId == null))
                .OrderBy(o => o.LogFrame.Name)
                .ThenBy(o => o.SortOrder)
                .Select(o => new
                {
                    o.LogFrameNodeId,
                    o.LogFrameId,
                    LogFrameName = o.LogFrame.Name,
                    o.Code,
                    o.Title,
                    LevelName = o.Level.Name
                })
                .ToListAsync();

            return Ok(nodes);
        }

        /// <summary>Reporting periods available for the period filter.</summary>
        [HttpGet("periods")]
        public async Task<IActionResult> Periods([FromQuery] DateType? dateType)
        {
            var query = db.Dates.AsNoTracking().AsQueryable();
            if (dateType.HasValue) query = query.Where(o => o.DateType == dateType.Value);

            var periods = await query
                .OrderByDescending(o => o.SortOrder)
                .ThenByDescending(o => o.OpenFrom)
                .Take(36)
                .Select(o => new { o.DateId, o.Name, o.DateType, o.OpenFrom, o.OpenTo, o.SortOrder })
                .ToListAsync();

            return Ok(periods.OrderBy(o => o.SortOrder).ToList());
        }

        /// <summary>Projects visible to the current user (for the monitoring project picker).</summary>
        [HttpGet("projects")]
        public async Task<IActionResult> Projects()
        {
            var permitted = await GetPermittedProjectIdsAsync();
            var query = db.Projects.AsNoTracking().AsQueryable();
            if (permitted != null) query = query.Where(o => permitted.Contains(o.ProjectId));

            return Ok(await query.OrderBy(o => o.Name).Select(o => new { o.ProjectId, o.Name }).ToListAsync());
        }
    }
}
