using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests;

public class UniversalMeModelTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void BeneficiaryFeedback_Entity_HasConfiguredPropertiesAndRelationships()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(BeneficiaryFeedback));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("ProjectId"));
        Assert.NotNull(entity.FindProperty("TrackingCode"));
        Assert.NotNull(entity.FindProperty("DateReceivedUtc"));
        Assert.NotNull(entity.FindProperty("Channel"));
        Assert.NotNull(entity.FindProperty("Category"));
        Assert.NotNull(entity.FindProperty("Description"));
        Assert.NotNull(entity.FindProperty("Severity"));
        Assert.NotNull(entity.FindProperty("Status"));
        Assert.NotNull(entity.FindProperty("IsAnonymous"));
        Assert.NotNull(entity.FindProperty("ContactDetails"));
        Assert.NotNull(entity.FindProperty("ResolutionNotes"));
        Assert.NotNull(entity.FindProperty("ResolvedAtUtc"));
        Assert.NotNull(entity.FindProperty("AssignedToUserId"));
    }

    [Fact]
    public void LogFrameRowJunctions_ConfiguredWithKeysAndUniqueIndexes()
    {
        using var db = CreateInMemoryDbContext();

        var riskJunction = db.Model.FindEntityType(typeof(LogFrameRowRiskAssumption));
        Assert.NotNull(riskJunction);
        Assert.NotNull(riskJunction.FindPrimaryKey());
        var riskIndex = riskJunction.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == "LogFrameRowId") &&
            i.Properties.Any(p => p.Name == "RiskAssumptionId"));
        Assert.NotNull(riskIndex);
        Assert.True(riskIndex.IsUnique);

        var taskJunction = db.Model.FindEntityType(typeof(LogFrameRowTask));
        Assert.NotNull(taskJunction);
        Assert.NotNull(taskJunction.FindPrimaryKey());
        var taskIndex = taskJunction.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == "LogFrameRowId") &&
            i.Properties.Any(p => p.Name == "TaskId"));
        Assert.NotNull(taskIndex);
        Assert.True(taskIndex.IsUnique);

        var budgetJunction = db.Model.FindEntityType(typeof(LogFrameRowBudgetLine));
        Assert.NotNull(budgetJunction);
        Assert.NotNull(budgetJunction.FindPrimaryKey());
        var budgetIndex = budgetJunction.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == "LogFrameRowId") &&
            i.Properties.Any(p => p.Name == "BudgetLineId"));
        Assert.NotNull(budgetIndex);
        Assert.True(budgetIndex.IsUnique);
    }

    [Fact]
    public void IndicatorTarget_HasDisaggregationOptionRelationship()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(IndicatorTarget));
        Assert.NotNull(entity);

        var disaggProp = entity.FindProperty("DisaggregationOptionId");
        Assert.NotNull(disaggProp);

        var index = entity.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == "DisaggregationOptionId") &&
            i.Properties.Any(p => p.Name == "IndicatorId") &&
            i.Properties.Any(p => p.Name == "DateId") &&
            i.Properties.Any(p => p.Name == "EntityId"));
        Assert.NotNull(index);
    }

    [Fact]
    public void Indicator_HasPirsMetadataFields()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(Indicator));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("OperationalDefinition"));
        Assert.NotNull(entity.FindProperty("PurposeRationale"));
        Assert.NotNull(entity.FindProperty("DataQualityProtocol"));
        Assert.NotNull(entity.FindProperty("ReportingSchedule"));
        Assert.NotNull(entity.FindProperty("CalculationFormulaNarrative"));
        Assert.NotNull(entity.FindProperty("NumeratorIndicatorId"));
        Assert.NotNull(entity.FindProperty("DenominatorIndicatorId"));
    }

    [Fact]
    public void Project_HasUniversalSectorTaxonomyFields()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(Project));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("Sector"));
        Assert.NotNull(entity.FindProperty("StartDate"));
        Assert.NotNull(entity.FindProperty("EndDate"));
        Assert.NotNull(entity.FindProperty("CurrentPhase"));
        Assert.NotNull(entity.FindProperty("DonorName"));
        Assert.NotNull(entity.FindProperty("GrantCode"));
        Assert.NotNull(entity.FindProperty("TotalBudget"));
        Assert.NotNull(entity.FindProperty("Currency"));
        Assert.NotNull(entity.FindProperty("TargetDirectBeneficiaries"));
        Assert.NotNull(entity.FindProperty("TargetIndirectBeneficiaries"));
        Assert.NotNull(entity.FindProperty("WorkflowMode"));
        Assert.NotNull(entity.FindProperty("Level4Label"));
        Assert.NotNull(entity.FindProperty("Level3Label"));
        Assert.NotNull(entity.FindProperty("Level2Label"));
        Assert.NotNull(entity.FindProperty("Level1Label"));
    }

    [Fact]
    public void Question_HasUniversalValidationFields()
    {
        using var db = CreateInMemoryDbContext();

        var entity = db.Model.FindEntityType(typeof(Question));
        Assert.NotNull(entity);

        Assert.NotNull(entity.FindProperty("MinValue"));
        Assert.NotNull(entity.FindProperty("MaxValue"));
        Assert.NotNull(entity.FindProperty("ValidationRegex"));
    }
}
