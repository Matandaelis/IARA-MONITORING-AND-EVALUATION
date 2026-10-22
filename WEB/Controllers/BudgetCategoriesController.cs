using System;
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
    public class BudgetCategoriesController : BaseApiController
    {
        public BudgetCategoriesController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] BudgetCategorySearchOptions searchOptions)
        {
            IQueryable<BudgetCategory> results = db.BudgetCategories;

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.CreatedBy);
            }

            if (searchOptions.IncludeChildren)
            {
                results = results.Include(o => o.BudgetLines);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.q))
                results = results.Where(o => o.Name.Contains(searchOptions.q));

            results = results.OrderBy(o => o.SortOrder).ThenBy(o => o.Name);

            return Ok((await GetPaginatedResponse(results, searchOptions)).Select(o => ModelFactory.Create(o, searchOptions.IncludeParents)));
        }

        [HttpGet("{budgetCategoryId:Guid}")]
        public async Task<IActionResult> Get(Guid budgetCategoryId)
        {
            var budgetCategory = await db.BudgetCategories
                .Include(o => o.CreatedBy)
                .Include(o => o.BudgetLines)
                .FirstOrDefaultAsync(o => o.BudgetCategoryId == budgetCategoryId);

            if (budgetCategory == null)
                return NotFound();

            return Ok(ModelFactory.Create(budgetCategory));
        }

        [HttpPost("{budgetCategoryId:Guid}"), AuthorizeRoles(Roles.Manager, Roles.Finance)]
        public async Task<IActionResult> Save(Guid budgetCategoryId, [FromBody] BudgetCategoryDTO budgetCategoryDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (budgetCategoryDTO.BudgetCategoryId != budgetCategoryId) return BadRequest("Id mismatch");

            var isNew = budgetCategoryDTO.BudgetCategoryId == Guid.Empty;

            BudgetCategory budgetCategory;
            object beforeSnapshot = null;
            if (isNew)
            {
                budgetCategory = new BudgetCategory
                {
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedById = CurrentUser.Id
                };

                db.Entry(budgetCategory).State = EntityState.Added;
            }
            else
            {
                budgetCategory = await db.BudgetCategories
                    .FirstOrDefaultAsync(o => o.BudgetCategoryId == budgetCategoryDTO.BudgetCategoryId);

                if (budgetCategory == null)
                    return NotFound();

                beforeSnapshot = ModelFactory.Create(budgetCategory);

                db.Entry(budgetCategory).State = EntityState.Modified;
            }

            ModelFactory.Hydrate(budgetCategory, budgetCategoryDTO);

            if (await db.BudgetCategories.AnyAsync(o => o.Name == budgetCategory.Name && o.BudgetCategoryId != budgetCategory.BudgetCategoryId))
                return BadRequest("A budget category with this name already exists");

            await db.SaveChangesAsync();

            AuditHelper.Write(db, HttpContext, CurrentUser, "BudgetCategory", budgetCategory.BudgetCategoryId,
                isNew ? "Create" : "Update", beforeSnapshot, ModelFactory.Create(budgetCategory));
            await db.SaveChangesAsync();

            return await Get(budgetCategory.BudgetCategoryId);
        }

        [HttpDelete("{budgetCategoryId:Guid}"), AuthorizeRoles(Roles.Manager, Roles.Finance)]
        public async Task<IActionResult> Delete(Guid budgetCategoryId)
        {
            var budgetCategory = await db.BudgetCategories
                .FirstOrDefaultAsync(o => o.BudgetCategoryId == budgetCategoryId);

            if (budgetCategory == null)
                return NotFound();

            if (await db.BudgetLines.AnyAsync(o => o.BudgetCategoryId == budgetCategoryId && !o.IsDeleted))
                return BadRequest("Unable to delete the budget category as it is used by one or more budget lines");

            var beforeSnapshot = ModelFactory.Create(budgetCategory);

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                db.Entry(budgetCategory).State = EntityState.Deleted;

                await db.SaveChangesAsync();

                transactionScope.Complete();
            }

            AuditHelper.Write(db, HttpContext, CurrentUser, "BudgetCategory", budgetCategoryId, "Delete", beforeSnapshot, null);
            await db.SaveChangesAsync();

            return Ok();
        }

    }
}
