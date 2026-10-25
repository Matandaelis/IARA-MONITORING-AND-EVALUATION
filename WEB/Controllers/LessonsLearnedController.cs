using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    /// <summary>Lessons learned for adaptive management and organisational learning.</summary>
    [Route("api/[Controller]")]
    public class LessonsLearnedController : BaseApiController
    {
        public LessonsLearnedController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        private IQueryable<LessonLearned> LessonsWithParents()
        {
            return db.LessonsLearned
                .Include(o => o.Component)
                .Include(o => o.LogFrameNode);
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] Guid projectId, [FromQuery] MonitoringType? monitoringType)
        {
            if (projectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(projectId)) return Forbid();

            var query = LessonsWithParents().AsNoTracking().Where(o => o.ProjectId == projectId);
            if (monitoringType.HasValue) query = query.Where(o => o.MonitoringType == monitoringType.Value);

            var lessons = await query
                .OrderByDescending(o => o.RecordedOnUtc)
                .ToListAsync();

            return Ok(lessons.Select(o => ModelFactory.Create(o)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var lesson = await LessonsWithParents().AsNoTracking().FirstOrDefaultAsync(o => o.LessonLearnedId == id);
            if (lesson == null) return NotFound();
            if (!await CanViewProjectAsync(lesson.ProjectId)) return Forbid();
            return Ok(ModelFactory.Create(lesson));
        }

        [HttpPost, AuthorizeRoles(Roles.Manager, Roles.Oversight, Roles.DataEntry)]
        public async Task<IActionResult> Create([FromBody] LessonLearnedDTO dto)
        {
            if (dto == null || dto.ProjectId == Guid.Empty) return BadRequest("projectId is required");
            if (!await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            var lesson = new LessonLearned();
            ModelFactory.Hydrate(lesson, dto);
            lesson.RecordedById = CurrentUser.Id;
            lesson.RecordedOnUtc = DateTime.UtcNow;

            db.LessonsLearned.Add(lesson);
            await db.SaveChangesAsync();

            var saved = await LessonsWithParents().AsNoTracking()
                .FirstAsync(o => o.LessonLearnedId == lesson.LessonLearnedId);
            return Ok(ModelFactory.Create(saved));
        }

        [HttpPut("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight, Roles.DataEntry)]
        public async Task<IActionResult> Update(Guid id, [FromBody] LessonLearnedDTO dto)
        {
            var lesson = await db.LessonsLearned.FirstOrDefaultAsync(o => o.LessonLearnedId == id);
            if (lesson == null) return NotFound();
            if (!await CanViewProjectAsync(lesson.ProjectId)) return Forbid();
            if (dto.ProjectId != Guid.Empty && !await CanViewProjectAsync(dto.ProjectId)) return Forbid();

            ModelFactory.Hydrate(lesson, dto);
            await db.SaveChangesAsync();

            var saved = await LessonsWithParents().AsNoTracking()
                .FirstAsync(o => o.LessonLearnedId == lesson.LessonLearnedId);
            return Ok(ModelFactory.Create(saved));
        }

        [HttpDelete("{id}"), AuthorizeRoles(Roles.Manager, Roles.Oversight)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var lesson = await db.LessonsLearned.FirstOrDefaultAsync(o => o.LessonLearnedId == id);
            if (lesson == null) return NotFound();
            if (!await CanViewProjectAsync(lesson.ProjectId)) return Forbid();

            db.LessonsLearned.Remove(lesson);
            await db.SaveChangesAsync();
            return Ok();
        }
    }
}
