using System;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests;

public class FormIndicatorLedgerModelTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void IndicatorContribution_Entity_HasExpectedProperties()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(IndicatorContribution));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("FormIndicatorMappingId"));
        Assert.NotNull(entity.FindProperty("ResponseId"));
        Assert.NotNull(entity.FindProperty("IndicatorId"));
        Assert.NotNull(entity.FindProperty("EntityId"));
        Assert.NotNull(entity.FindProperty("DateId"));
        Assert.NotNull(entity.FindProperty("DisaggregationOptionId"));
        Assert.NotNull(entity.FindProperty("Value"));
        Assert.NotNull(entity.FindProperty("Numerator"));
        Assert.NotNull(entity.FindProperty("Denominator"));
        Assert.NotNull(entity.FindProperty("ComputedAtUtc"));
    }

    [Fact]
    public void IndicatorContribution_RowVersion_IsConcurrencyToken()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(IndicatorContribution));
        Assert.NotNull(entity);

        var rowVersion = entity.FindProperty("RowVersion");
        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
    }

    [Fact]
    public void FormIndicatorMapping_Entity_HasExpectedProperties_And_QuestionLinks()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(FormIndicatorMapping));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("QuestionnaireId"));
        Assert.NotNull(entity.FindProperty("IndicatorId"));
        Assert.NotNull(entity.FindProperty("SectionId"));
        Assert.NotNull(entity.FindProperty("Aggregation"));
        Assert.NotNull(entity.FindProperty("ValueExpressionJson"));
        Assert.NotNull(entity.FindProperty("IsActive"));

        // QuestionLinks navigation exists on the mapping.
        var questionLinks = entity.FindNavigation("QuestionLinks");
        Assert.NotNull(questionLinks);
    }

    [Fact]
    public void QuestionIndicatorLink_Entity_HasExpectedProperties()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(QuestionIndicatorLink));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("FormIndicatorMappingId"));
        Assert.NotNull(entity.FindProperty("QuestionId"));
        Assert.NotNull(entity.FindProperty("Role"));
        Assert.NotNull(entity.FindProperty("DisaggregationOptionId"));
        Assert.NotNull(entity.FindProperty("ValueMapJson"));
        Assert.NotNull(entity.FindProperty("SortOrder"));
    }

    [Fact]
    public void QuestionIndicatorLink_Maps_To_FormIndicatorMapping_QuestionLinks()
    {
        using var db = CreateInMemoryDbContext();

        var linkEntity = db.Model.FindEntityType(typeof(QuestionIndicatorLink));
        Assert.NotNull(linkEntity);

        var fk = linkEntity.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType == db.Model.FindEntityType(typeof(FormIndicatorMapping))
                                 && f.Properties.Any(p => p.Name == "FormIndicatorMappingId"));

        Assert.NotNull(fk);
        var targetNav = fk.PrincipalToDependent;
        Assert.NotNull(targetNav);
        Assert.Equal("QuestionLinks", targetNav.Name);
    }

    [Fact]
    public void Enums_HaveExpectedMembers()
    {
        var aggregation = Enum.GetNames(typeof(MappingAggregation));
        Assert.Equal(new[] { "Sum", "Count", "CountDistinct", "Average", "Min", "Max", "MostRecent" }, aggregation);

        var role = Enum.GetNames(typeof(QuestionIndicatorRole));
        Assert.Equal(new[] { "Value", "Numerator", "Denominator", "Disaggregation", "Filter", "Date", "Entity" }, role);
    }

    [Fact]
    public async Task Save_Mapping_And_Contribution_Succeeds_UnderInMemory()
    {
        using var db = CreateInMemoryDbContext();

        var questionnaireId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();
        var responseId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var dateId = Guid.NewGuid();

        var mapping = new FormIndicatorMapping
        {
            QuestionnaireId = questionnaireId,
            IndicatorId = indicatorId,
            Aggregation = MappingAggregation.Sum,
            ValueExpressionJson = "{\"op\":\"sum\",\"args\":[]}",
            IsActive = true,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.FormIndicatorMappings.Add(mapping);

        db.IndicatorContributions.Add(new IndicatorContribution
        {
            FormIndicatorMappingId = mapping.FormIndicatorMappingId,
            ResponseId = responseId,
            IndicatorId = indicatorId,
            EntityId = entityId,
            DateId = dateId,
            Value = 12m,
            ComputedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var count = await db.IndicatorContributions.CountAsync();
        Assert.Equal(1, count);

        var saved = await db.FormIndicatorMappings
            .Include(m => m.QuestionLinks)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FormIndicatorMappingId == mapping.FormIndicatorMappingId);
        Assert.NotNull(saved);
        Assert.NotNull(saved.QuestionLinks);
    }
}
