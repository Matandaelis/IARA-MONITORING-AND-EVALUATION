using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Services;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class DocumentsController : BaseApiController
    {
        private readonly DocumentsStorageService documentsStorageService;
        
        public DocumentsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings, DocumentsStorageService documentsStorageService)
            : base(dbFactory, um, appSettings)
        {
            this.documentsStorageService = documentsStorageService;
        }

        [HttpGet, AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Search([FromQuery] DocumentSearchOptions searchOptions)
        {
            IQueryable<Document> results = db.Documents;

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.Item);
                results = results.Include(o => o.UploadedBy);
            }

            if (searchOptions.IncludeChildren)
            {
                results = results.Include(o => o.DocumentTags).ThenInclude(o => o.Tag);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.q))
                results = results.Where(o => o.FileName.Contains(searchOptions.q));

            if (searchOptions.ItemId.HasValue) results = results.Where(o => o.ItemId == searchOptions.ItemId);

            if (searchOptions.TagId.HasValue)
                results = results.Where(o => o.DocumentTags.Any(dt => dt.TagId == searchOptions.TagId.Value));

            results = results.OrderBy(o => o.DocumentId);

            return Ok((await GetPaginatedResponse(results, searchOptions)).Select(o => ModelFactory.Create(o, searchOptions.IncludeParents, searchOptions.IncludeChildren)));
        }

        [HttpGet("{documentId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Get(Guid documentId)
        {
            var document = await db.Documents
                .Include(o => o.Item)
                .Include(o => o.UploadedBy)
                .Include(o => o.DocumentTags).ThenInclude(o => o.Tag)
                .FirstOrDefaultAsync(o => o.DocumentId == documentId);

            if (document == null)
                return NotFound();

            return Ok(ModelFactory.Create(document, includeParents: true, includeChildren: true));
        }

        [HttpPost("{documentId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Save(Guid documentId, [FromBody] DocumentDTO documentDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (documentDTO.DocumentId != documentId) return BadRequest("Id mismatch");

            var isNew = documentDTO.DocumentId == Guid.Empty;

            Document document;
            if (isNew)
            {
                document = new Document();

                document.UploadedOn = DateTime.UtcNow;
                document.UploadedById = CurrentUser.Id;

                db.Entry(document).State = EntityState.Added;
            }
            else
            {
                document = await db.Documents
                    .FirstOrDefaultAsync(o => o.DocumentId == documentDTO.DocumentId);

                if (document == null)
                    return NotFound();

                db.Entry(document).State = EntityState.Modified;
            }

            ModelFactory.Hydrate(document, documentDTO);

            await SyncDocumentTagsAsync(document, documentDTO);

            document.Size = document.DocumentContent.FileContents.Length;

            if (documentDTO.FileContents != null)
            {
                if (isNew)
                    db.Entry(document.DocumentContent).State = EntityState.Added;
                else
                {
                    if (await db.Documents.AnyAsync(o => o.DocumentId == documentDTO.DocumentId && o.DocumentContent != null))
                        db.Entry(document.DocumentContent).State = EntityState.Modified;
                    else
                        db.Entry(document.DocumentContent).State = EntityState.Added;
                }
            }

            await db.SaveChangesAsync();

            return await Get(document.DocumentId);
        }

        [HttpDelete("{documentId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Delete(Guid documentId)
        {
            var document = await db.Documents
                .FirstOrDefaultAsync(o => o.DocumentId == documentId);

            if (document == null)
                return NotFound();

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                await db.DocumentTags.Where(o => o.DocumentId == documentId).ExecuteDeleteAsync();

                await db.DocumentContents.Where(o => o.DocumentId == documentId).ExecuteDeleteAsync();

                db.Entry(document).State = EntityState.Deleted;

                await db.SaveChangesAsync();

                transactionScope.Complete();
            }

            return Ok();
        }

        /// <summary>Replaces the document's tag links with the tag set in the DTO.</summary>
        private async System.Threading.Tasks.Task SyncDocumentTagsAsync(Document document, DocumentDTO documentDTO)
        {
            if (documentDTO.Tags == null) return;

            var requestedTagIds = documentDTO.Tags
                .Where(o => o.TagId != Guid.Empty)
                .Select(o => o.TagId)
                .Distinct()
                .ToList();

            var existing = await db.DocumentTags
                .Where(o => o.DocumentId == document.DocumentId)
                .ToListAsync();

            foreach (var existingLink in existing.Where(o => !requestedTagIds.Contains(o.TagId)))
                db.Entry(existingLink).State = EntityState.Deleted;

            foreach (var tagId in requestedTagIds.Except(existing.Select(o => o.TagId)))
            {
                if (!await db.Tags.AnyAsync(o => o.TagId == tagId)) continue;

                var link = new DocumentTag
                {
                    DocumentId = document.DocumentId,
                    TagId = tagId
                };

                db.DocumentTags.Add(link);
            }
        }

    }
}
