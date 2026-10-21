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

public class LogFrameTreeServiceTests
{
    private ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var db = new ApplicationDbContext(options)
        {
            ChangeTracker = { AutoDetectChangesEnabled = true }
        };
        return db;
    }

    private static (Guid logFrameId, Guid levelId, Guid nodeId) Seed(LogFrameTreeServiceTests _) =>
        (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task LoadTree_ReturnsNull_ForUnknownLogFrame()
    {
        using var db = CreateDb();
        var service = new LogFrameTreeService(new DbContextFactoryWrapper(db));

        var result = await service.LoadTreeAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task LoadTree_OrdersByDepthThenSortOrder()
    {
        using var db = CreateDb();
        var service = new LogFrameTreeService(new DbContextFactoryWrapper(db));

        var logFrameId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });
        db.LogFrameLevels.Add(new LogFrameLevel
        {
            LogFrameLevelId = levelId, LogFrameId = logFrameId, Name = "Outcome",
            PluralName = "Outcomes", Rank = 2, IsIndicatorLevel = true, IsActive = true
        });

        // SortOrder 3000 should come after 1000 despite being added first.
        db.LogFrameNodes.Add(new LogFrameNode
        {
            LogFrameNodeId = Guid.NewGuid(), LogFrameId = logFrameId, LogFrameLevelId = levelId,
            Code = "B", Title = "Second", SortOrder = 3000, Depth = 1, IsActive = true
        });
        db.LogFrameNodes.Add(new LogFrameNode
        {
            LogFrameNodeId = Guid.NewGuid(), LogFrameId = logFrameId, LogFrameLevelId = levelId,
            Code = "A", Title = "First", SortOrder = 1000, Depth = 1, IsActive = true
        });

        await db.SaveChangesAsync();

        var tree = await service.LoadTreeAsync(logFrameId);

        Assert.NotNull(tree);
        Assert.Equal(2, tree.Nodes.Count);
        Assert.Equal("A", tree.Nodes[0].Code);
        Assert.Equal("B", tree.Nodes[1].Code);
    }

    [Fact]
    public async Task LoadTree_ProjectsParentAndLevelName()
    {
        using var db = CreateDb();
        var service = new LogFrameTreeService(new DbContextFactoryWrapper(db));

        var logFrameId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });
        db.LogFrameLevels.Add(new LogFrameLevel
        {
            LogFrameLevelId = levelId, LogFrameId = logFrameId, Name = "Output",
            PluralName = "Outputs", Rank = 3, IsIndicatorLevel = true, IsActive = true
        });

        db.LogFrameNodes.Add(new LogFrameNode
        {
            LogFrameNodeId = parentId, LogFrameId = logFrameId, LogFrameLevelId = levelId,
            Code = "P", Title = "Parent", SortOrder = 1000, Depth = 0, IsActive = true
        });
        db.LogFrameNodes.Add(new LogFrameNode
        {
            LogFrameNodeId = Guid.NewGuid(), LogFrameId = logFrameId, LogFrameLevelId = levelId,
            ParentNodeId = parentId, Code = "C", Title = "Child", SortOrder = 2000,
            Depth = 1, IsActive = true
        });

        await db.SaveChangesAsync();

        var tree = await service.LoadTreeAsync(logFrameId);

        Assert.NotNull(tree);
        var child = tree.Nodes.Single(n => n.Code == "C");
        Assert.Equal("Output", child.Level);
        Assert.Equal(parentId, child.ParentId);
        Assert.Equal(1, child.Depth);
    }

    [Fact]
    public async Task LoadTree_CountsIndicatorsPerNode()
    {
        using var db = CreateDb();
        var service = new LogFrameTreeService(new DbContextFactoryWrapper(db));

        var logFrameId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });
        db.LogFrameLevels.Add(new LogFrameLevel
        {
            LogFrameLevelId = levelId, LogFrameId = logFrameId, Name = "Outcome",
            PluralName = "Outcomes", Rank = 2, IsIndicatorLevel = true, IsActive = true
        });
        db.LogFrameNodes.Add(new LogFrameNode
        {
            LogFrameNodeId = nodeId, LogFrameId = logFrameId, LogFrameLevelId = levelId,
            Code = "N", Title = "Node", SortOrder = 1000, Depth = 1, IsActive = true
        });
        db.LogFrameNodeIndicators.Add(new LogFrameNodeIndicator
        {
            LogFrameNodeId = nodeId, IndicatorId = Guid.NewGuid(), Weight = 1m,
            IsPrimary = true, SortOrder = 1
        });
        db.LogFrameNodeIndicators.Add(new LogFrameNodeIndicator
        {
            LogFrameNodeId = nodeId, IndicatorId = Guid.NewGuid(), Weight = 1m,
            IsPrimary = false, SortOrder = 2
        });

        await db.SaveChangesAsync();

        var tree = await service.LoadTreeAsync(logFrameId);

        Assert.NotNull(tree);
        Assert.Equal(2, tree.Nodes.Single().IndicatorCount);
    }

    [Fact]
    public async Task LoadTree_ExcludesInactiveNodes()
    {
        using var db = CreateDb();
        var service = new LogFrameTreeService(new DbContextFactoryWrapper(db));

        var logFrameId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });
        db.LogFrameLevels.Add(new LogFrameLevel
        {
            LogFrameLevelId = levelId, LogFrameId = logFrameId, Name = "Outcome",
            PluralName = "Outcomes", Rank = 2, IsIndicatorLevel = true, IsActive = true
        });
        db.LogFrameNodes.Add(new LogFrameNode
        {
            LogFrameNodeId = Guid.NewGuid(), LogFrameId = logFrameId, LogFrameLevelId = levelId,
            Code = "VISIBLE", Title = "Visible", SortOrder = 1000, Depth = 1, IsActive = true
        });
        db.LogFrameNodes.Add(new LogFrameNode
        {
            LogFrameNodeId = Guid.NewGuid(), LogFrameId = logFrameId, LogFrameLevelId = levelId,
            Code = "HIDDEN", Title = "Hidden", SortOrder = 2000, Depth = 1, IsActive = false
        });

        await db.SaveChangesAsync();

        var tree = await service.LoadTreeAsync(logFrameId);

        Assert.NotNull(tree);
        Assert.Single(tree.Nodes);
        Assert.Equal("VISIBLE", tree.Nodes[0].Code);
    }

    [Fact]
    public async Task LoadTree_IncludesRootNodesWithoutParent()
    {
        using var db = CreateDb();
        var service = new LogFrameTreeService(new DbContextFactoryWrapper(db));

        var logFrameId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        db.LogFrames.Add(new LogFrame { LogFrameId = logFrameId, Name = "LF" });
        db.LogFrameLevels.Add(new LogFrameLevel
        {
            LogFrameLevelId = levelId, LogFrameId = logFrameId, Name = "Goal",
            PluralName = "Goals", Rank = 1, IsIndicatorLevel = true, IsActive = true
        });
        db.LogFrameNodes.Add(new LogFrameNode
        {
            LogFrameNodeId = Guid.NewGuid(), LogFrameId = logFrameId, LogFrameLevelId = levelId,
            Code = "ROOT", Title = "Root", SortOrder = 1000, Depth = 0, IsActive = true
        });

        await db.SaveChangesAsync();

        var tree = await service.LoadTreeAsync(logFrameId);

        Assert.NotNull(tree);
        Assert.Single(tree.Nodes);
        Assert.Null(tree.Nodes[0].ParentId);
        Assert.Equal(0, tree.Nodes[0].Depth);
        Assert.Equal("", tree.Nodes[0].RowVersion); // null RowVersion -> empty string
    }

    // Wraps an existing DbContext in an IDbContextFactory so the service can be unit-tested
    // without a real connection string.
    private sealed class DbContextFactoryWrapper : IDbContextFactory<ApplicationDbContext>
    {
        private readonly ApplicationDbContext _db;
        public DbContextFactoryWrapper(ApplicationDbContext db) => _db = db;
        public ApplicationDbContext CreateDbContext() => _db;
        public ApplicationDbContext CreateDbContext(string? name) => _db;
    }
}
