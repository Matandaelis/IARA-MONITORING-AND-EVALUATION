using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Code;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class BudgetLinesController : BaseApiController
    {
        public BudgetLinesController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        /// <summary>Budget figures are sensitive: users see projects of their permitted org units (or open projects);
        /// Administrator/Manager/Oversight see everything.</summary>
        private bool CanViewAllBudgets()
        {
            return CurrentUserIsInRole(Roles.Manager) || CurrentUserIsInRole(Roles.Oversight);
        }

        private async Task<List<Guid>> GetPermittedProjectIdsAsync()
        {
            if (CanViewAllBudgets()) return null;

            var permittedEntityIds = CurrentUser.GetPermittedEntityIds();

            return await db.Projects
                .Where(o => o.EntityId == null || permittedEntityIds.Contains(o.EntityId.Value))
                .Select(o => o.ProjectId)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] BudgetLineSearchOptions searchOptions)
        {
            IQueryable<BudgetLine> results = db.BudgetLines.Where(o => !o.IsDeleted);

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.Project);
                results = results.Include(o => o.BudgetCategory);
                results = results.Include(o => o.Date);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.q))
                results = results.Where(o => o.Description.Contains(searchOptions.q) || o.Project.Name.Contains(searchOptions.q));

            var permittedProjectIds = await GetPermittedProjectIdsAsync();
            if (permittedProjectIds != null)
                results = results.Where(o => permittedProjectIds.Contains(o.ProjectId));

            if (searchOptions.ProjectId.HasValue)
            {
                if (permittedProjectIds != null && !permittedProjectIds.Contains(searchOptions.ProjectId.Value))
                    return Forbid();

                results = results.Where(o => o.ProjectId == searchOptions.ProjectId.Value);
            }

            if (searchOptions.BudgetCategoryId.HasValue)
                results = results.Where(o => o.BudgetCategoryId == searchOptions.BudgetCategoryId.Value);

            results = results.OrderBy(o => o.PeriodStart).ThenBy(o => o.BudgetCategoryId);

            return Ok((await GetPaginatedResponse(results, searchOptions)).Select(o => ModelFactory.Create(o, searchOptions.IncludeParents)));
        }

        /// <summary>4-ledger per-category summary for a project, optionally converted to the organisation's base currency.</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] Guid projectId, [FromQuery] string currency = "base")
        {
            var permittedProjectIds = await GetPermittedProjectIdsAsync();
            if (permittedProjectIds != null && !permittedProjectIds.Contains(projectId))
                return Forbid();

            var baseCurrency = BudgetAnalytics.ResolveBaseCurrency(dbSettings);
            var inBaseCurrency = !string.Equals(currency, "original", StringComparison.OrdinalIgnoreCase);

            if (inBaseCurrency)
                return Ok(await BudgetAnalytics.ComputeSummaryAsync(db, projectId, baseCurrency, true));

            // original-currency mode: no conversion, no cross-currency totals
            var response = new BudgetSummaryResponseDTO { BaseCurrency = baseCurrency };
            var rows = await db.BudgetLines
                .Where(o => o.ProjectId == projectId && !o.IsDeleted)
                .GroupBy(o => new { o.BudgetCategoryId, CategoryName = o.BudgetCategory.Name })
                .Select(g => new BudgetSummaryRowDTO
                {
                    BudgetCategoryId = g.Key.BudgetCategoryId,
                    CategoryName = g.Key.CategoryName,
                    Planned = g.Sum(o => o.PlannedAmount),
                    Committed = g.Sum(o => o.CommittedAmount),
                    Disbursed = g.Sum(o => o.DisbursedAmount),
                    Actual = g.Sum(o => o.ActualAmount),
                    LineCount = g.Count(),
                    Currency = g.Select(o => o.Currency).FirstOrDefault()
                })
                .ToListAsync();

            response.Rows = rows.OrderBy(o => o.CategoryName).ToList();
            return Ok(response);
        }

        /// <summary>Planned vs expended per reporting period (S-curve / burn-up data), base currency.</summary>
        [HttpGet("burnseries")]
        public async Task<IActionResult> BurnSeries([FromQuery] Guid projectId)
        {
            var permittedProjectIds = await GetPermittedProjectIdsAsync();
            if (permittedProjectIds != null && !permittedProjectIds.Contains(projectId))
                return Forbid();

            var baseCurrency = BudgetAnalytics.ResolveBaseCurrency(dbSettings);
            return Ok(await BudgetAnalytics.ComputeBurnSeriesAsync(db, projectId, baseCurrency));
        }

        /// <summary>Cost-per-outcome: expended ÷ latest approved datum value for the indicator.</summary>
        [HttpGet("costperoutcome")]
        public async Task<IActionResult> CostPerOutcome([FromQuery] Guid projectId, [FromQuery] Guid indicatorId)
        {
            var permittedProjectIds = await GetPermittedProjectIdsAsync();
            if (permittedProjectIds != null && !permittedProjectIds.Contains(projectId))
                return Forbid();

            var baseCurrency = BudgetAnalytics.ResolveBaseCurrency(dbSettings);
            return Ok(await BudgetAnalytics.ComputeCostPerOutcomeAsync(db, projectId, indicatorId, baseCurrency));
        }

        [HttpGet("{budgetLineId:Guid}")]
        public async Task<IActionResult> Get(Guid budgetLineId)
        {
            var budgetLine = await db.BudgetLines
                .Include(o => o.Project)
                .Include(o => o.BudgetCategory)
                .Include(o => o.Date)
                .FirstOrDefaultAsync(o => o.BudgetLineId == budgetLineId && !o.IsDeleted);

            if (budgetLine == null)
                return NotFound();

            var permittedProjectIds = await GetPermittedProjectIdsAsync();
            if (permittedProjectIds != null && !permittedProjectIds.Contains(budgetLine.ProjectId))
                return Forbid();

            return Ok(ModelFactory.Create(budgetLine));
        }

        [HttpPost("{budgetLineId:Guid}"), AuthorizeRoles(Roles.Manager, Roles.Finance)]
        public async Task<IActionResult> Save(Guid budgetLineId, [FromBody] BudgetLineDTO budgetLineDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (budgetLineDTO.BudgetLineId != budgetLineId) return BadRequest("Id mismatch");

            if (budgetLineDTO.ProjectId == Guid.Empty) return BadRequest("ProjectId is required");
            if (budgetLineDTO.BudgetCategoryId == Guid.Empty) return BadRequest("BudgetCategoryId is required");

            if (!await db.Projects.AnyAsync(o => o.ProjectId == budgetLineDTO.ProjectId))
                return BadRequest("Project not found");

            if (!await db.BudgetCategories.AnyAsync(o => o.BudgetCategoryId == budgetLineDTO.BudgetCategoryId))
                return BadRequest("Budget category not found");

            var isNew = budgetLineDTO.BudgetLineId == Guid.Empty;

            BudgetLine budgetLine;
            object beforeSnapshot = null;
            if (isNew)
            {
                budgetLine = new BudgetLine
                {
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedById = CurrentUser.Id
                };

                db.Entry(budgetLine).State = EntityState.Added;
            }
            else
            {
                budgetLine = await db.BudgetLines
                    .Include(o => o.BudgetCategory)
                    .FirstOrDefaultAsync(o => o.BudgetLineId == budgetLineId && !o.IsDeleted);

                if (budgetLine == null)
                    return NotFound();

                beforeSnapshot = ModelFactory.Create(budgetLine, includeParents: false);

                budgetLine.ModifiedOnUtc = DateTime.UtcNow;
                budgetLine.ModifiedById = CurrentUser.Id;

                db.Entry(budgetLine).State = EntityState.Modified;
            }

            ModelFactory.Hydrate(budgetLine, budgetLineDTO);

            await db.SaveChangesAsync();

            AuditHelper.Write(db, HttpContext, CurrentUser, "BudgetLine", budgetLine.BudgetLineId,
                isNew ? "Create" : "Update", beforeSnapshot, ModelFactory.Create(budgetLine, includeParents: false));
            await db.SaveChangesAsync();

            return await Get(budgetLine.BudgetLineId);
        }

        /// <summary>Soft delete: budget lines are financial records and are never physically removed.</summary>
        [HttpDelete("{budgetLineId:Guid}"), AuthorizeRoles(Roles.Manager, Roles.Finance)]
        public async Task<IActionResult> Delete(Guid budgetLineId)
        {
            var budgetLine = await db.BudgetLines
                .Include(o => o.BudgetCategory)
                .FirstOrDefaultAsync(o => o.BudgetLineId == budgetLineId && !o.IsDeleted);

            if (budgetLine == null)
                return NotFound();

            var beforeSnapshot = ModelFactory.Create(budgetLine, includeParents: false);

            budgetLine.IsDeleted = true;
            budgetLine.ModifiedOnUtc = DateTime.UtcNow;
            budgetLine.ModifiedById = CurrentUser.Id;

            db.Entry(budgetLine).State = EntityState.Modified;

            await db.SaveChangesAsync();

            AuditHelper.Write(db, HttpContext, CurrentUser, "BudgetLine", budgetLine.BudgetLineId,
                "SoftDelete", beforeSnapshot, new { budgetLine.IsDeleted });
            await db.SaveChangesAsync();

            return Ok();
        }

    }
}
