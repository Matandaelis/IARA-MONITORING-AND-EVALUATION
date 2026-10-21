using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;

namespace Monic.Web.Code;

// Read-only projection of the LogFrame v2 tree. Returns a flat, ordered node list;
// the client builds the tree from parentId/sortOrder (see the Angular store).
public sealed class LogFrameTreeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public LogFrameTreeService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<LogFrameTreeDto> LoadTreeAsync(Guid logFrameId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var logFrame = await db.LogFrames.AsNoTracking()
            .FirstOrDefaultAsync(l => l.LogFrameId == logFrameId, ct);

        if (logFrame is null)
            return null;

        var rows = await db.LogFrameNodes.AsNoTracking()
            .Where(n => n.LogFrameId == logFrameId && n.IsActive)
            .OrderBy(n => n.Depth).ThenBy(n => n.SortOrder)
            .Select(n => new
            {
                n.LogFrameNodeId,
                n.ParentNodeId,
                n.Code,
                n.Title,
                LevelName = n.Level.Name,
                n.Depth,
                n.SortOrder,
                RowVersion = n.RowVersion,
                IndicatorCount = n.Indicators.Count
            })
            .ToListAsync(ct);

        var nodes = rows.Select(r => new LogFrameNodeDto(
            r.LogFrameNodeId,
            r.ParentNodeId,
            r.Code,
            r.Title,
            r.LevelName,
            r.Depth,
            r.SortOrder,
            r.RowVersion is null ? "" : Convert.ToBase64String(r.RowVersion),
            r.IndicatorCount)).ToList();

        return new LogFrameTreeDto(logFrameId, logFrame.Name, nodes);
    }
}

public sealed record LogFrameNodeDto(
    Guid Id,
    Guid? ParentId,
    string Code,
    string Title,
    string Level,
    int Depth,
    int SortOrder,
    string RowVersion,
    int IndicatorCount);

public sealed record LogFrameTreeDto(
    Guid LogFrameId,
    string Name,
    IReadOnlyList<LogFrameNodeDto> Nodes);
