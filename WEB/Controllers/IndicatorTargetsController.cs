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
    [Route("api/indicators/{indicatorId}/targets")]
    public class IndicatorTargetsController : BaseApiController
    {
        public IndicatorTargetsController(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            UserManager<User> userManager,
            AppSettings appSettings)
            : base(dbFactory, userManager, appSettings)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetTargets([FromRoute] Guid indicatorId, [FromQuery] Guid? entityId = null)
        {
            var query = db.IndicatorTargets
                .Include(t => t.Date)
                .Include(t => t.Entity)
                .Where(t => t.IndicatorId == indicatorId);

            if (entityId.HasValue)
            {
                query = query.Where(t => t.EntityId == entityId.Value || t.EntityId == null);
            }

            var targets = await query
                .OrderBy(t => t.Date.SortOrder)
                .ThenBy(t => t.Date.OpenFrom)
                .ToListAsync();

            var dtos = targets.Select(t => ModelFactory.Create(t)).ToList();
            return Ok(dtos);
        }

        [HttpPost]
        [AuthorizeRoles(Roles.Administrator, Roles.Manager)]
        public async Task<IActionResult> UpsertTarget([FromRoute] Guid indicatorId, [FromBody] IndicatorTargetDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var indicator = await db.Indicators.FindAsync(indicatorId);
            if (indicator == null)
            {
                return NotFound("Indicator not found");
            }

            var existing = await db.IndicatorTargets
                .FirstOrDefaultAsync(t => t.IndicatorId == indicatorId &&
                                          t.DateId == dto.DateId &&
                                          t.EntityId == dto.EntityId);

            if (existing != null)
            {
                ModelFactory.Hydrate(existing, dto);
                await db.SaveChangesAsync();
                return Ok(ModelFactory.Create(existing));
            }
            else
            {
                var newTarget = new IndicatorTarget
                {
                    IndicatorTargetId = Guid.NewGuid(),
                    IndicatorId = indicatorId
                };
                ModelFactory.Hydrate(newTarget, dto);
                db.IndicatorTargets.Add(newTarget);
                await db.SaveChangesAsync();

                // Reload Date navigation for DTO
                await db.Entry(newTarget).Reference(t => t.Date).LoadAsync();
                if (newTarget.EntityId.HasValue)
                {
                    await db.Entry(newTarget).Reference(t => t.Entity).LoadAsync();
                }

                return CreatedAtAction(nameof(GetTargets), new { indicatorId }, ModelFactory.Create(newTarget));
            }
        }

        [HttpDelete("{targetId}")]
        [AuthorizeRoles(Roles.Administrator, Roles.Manager)]
        public async Task<IActionResult> DeleteTarget([FromRoute] Guid indicatorId, [FromRoute] Guid targetId)
        {
            var target = await db.IndicatorTargets
                .FirstOrDefaultAsync(t => t.IndicatorTargetId == targetId && t.IndicatorId == indicatorId);

            if (target == null)
            {
                return NotFound();
            }

            db.IndicatorTargets.Remove(target);
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("~/api/indicators/{indicatorId}/variance")]
        public async Task<IActionResult> GetVariance([FromRoute] Guid indicatorId, [FromQuery] Guid? entityId = null)
        {
            var indicator = await db.Indicators
                .Include(i => i.DisaggregationGroup)
                .ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(i => i.IndicatorId == indicatorId);

            if (indicator == null)
            {
                return NotFound("Indicator not found");
            }

            // 1. Fetch scheduled targets
            var targetQuery = db.IndicatorTargets
                .Include(t => t.Date)
                .Where(t => t.IndicatorId == indicatorId);

            if (entityId.HasValue)
            {
                targetQuery = targetQuery.Where(t => t.EntityId == entityId.Value || t.EntityId == null);
            }

            var targets = await targetQuery.ToListAsync();

            // 2. Fetch actual reported datum observations
            var dataQuery = db.Data
                .Include(d => d.Date)
                .Include(d => d.DisaggregationData)
                .ThenInclude(dd => dd.DisaggregationOption)
                .Where(d => d.IndicatorId == indicatorId);

            if (entityId.HasValue)
            {
                dataQuery = dataQuery.Where(d => d.EntityId == entityId.Value);
            }

            var actualData = await dataQuery.ToListAsync();

            // 3. Union distinct periods between targets and actual data
            var dateIds = targets.Select(t => t.DateId)
                .Union(actualData.Select(d => d.DateId))
                .Distinct()
                .ToList();

            var dates = await db.Dates
                .Where(d => dateIds.Contains(d.DateId))
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.OpenFrom)
                .ToListAsync();

            var result = new IndicatorVarianceDTO
            {
                IndicatorId = indicator.IndicatorId,
                IndicatorName = indicator.Name,
                Direction = indicator.Direction,
                Periods = new List<PeriodVarianceDTO>()
            };

            foreach (var date in dates)
            {
                var target = targets.FirstOrDefault(t => t.DateId == date.DateId);
                var datum = actualData.FirstOrDefault(d => d.DateId == date.DateId);

                var variance = VarianceEngine.CalculateVariance(datum?.Value, target?.TargetValue, indicator.Direction);

                var periodDto = new PeriodVarianceDTO
                {
                    DateId = date.DateId,
                    PeriodName = date.Name,
                    TargetValue = target?.TargetValue,
                    BaselineValue = target?.BaselineValue,
                    ActualValue = datum?.Value,
                    PercentageAchievement = variance.PercentageAchievement,
                    StatusBadge = variance.StatusBadge
                };

                if (datum?.DisaggregationData != null && datum.DisaggregationData.Count > 0)
                {
                    foreach (var dd in datum.DisaggregationData)
                    {
                        periodDto.Disaggregations.Add(ModelFactory.Create(dd));
                    }
                }

                result.Periods.Add(periodDto);
            }

            return Ok(result);
        }
    }
}
