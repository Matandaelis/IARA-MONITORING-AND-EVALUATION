using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests;

public class LogFrameBackfillServiceTests
{
    private (ApplicationDbContext Db, string DatabaseName) CreateDb()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var db = new ApplicationDbContext(options)
        {
            ChangeTracker = { AutoDetectChangesEnabled = true }
        };
        return (db, databaseName);
    }

    private LogFrameBackfillService Service(string databaseName) =>
        new LogFrameBackfillService(new DbContextFactoryWrapper(databaseName));

    // Gives the service its own context instance (which it disposes) while sharing the
    // same InMemory database as the test's context.
    private sealed class DbContextFactoryWrapper : IDbContextFactory<ApplicationDbContext>
    {
        private readonly string _databaseName;
        public DbContextFactoryWrapper(string databaseName) => _databaseName = databaseName;
        public ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;
            return new ApplicationDbContext(options)
            {
                ChangeTracker = { AutoDetectChangesEnabled = true }
            };
        }
        public ApplicationDbContext CreateDbContext(string? name) => CreateDbContext();
    }

    private Guid SeedRow(ApplicationDbContext db, Guid logFrameId, int rowNumber,
        LogFrameRowType rowType, string description)
    {
        var rowId = Guid.NewGuid();
        db.LogFrameRows.Add(new LogFrameRow
        {
            LogFrameRowId = rowId,
            LogFrameId = logFrameId,
            RowNumber = rowNumber,
            RowType = rowType,
            Description = description,
            Indicators = "",
            MeansOfVerification = "",
            RisksAndAssumptions = ""
        });
        return rowId;
    }

    [Fact]
    public async Task Backfill_CreatesFourLevels_WithExpectedRanks()
    {
        var (db, databaseName) = CreateDb();
        var logFrameId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });
        await db.SaveChangesAsync();

        await Service(databaseName).BackfillAsync(logFrameId);

        var levels = await db.LogFrameLevels.Where(l => l.LogFrameId == logFrameId)
            .OrderBy(l => l.Rank).ToListAsync();
        Assert.Equal(4, levels.Count);
        Assert.Equal(new[] { "Goals", "Outcomes", "Outputs", "Activities" },
            levels.Select(l => l.Name).ToList());
        Assert.Equal(new[] { 1, 2, 3, 4 }, levels.Select(l => l.Rank).ToList());
        Assert.All(levels, l =>
        {
            Assert.True(l.IsIndicatorLevel);
            Assert.True(l.IsActive);
            Assert.Equal(l.Name, l.PluralName);
        });
    }

    [Fact]
    public async Task Backfill_NestsRows_UnderNearestHigherRank()
    {
        var (db, databaseName) = CreateDb();
        var logFrameId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });

        // RowNumber order: Goals(1), Activities(2), Outputs(3), Activities(4)
        SeedRow(db, logFrameId, 1, LogFrameRowType.Goals, "G");
        SeedRow(db, logFrameId, 2, LogFrameRowType.Activities, "A1");
        SeedRow(db, logFrameId, 3, LogFrameRowType.Outputs, "O");
        SeedRow(db, logFrameId, 4, LogFrameRowType.Activities, "A2");
        await db.SaveChangesAsync();

        await Service(databaseName).BackfillAsync(logFrameId);

        var nodes = await db.LogFrameNodes
            .Where(n => n.LogFrameId == logFrameId)
            .OrderBy(n => n.SortOrder)
            .ToListAsync();
        Assert.Equal(4, nodes.Count);

        var g = nodes.Single(n => n.Description == "G");
        var a1 = nodes.Single(n => n.Description == "A1");
        var o = nodes.Single(n => n.Description == "O");
        var a2 = nodes.Single(n => n.Description == "A2");

        Assert.Null(g.ParentNodeId);
        Assert.Equal(g.LogFrameNodeId, a1.ParentNodeId);
        Assert.Equal(g.LogFrameNodeId, o.ParentNodeId);
        Assert.Equal(o.LogFrameNodeId, a2.ParentNodeId);
    }

    [Fact]
    public async Task Backfill_SetsDepthAndSortOrder()
    {
        var (db, databaseName) = CreateDb();
        var logFrameId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });

        SeedRow(db, logFrameId, 1, LogFrameRowType.Goals, "G");
        SeedRow(db, logFrameId, 2, LogFrameRowType.Activities, "A");
        await db.SaveChangesAsync();

        await Service(databaseName).BackfillAsync(logFrameId);

        var g = await db.LogFrameNodes.SingleAsync(n => n.Description == "G");
        var a = await db.LogFrameNodes.SingleAsync(n => n.Description == "A");

        Assert.Equal(0, g.Depth);
        Assert.Equal(1000, g.SortOrder);
        Assert.Equal(1, a.Depth);
        Assert.Equal(2000, a.SortOrder);
        Assert.Equal(g.LogFrameNodeId, a.ParentNodeId);
    }

    [Fact]
    public async Task Backfill_SetsLegacyLogFrameRowId_ForEveryRow()
    {
        var (db, databaseName) = CreateDb();
        var logFrameId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });

        var rowId1 = SeedRow(db, logFrameId, 1, LogFrameRowType.Goals, "G");
        var rowId2 = SeedRow(db, logFrameId, 2, LogFrameRowType.Outputs, "O");
        await db.SaveChangesAsync();

        await Service(databaseName).BackfillAsync(logFrameId);

        var legacyIds = await db.LogFrameNodes
            .Where(n => n.LogFrameId == logFrameId)
            .Select(n => n.LegacyLogFrameRowId)
            .ToListAsync();
        Assert.Equal(2, legacyIds.Count(id => id.HasValue));
        Assert.Contains(rowId1, legacyIds);
        Assert.Contains(rowId2, legacyIds);
    }

    [Fact]
    public async Task Backfill_IsIdempotent_OnSecondRun()
    {
        var (db, databaseName) = CreateDb();
        var logFrameId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });
        SeedRow(db, logFrameId, 1, LogFrameRowType.Goals, "G");
        await db.SaveChangesAsync();

        var first = await Service(databaseName).BackfillAsync(logFrameId);
        var second = await Service(databaseName).BackfillAsync(logFrameId);

        Assert.Equal(1, first.NodesCreated);
        Assert.Equal(0, second.NodesCreated);
        Assert.Equal(1, second.RowsSkipped);
        Assert.Equal(1, await db.LogFrameNodes.CountAsync(n => n.LogFrameId == logFrameId));
        Assert.Equal(4, await db.LogFrameLevels.CountAsync(l => l.LogFrameId == logFrameId));
    }

    [Fact]
    public async Task Backfill_LeavesEmptyLogFrame_Untouched()
    {
        var (db, databaseName) = CreateDb();
        var logFrameId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "Empty" });
        await db.SaveChangesAsync();

        var result = await Service(databaseName).BackfillAsync(logFrameId);

        Assert.Equal(0, result.NodesCreated);
        Assert.Equal(0, await db.LogFrameNodes.CountAsync(n => n.LogFrameId == logFrameId));
        Assert.Equal(4, await db.LogFrameLevels.CountAsync(l => l.LogFrameId == logFrameId)); // levels still created
    }

    [Fact]
    public async Task Backfill_TruncatesTitleTo500Chars()
    {
        var (db, databaseName) = CreateDb();
        var logFrameId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });
        var longDesc = new string('x', 1000);
        SeedRow(db, logFrameId, 1, LogFrameRowType.Goals, longDesc);
        await db.SaveChangesAsync();

        await Service(databaseName).BackfillAsync(logFrameId);

        var node = await db.LogFrameNodes.SingleAsync(n => n.LogFrameId == logFrameId);
        Assert.Equal(500, node.Title.Length);
        Assert.Equal(longDesc, node.Description); // full text kept in Description
    }

    [Fact]
    public async Task Reconcile_ReportsMissingAndOrphanNodes()
    {
        var (db, databaseName) = CreateDb();
        var logFrameId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });

        var orphanRowId = SeedRow(db, logFrameId, 1, LogFrameRowType.Goals, "G");
        await db.SaveChangesAsync();

        // Convert once so the row has a matching node.
        await Service(databaseName).BackfillAsync(logFrameId);

        // Delete the LogFrameRow -> it becomes an orphan node.
        db.LogFrameRows.Remove(await db.LogFrameRows.SingleAsync(r => r.LogFrameRowId == orphanRowId));
        await db.SaveChangesAsync();

        var report = await Service(databaseName).ReconcileAsync(logFrameId);

        Assert.Equal(0, report.Rows);
        Assert.Equal(1, report.Nodes);
        Assert.Equal(0, report.MissingNodes);
        Assert.Equal(1, report.OrphanNodes);
        Assert.Equal(0, report.DuplicateCodes);
        Assert.Equal(0, report.MaxDepth); // single root node has Depth 0
    }
}
