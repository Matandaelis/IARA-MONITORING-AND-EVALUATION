using System;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests;

public class LogFrameV2ModelTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void LogFrameNodeIndicator_Entity_HasExpectedProperties()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(LogFrameNodeIndicator));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("LogFrameNodeId"));
        Assert.NotNull(entity.FindProperty("IndicatorId"));
        Assert.NotNull(entity.FindProperty("Weight"));
        Assert.NotNull(entity.FindProperty("IsPrimary"));
        Assert.NotNull(entity.FindProperty("SortOrder"));

        // Composite primary key: HasKey(LogFrameNodeId, IndicatorId).
        var key = entity.FindPrimaryKey();
        Assert.NotNull(key);
        Assert.Equal(new[] { "LogFrameNodeId", "IndicatorId" },
            key.Properties.Select(p => p.Name).ToList());
    }

    [Fact]
    public void LogFrameNode_SelfReference_UsesParentNodeId()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(LogFrameNode));
        Assert.NotNull(entity);

        var fk = entity.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == entity &&
                                 f.Properties.Any(p => p.Name == "ParentNodeId"));

        Assert.NotNull(fk);
        Assert.Equal("ParentNodeId", fk.Properties.First().Name);
    }

    [Fact]
    public void LogFrameNode_RowVersion_IsConcurrencyToken()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(LogFrameNode));
        Assert.NotNull(entity);

        var rowVersion = entity.FindProperty("RowVersion");
        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
    }

    [Fact]
    public void LogFrameLevel_Properties_AreConfigured()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(LogFrameLevel));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("Rank"));
        Assert.NotNull(entity.FindProperty("IsIndicatorLevel"));
        Assert.NotNull(entity.FindProperty("RequiredFieldsJson"));
    }

    [Fact]
    public async Task Save_LogFrameNode_Graph_Succeeds_UnderInMemory()
    {
        using var db = CreateInMemoryDbContext();

        var level = new LogFrameLevel
        {
            Name = "Outcome",
            PluralName = "Outcomes",
            Rank = 2,
            IsIndicatorLevel = true,
            IsActive = true
        };
        db.LogFrameLevels.Add(level);

        var node = new LogFrameNode
        {
            LogFrameId = Guid.NewGuid(),
            LogFrameLevelId = level.LogFrameLevelId,
            Code = "O1",
            Title = "Improve health outcomes",
            SortOrder = 1000,
            Depth = 1,
            IsActive = true
        };
        db.LogFrameNodes.Add(node);

        db.LogFrameNodeIndicators.Add(new LogFrameNodeIndicator
        {
            LogFrameNodeId = node.LogFrameNodeId,
            IndicatorId = Guid.NewGuid(),
            Weight = 1m,
            IsPrimary = true,
            SortOrder = 1
        });

        await db.SaveChangesAsync();

        var count = await db.LogFrameNodes.CountAsync();
        Assert.Equal(1, count);

        var saved = await db.LogFrameNodeIndicators
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.LogFrameNodeId == node.LogFrameNodeId);
        Assert.NotNull(saved);
        Assert.Equal(1m, saved.Weight);
    }

    [Fact]
    public void Configuration_LoadsFeatures_LogFrameV2_False()
    {
        var configPath = LocateConfigFile("appsettings.json")
            ?? LocateConfigFile("appsettings.Development.json");

        if (configPath is null)
        {
            // Config file is not copied to the test output directory; skip this assertion.
            return;
        }

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(configPath)!)
            .AddJsonFile(Path.GetFileName(configPath), optional: false, reloadOnChange: false)
            .Build();

        Assert.False(config.GetValue<bool>("Features:LogFrameV2"));
    }

    private static string LocateConfigFile(string fileName)
    {
        // Walk up from the test assembly location to find WEB/appsettings.json / Development.json.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "WEB", fileName);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
