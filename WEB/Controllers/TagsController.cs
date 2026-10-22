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
    public class TagsController : BaseApiController
    {
        public TagsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] TagSearchOptions searchOptions)
        {
            IQueryable<Tag> results = db.Tags;

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.CreatedBy);
            }

            if (searchOptions.IncludeChildren)
            {
                results = results.Include(o => o.DocumentTags);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.q))
                results = results.Where(o => o.Name.Contains(searchOptions.q));

            if (searchOptions.DocumentId.HasValue)
                results = results.Where(o => o.DocumentTags.Any(dt => dt.DocumentId == searchOptions.DocumentId.Value));

            results = results.OrderBy(o => o.Name);

            return Ok((await GetPaginatedResponse(results, searchOptions)).Select(o => ModelFactory.Create(o, searchOptions.IncludeParents)));
        }

        [HttpGet("{tagId:Guid}")]
        public async Task<IActionResult> Get(Guid tagId)
        {
            var tag = await db.Tags
                .Include(o => o.CreatedBy)
                .Include(o => o.DocumentTags)
                .FirstOrDefaultAsync(o => o.TagId == tagId);

            if (tag == null)
                return NotFound();

            return Ok(ModelFactory.Create(tag));
        }

        [HttpPost("{tagId:Guid}"), AuthorizeRoles(Roles.Manager)]
        public async Task<IActionResult> Save(Guid tagId, [FromBody] TagDTO tagDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (tagDTO.TagId != tagId) return BadRequest("Id mismatch");

            var isNew = tagDTO.TagId == Guid.Empty;

            Tag tag;
            if (isNew)
            {
                tag = new Tag
                {
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedById = CurrentUser.Id
                };

                db.Entry(tag).State = EntityState.Added;
            }
            else
            {
                tag = await db.Tags
                    .FirstOrDefaultAsync(o => o.TagId == tagDTO.TagId);

                if (tag == null)
                    return NotFound();

                db.Entry(tag).State = EntityState.Modified;
            }

            ModelFactory.Hydrate(tag, tagDTO);

            if (await db.Tags.AnyAsync(o => o.Name == tag.Name && o.TagId != tag.TagId))
                return BadRequest("A tag with this name already exists");

            await db.SaveChangesAsync();

            AuditHelper.Write(db, HttpContext, CurrentUser, "Tag", tag.TagId, isNew ? "Create" : "Update", null, ModelFactory.Create(tag, includeParents: false));
            await db.SaveChangesAsync();

            return await Get(tag.TagId);
        }

        [HttpDelete("{tagId:Guid}"), AuthorizeRoles(Roles.Manager)]
        public async Task<IActionResult> Delete(Guid tagId)
        {
            var tag = await db.Tags
                .FirstOrDefaultAsync(o => o.TagId == tagId);

            if (tag == null)
                return NotFound();

            if (await db.DocumentTags.AnyAsync(o => o.TagId == tagId))
                return BadRequest("Unable to delete the tag as it is used by one or more documents");

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                db.Entry(tag).State = EntityState.Deleted;

                await db.SaveChangesAsync();

                transactionScope.Complete();
            }

            AuditHelper.Write(db, HttpContext, CurrentUser, "Tag", tagId, "Delete", ModelFactory.Create(tag, includeParents: false), null);
            await db.SaveChangesAsync();

            return Ok();
        }

    }
}
