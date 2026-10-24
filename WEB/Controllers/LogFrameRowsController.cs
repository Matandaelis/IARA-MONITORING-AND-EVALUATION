using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class LogFrameRowsController : BaseApiController
    {
        public LogFrameRowsController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        [HttpGet, AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Search([FromQuery] LogFrameRowSearchOptions searchOptions)
        {
            IQueryable<LogFrameRow> results = db.LogFrameRows;

            if (searchOptions.IncludeParents)
            {
                results = results.Include(o => o.LogFrame);
            }

            if (searchOptions.IncludeChildren)
            {
                results = results.Include(o => o.LogFrameRowComponents)
                    .ThenInclude(o => o.Component);
                results = results.Include(o => o.LogFrameRowIndicators)
                    .ThenInclude(o => o.Indicator);
                results = results.Include(o => o.LogFrameRowRiskAssumptions)
                    .ThenInclude(o => o.RiskAssumption);
                results = results.Include(o => o.LogFrameRowTasks)
                    .ThenInclude(o => o.Task);
                results = results.Include(o => o.LogFrameRowBudgetLines)
                    .ThenInclude(o => o.BudgetLine);
            }

            if (!string.IsNullOrWhiteSpace(searchOptions.q))
                results = results.Where(o => o.Description.Contains(searchOptions.q) || o.Indicators.Contains(searchOptions.q) || o.MeansOfVerification.Contains(searchOptions.q) || o.RisksAndAssumptions.Contains(searchOptions.q));

            if (searchOptions.LogFrameId.HasValue) results = results.Where(o => o.LogFrameId == searchOptions.LogFrameId);
            if (searchOptions.RowType.HasValue) results = results.Where(o => o.RowType == searchOptions.RowType);

            results = results.OrderBy(o => o.RowNumber);

            var page = await GetPaginatedResponse(results, searchOptions);
            var dtos = page.Select(o => ModelFactory.Create(o, searchOptions.IncludeParents, searchOptions.IncludeChildren)).ToList();

            if (searchOptions.IncludeChildren)
            {
                for (int i = 0; i < page.Count; i++)
                {
                    await PopulateLiveMetricsAndMovAsync(dtos[i], page[i]);
                }
            }

            return Ok(dtos);
        }

        [HttpGet("{logFrameRowId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Get(Guid logFrameRowId)
        {
            var logFrameRow = await db.LogFrameRows
                .Include(o => o.LogFrame)
                .Include(o => o.LogFrameRowComponents).ThenInclude(o => o.Component)
                .Include(o => o.LogFrameRowIndicators).ThenInclude(o => o.Indicator)
                .Include(o => o.LogFrameRowRiskAssumptions).ThenInclude(o => o.RiskAssumption)
                .Include(o => o.LogFrameRowTasks).ThenInclude(o => o.Task)
                .Include(o => o.LogFrameRowBudgetLines).ThenInclude(o => o.BudgetLine)
                .FirstOrDefaultAsync(o => o.LogFrameRowId == logFrameRowId);

            if (logFrameRow == null)
                return NotFound();

            var dto = ModelFactory.Create(logFrameRow, includeParents: true, includeChildren: true);
            await PopulateLiveMetricsAndMovAsync(dto, logFrameRow);

            return Ok(dto);
        }

        [HttpPost("{logFrameRowId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Save(Guid logFrameRowId, [FromBody] LogFrameRowDTO logFrameRowDTO, [FromQuery] bool saveChildren = false)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (logFrameRowDTO.LogFrameRowId != logFrameRowId) return BadRequest("Id mismatch");

            var isNew = logFrameRowDTO.LogFrameRowId == Guid.Empty;

            LogFrameRow logFrameRow;
            if (isNew)
            {
                logFrameRow = new LogFrameRow();
                logFrameRowDTO.RowNumber = (await db.LogFrameRows.Where(o => o.LogFrameId == logFrameRowDTO.LogFrameId).MaxAsync(o => (int?)o.RowNumber) ?? 0) + 1;
                db.Entry(logFrameRow).State = EntityState.Added;
            }
            else
            {
                logFrameRow = await db.LogFrameRows
                    .Include(o => o.LogFrameRowIndicators)
                    .Include(o => o.LogFrameRowComponents)
                    .Include(o => o.LogFrameRowRiskAssumptions)
                    .Include(o => o.LogFrameRowTasks)
                    .Include(o => o.LogFrameRowBudgetLines)
                    .FirstOrDefaultAsync(o => o.LogFrameRowId == logFrameRowDTO.LogFrameRowId);

                if (logFrameRow == null)
                    return NotFound();

                db.Entry(logFrameRow).State = EntityState.Modified;
            }

            ModelFactory.Hydrate(logFrameRow, logFrameRowDTO);

            if (saveChildren)
            {
                // save indicators
                foreach (var lfriDTO in logFrameRowDTO.LogFrameRowIndicators)
                {
                    if (!logFrameRow.LogFrameRowIndicators.Any(o => o.IndicatorId == lfriDTO.IndicatorId))
                    {
                        var logFrameRowIndicator = new LogFrameRowIndicator();
                        logFrameRowIndicator.LogFrameRowId = logFrameRow.LogFrameRowId;
                        logFrameRowIndicator.IndicatorId = lfriDTO.IndicatorId;
                        ModelFactory.Hydrate(logFrameRowIndicator, lfriDTO);
                        db.Entry(logFrameRowIndicator).State = EntityState.Added;
                    }
                }
                foreach (var lfri in logFrameRow.LogFrameRowIndicators.ToList())
                {
                    if (!logFrameRowDTO.LogFrameRowIndicators.Any(lfriDTO => lfriDTO.IndicatorId == lfri.IndicatorId))
                        db.Entry(lfri).State = EntityState.Deleted;
                }

                // save components
                foreach (var lfrcDTO in logFrameRowDTO.LogFrameRowComponents)
                {
                    if (!logFrameRow.LogFrameRowComponents.Any(o => o.ComponentId == lfrcDTO.ComponentId))
                    {
                        var logFrameRowComponent = new LogFrameRowComponent();
                        logFrameRowComponent.LogFrameRowId = logFrameRow.LogFrameRowId;
                        logFrameRowComponent.ComponentId = lfrcDTO.ComponentId;
                        ModelFactory.Hydrate(logFrameRowComponent, lfrcDTO);
                        db.Entry(logFrameRowComponent).State = EntityState.Added;
                    }
                }
                foreach (var lfrc in logFrameRow.LogFrameRowComponents.ToList())
                {
                    if (!logFrameRowDTO.LogFrameRowComponents.Any(lfrcDTO => lfrcDTO.ComponentId == lfrc.ComponentId))
                        db.Entry(lfrc).State = EntityState.Deleted;
                }

                // save risk assumptions
                foreach (var riskDTO in logFrameRowDTO.LogFrameRowRiskAssumptions)
                {
                    if (!logFrameRow.LogFrameRowRiskAssumptions.Any(o => o.RiskAssumptionId == riskDTO.RiskAssumptionId))
                    {
                        var j = new LogFrameRowRiskAssumption
                        {
                            LogFrameRowId = logFrameRow.LogFrameRowId,
                            RiskAssumptionId = riskDTO.RiskAssumptionId
                        };
                        db.LogFrameRowRiskAssumptions.Add(j);
                    }
                }
                foreach (var r in logFrameRow.LogFrameRowRiskAssumptions.ToList())
                {
                    if (!logFrameRowDTO.LogFrameRowRiskAssumptions.Any(dto => dto.RiskAssumptionId == r.RiskAssumptionId))
                        db.Entry(r).State = EntityState.Deleted;
                }

                // save tasks
                foreach (var taskDTO in logFrameRowDTO.LogFrameRowTasks)
                {
                    if (!logFrameRow.LogFrameRowTasks.Any(o => o.TaskId == taskDTO.TaskId))
                    {
                        var j = new LogFrameRowTask
                        {
                            LogFrameRowId = logFrameRow.LogFrameRowId,
                            TaskId = taskDTO.TaskId
                        };
                        db.LogFrameRowTasks.Add(j);
                    }
                }
                foreach (var t in logFrameRow.LogFrameRowTasks.ToList())
                {
                    if (!logFrameRowDTO.LogFrameRowTasks.Any(dto => dto.TaskId == t.TaskId))
                        db.Entry(t).State = EntityState.Deleted;
                }

                // save budget lines
                foreach (var budgetDTO in logFrameRowDTO.LogFrameRowBudgetLines)
                {
                    if (!logFrameRow.LogFrameRowBudgetLines.Any(o => o.BudgetLineId == budgetDTO.BudgetLineId))
                    {
                        var j = new LogFrameRowBudgetLine
                        {
                            LogFrameRowId = logFrameRow.LogFrameRowId,
                            BudgetLineId = budgetDTO.BudgetLineId
                        };
                        db.LogFrameRowBudgetLines.Add(j);
                    }
                }
                foreach (var b in logFrameRow.LogFrameRowBudgetLines.ToList())
                {
                    if (!logFrameRowDTO.LogFrameRowBudgetLines.Any(dto => dto.BudgetLineId == b.BudgetLineId))
                        db.Entry(b).State = EntityState.Deleted;
                }
            }

            await db.SaveChangesAsync();

            return await Get(logFrameRow.LogFrameRowId);
        }

        [HttpDelete("{logFrameRowId:Guid}"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Delete(Guid logFrameRowId)
        {
            var logFrameRow = await db.LogFrameRows
                .FirstOrDefaultAsync(o => o.LogFrameRowId == logFrameRowId);

            if (logFrameRow == null)
                return NotFound();

            using (var transactionScope = Code.Db.CreateTransactionScope())
            {
                await db.LogFrameRowIndicators.Where(o => o.LogFrameRowId == logFrameRow.LogFrameRowId).ExecuteDeleteAsync();
                await db.LogFrameRowComponents.Where(o => o.LogFrameRowId == logFrameRow.LogFrameRowId).ExecuteDeleteAsync();
                await db.LogFrameRowRiskAssumptions.Where(o => o.LogFrameRowId == logFrameRow.LogFrameRowId).ExecuteDeleteAsync();
                await db.LogFrameRowTasks.Where(o => o.LogFrameRowId == logFrameRow.LogFrameRowId).ExecuteDeleteAsync();
                await db.LogFrameRowBudgetLines.Where(o => o.LogFrameRowId == logFrameRow.LogFrameRowId).ExecuteDeleteAsync();

                db.Entry(logFrameRow).State = EntityState.Deleted;

                await db.SaveChangesAsync();

                transactionScope.Complete();
            }

            return Ok();
        }

        [HttpPost("sort"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> Sort([FromQuery] Guid logFrameId, [FromBody] Guid[] sortedIds)
        {
            var logFrameRows = await db.LogFrameRows
                .Where(o => o.LogFrameId == logFrameId)
                .ToListAsync();

            if (logFrameRows.Count != sortedIds.Length) return BadRequest("Some of the logframe rows could not be found");

            foreach (var logFrameRow in logFrameRows)
            {
                db.Entry(logFrameRow).State = EntityState.Modified;
                logFrameRow.RowNumber = Array.IndexOf(sortedIds, logFrameRow.LogFrameRowId);
            }

            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("{logFrameRowId:Guid}/logframerowrisks"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> SaveLogFrameRowRisks(Guid logFrameRowId, [FromBody] Guid[] riskIds)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await db.LogFrameRowRiskAssumptions
                .Where(o => o.LogFrameRowId == logFrameRowId)
                .ToListAsync();

            foreach (var riskId in riskIds)
            {
                if (!existing.Any(o => o.RiskAssumptionId == riskId))
                {
                    db.LogFrameRowRiskAssumptions.Add(new LogFrameRowRiskAssumption
                    {
                        LogFrameRowId = logFrameRowId,
                        RiskAssumptionId = riskId
                    });
                }
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{logFrameRowId:Guid}/logframerowtasks"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> SaveLogFrameRowTasks(Guid logFrameRowId, [FromBody] Guid[] taskIds)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await db.LogFrameRowTasks
                .Where(o => o.LogFrameRowId == logFrameRowId)
                .ToListAsync();

            foreach (var taskId in taskIds)
            {
                if (!existing.Any(o => o.TaskId == taskId))
                {
                    db.LogFrameRowTasks.Add(new LogFrameRowTask
                    {
                        LogFrameRowId = logFrameRowId,
                        TaskId = taskId
                    });
                }
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{logFrameRowId:Guid}/logframerowbudgets"), AuthorizeRoles(Roles.Administrator)]
        public async Task<IActionResult> SaveLogFrameRowBudgets(Guid logFrameRowId, [FromBody] Guid[] budgetLineIds)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await db.LogFrameRowBudgetLines
                .Where(o => o.LogFrameRowId == logFrameRowId)
                .ToListAsync();

            foreach (var bId in budgetLineIds)
            {
                if (!existing.Any(o => o.BudgetLineId == bId))
                {
                    db.LogFrameRowBudgetLines.Add(new LogFrameRowBudgetLine
                    {
                        LogFrameRowId = logFrameRowId,
                        BudgetLineId = bId
                    });
                }
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        private async System.Threading.Tasks.Task PopulateLiveMetricsAndMovAsync(LogFrameRowDTO dto, LogFrameRow row)
        {
            if (row.LogFrameRowIndicators == null || row.LogFrameRowIndicators.Count == 0) return;

            var indicatorIds = row.LogFrameRowIndicators.Select(i => i.IndicatorId).ToList();

            var latestData = await db.Data
                .Where(d => indicatorIds.Contains(d.IndicatorId))
                .GroupBy(d => d.IndicatorId)
                .Select(g => g.OrderByDescending(d => d.LastSavedDateUtc).FirstOrDefault())
                .ToListAsync();

            var latestTargets = await db.IndicatorTargets
                .Where(t => indicatorIds.Contains(t.IndicatorId) && t.DisaggregationOptionId == null)
                .GroupBy(t => t.IndicatorId)
                .Select(g => g.OrderByDescending(t => t.UpdatedAt).FirstOrDefault())
                .ToListAsync();

            foreach (var lfri in row.LogFrameRowIndicators)
            {
                var ind = lfri.Indicator;
                if (ind == null) continue;

                var datum = latestData.FirstOrDefault(d => d != null && d.IndicatorId == ind.IndicatorId);
                var target = latestTargets.FirstOrDefault(t => t != null && t.IndicatorId == ind.IndicatorId);

                decimal? currentVal = datum?.Value;
                decimal? targetVal = target?.TargetValue;
                decimal? ach = null;
                string badgeColor = "secondary";

                if (targetVal.HasValue && targetVal.Value > 0m && currentVal.HasValue)
                {
                    ach = Math.Round(currentVal.Value / targetVal.Value * 100m, 1);
                    if (ach.Value >= 90m) badgeColor = "success";
                    else if (ach.Value >= 70m) badgeColor = "warning";
                    else badgeColor = "danger";
                }
                else if (currentVal.HasValue)
                {
                    badgeColor = "info";
                }

                dto.LiveIndicatorMetrics.Add(new LogFrameLiveIndicatorMetricDTO
                {
                    IndicatorId = ind.IndicatorId,
                    Code = ind.Code,
                    Name = ind.Name,
                    TargetValue = targetVal,
                    CurrentValue = currentVal,
                    PercentageAchievement = ach,
                    StatusBadgeColor = badgeColor
                });
            }

            // Auto-derive Means of Verification if empty
            if (string.IsNullOrWhiteSpace(dto.MeansOfVerification))
            {
                var movs = row.LogFrameRowIndicators
                    .Where(i => i.Indicator != null && !string.IsNullOrWhiteSpace(i.Indicator.DataQualityProtocol))
                    .Select(i => $"{i.Indicator.Code}: {i.Indicator.DataQualityProtocol}")
                    .ToList();

                if (movs.Count > 0)
                {
                    dto.MeansOfVerification = string.Join("; ", movs);
                }
                else if (row.LogFrameRowIndicators.Count > 0)
                {
                    dto.MeansOfVerification = string.Join("; ", row.LogFrameRowIndicators.Select(i => $"{i.Indicator?.Code ?? "Indicator"} Routine Monitoring Report"));
                }
            }
        }
    }
}
