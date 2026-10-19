using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    [Route("api/disaggregationgroups")]
    public class DisaggregationGroupsController : BaseApiController
    {
        public DisaggregationGroupsController(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            UserManager<User> userManager,
            AppSettings appSettings)
            : base(dbFactory, userManager, appSettings)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetGroups([FromQuery] bool includeInactive = false)
        {
            var query = db.DisaggregationGroups
                .Include(g => g.Options)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(g => g.IsActive);
            }

            var groups = await query
                .OrderBy(g => g.Name)
                .ToListAsync();

            var dtos = groups.Select(g => ModelFactory.Create(g)).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroup(Guid id)
        {
            var group = await db.DisaggregationGroups
                .Include(g => g.Options)
                .FirstOrDefaultAsync(g => g.DisaggregationGroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            return Ok(ModelFactory.Create(group));
        }

        [HttpPost]
        [AuthorizeRoles(Roles.Administrator, Roles.Manager)]
        public async Task<IActionResult> CreateGroup([FromBody] DisaggregationGroupDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var group = new DisaggregationGroup
            {
                DisaggregationGroupId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            ModelFactory.Hydrate(group, dto);

            if (dto.Options != null)
            {
                int order = 1;
                foreach (var optDto in dto.Options)
                {
                    group.Options.Add(new DisaggregationOption
                    {
                        DisaggregationOptionId = Guid.NewGuid(),
                        DisaggregationGroupId = group.DisaggregationGroupId,
                        Name = optDto.Name,
                        Code = optDto.Code,
                        SortOrder = optDto.SortOrder > 0 ? optDto.SortOrder : order++
                    });
                }
            }

            db.DisaggregationGroups.Add(group);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGroup), new { id = group.DisaggregationGroupId }, ModelFactory.Create(group));
        }

        [HttpPut("{id}")]
        [AuthorizeRoles(Roles.Administrator, Roles.Manager)]
        public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] DisaggregationGroupDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var group = await db.DisaggregationGroups
                .Include(g => g.Options)
                .FirstOrDefaultAsync(g => g.DisaggregationGroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            ModelFactory.Hydrate(group, dto);

            // Sync Options: Add new or update existing
            if (dto.Options != null)
            {
                var existingOptionIds = group.Options.Select(o => o.DisaggregationOptionId).ToHashSet();
                var incomingOptionIds = dto.Options.Where(o => o.DisaggregationOptionId != Guid.Empty).Select(o => o.DisaggregationOptionId).ToHashSet();

                // Remove options not present in incoming list (if not used in data)
                var toRemove = group.Options.Where(o => !incomingOptionIds.Contains(o.DisaggregationOptionId)).ToList();
                foreach (var opt in toRemove)
                {
                    bool isUsed = await db.DisaggregationData.AnyAsync(d => d.DisaggregationOptionId == opt.DisaggregationOptionId);
                    if (!isUsed)
                    {
                        group.Options.Remove(opt);
                        db.DisaggregationOptions.Remove(opt);
                    }
                }

                int order = 1;
                foreach (var optDto in dto.Options)
                {
                    if (optDto.DisaggregationOptionId != Guid.Empty && existingOptionIds.Contains(optDto.DisaggregationOptionId))
                    {
                        var opt = group.Options.First(o => o.DisaggregationOptionId == optDto.DisaggregationOptionId);
                        ModelFactory.Hydrate(opt, optDto);
                    }
                    else
                    {
                        group.Options.Add(new DisaggregationOption
                        {
                            DisaggregationOptionId = Guid.NewGuid(),
                            DisaggregationGroupId = group.DisaggregationGroupId,
                            Name = optDto.Name,
                            Code = optDto.Code,
                            SortOrder = optDto.SortOrder > 0 ? optDto.SortOrder : order++
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
            return Ok(ModelFactory.Create(group));
        }

        [HttpDelete("{id}")]
        [AuthorizeRoles(Roles.Administrator, Roles.Manager)]
        public async Task<IActionResult> DeleteGroup(Guid id)
        {
            var group = await db.DisaggregationGroups
                .Include(g => g.Indicators)
                .FirstOrDefaultAsync(g => g.DisaggregationGroupId == id);

            if (group == null)
            {
                return NotFound();
            }

            if (group.Indicators.Count > 0)
            {
                // Soft delete by deactivating if currently linked to indicators
                group.IsActive = false;
                await db.SaveChangesAsync();
                return Ok(new { message = "Group is assigned to indicators; marked as inactive instead of deleted." });
            }

            db.DisaggregationGroups.Remove(group);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
