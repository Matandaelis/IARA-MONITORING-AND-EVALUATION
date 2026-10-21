using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code;

// Idempotent conversion of flat LogFrameRow data into the LogFrame v2 node/level hierarchy.
// One SaveChangesAsync per logframe at the end - transactional per logframe.
public sealed class LogFrameBackfillService
{
    private static readonly Dictionary<LogFrameRowType, int> RankByType = new()
    {
        { LogFrameRowType.Goals, 1 },
        { LogFrameRowType.Outcomes, 2 },
        { LogFrameRowType.Outputs, 3 },
        { LogFrameRowType.Activities, 4 },
    };

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public LogFrameBackfillService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<BackfillResult> BackfillAsync(Guid logFrameId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var logFrame = await db.LogFrames.AsNoTracking()
            .FirstOrDefaultAsync(l => l.LogFrameId == logFrameId, ct)
            ?? throw new InvalidOperationException($"LogFrame {logFrameId} not found.");

        // Level vocabulary created once per logframe, keyed by (LogFrameId, Rank).
        var existingLevels = await db.LogFrameLevels
            .Where(l => l.LogFrameId == logFrameId)
            .ToDictionaryAsync(l => l.Rank, ct);

        var levelsToCreate = new[] { (1, "Goals"), (2, "Outcomes"), (3, "Outputs"), (4, "Activities") }
            .Where(r => !existingLevels.ContainsKey(r.Item1))
            .Select(r => new LogFrameLevel
            {
                LogFrameLevelId = Guid.NewGuid(),
                LogFrameId = logFrameId,
                Name = r.Item2,
                PluralName = r.Item2,
                Rank = r.Item1,
                IsIndicatorLevel = true,
                IsActive = true
            }).ToList();

        foreach (var level in levelsToCreate)
            db.LogFrameLevels.Add(level);

        var levelsByRank = existingLevels
            .Concat(levelsToCreate.ToDictionary(l => l.Rank))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Rows already converted are skipped - running twice is a no-op for nodes.
        var alreadyConverted = await db.LogFrameNodes
            .Where(n => n.LogFrameId == logFrameId && n.LegacyLogFrameRowId != null)
            .Select(n => n.LegacyLogFrameRowId!.Value)
            .ToHashSetAsync(ct);

        var rows = await db.LogFrameRows
            .Where(r => r.LogFrameId == logFrameId)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(ct);

        // Latest node created per rank, updated as we process rows in RowNumber order.
        var lastByRank = new Dictionary<int, LogFrameNode>();
        var nodesCreated = 0;
        var rowsSkipped = 0;

        foreach (var row in rows)
        {
            if (alreadyConverted.Contains(row.LogFrameRowId))
            {
                rowsSkipped++;
                continue;
            }

            var rank = RankByType[row.RowType];

            // Parent = the most recently created node at the highest lower-numbered rank.
            LogFrameNode? parent = null;
            var lowerRanks = lastByRank.Keys.Where(k => k < rank).ToList();
            if (lowerRanks.Count > 0)
            {
                var parentRank = lowerRanks.Max();
                parent = lastByRank[parentRank];
            }

            var depth = parent is null ? 0 : parent.Depth + 1;
            var title = (row.Description ?? "").Trim();
            if (title.Length > 500)
                title = title.Substring(0, 500);

            var node = new LogFrameNode
            {
                LogFrameNodeId = Guid.NewGuid(),
                LogFrameId = logFrameId,
                ParentNodeId = parent?.LogFrameNodeId,
                LogFrameLevelId = levelsByRank[rank].LogFrameLevelId,
                Code = "", // never synthesize from text; keeps the filtered code index safe
                Title = title,
                Description = row.Description,
                MeansOfVerification = row.MeansOfVerification,
                RisksAndAssumptions = row.RisksAndAssumptions,
                SortOrder = row.RowNumber * 1000,
                Depth = depth,
                IsActive = true,
                CustomFieldsJson = "{}",
                LegacyLogFrameRowId = row.LogFrameRowId
            };

            db.LogFrameNodes.Add(node);
            lastByRank[rank] = node;
            nodesCreated++;
        }

        await db.SaveChangesAsync(ct);

        return new BackfillResult(
            logFrameId,
            LevelsCreated: levelsToCreate.Count,
            NodesCreated: nodesCreated,
            RowsProcessed: rows.Count,
            RowsSkipped: rowsSkipped);
    }

    public async Task<ReconciliationReport> ReconcileAsync(Guid logFrameId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var rows = await db.LogFrameRows.CountAsync(r => r.LogFrameId == logFrameId, ct);
        var nodes = await db.LogFrameNodes.CountAsync(n => n.LogFrameId == logFrameId, ct);

        var rowIds = await db.LogFrameRows
            .Where(r => r.LogFrameId == logFrameId)
            .Select(r => r.LogFrameRowId)
            .ToHashSetAsync(ct);

        var convertedNodeIds = await db.LogFrameNodes
            .Where(n => n.LogFrameId == logFrameId && n.LegacyLogFrameRowId != null)
            .Select(n => n.LegacyLogFrameRowId!.Value)
            .ToHashSetAsync(ct);

        var missingNodes = rowIds.Except(convertedNodeIds).Count();
        var orphanNodes = convertedNodeIds.Except(rowIds).Count();

        var duplicateCodes = await db.LogFrameNodes
            .Where(n => n.LogFrameId == logFrameId && n.Code != "")
            .GroupBy(n => n.Code)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .CountAsync(ct);

        var maxDepth = await db.LogFrameNodes
            .Where(n => n.LogFrameId == logFrameId)
            .MaxAsync(n => (int?)n.Depth, ct) ?? 0;

        return new ReconciliationReport(
            logFrameId,
            Rows: rows,
            Nodes: nodes,
            MissingNodes: missingNodes,
            OrphanNodes: orphanNodes,
            DuplicateCodes: duplicateCodes,
            MaxDepth: maxDepth);
    }
}

public sealed record BackfillResult(
    Guid LogFrameId,
    int LevelsCreated,
    int NodesCreated,
    int RowsProcessed,
    int RowsSkipped);

public sealed record ReconciliationReport(
    Guid LogFrameId,
    int Rows,
    int Nodes,
    int MissingNodes,
    int OrphanNodes,
    int DuplicateCodes,
    int MaxDepth);
