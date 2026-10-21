using System;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests;

public class AnswerValueProjectorTests
{
    [Fact]
    public void Apply_Numeric_SetsValueNumberOnly()
    {
        var answer = new Answer { Value = "42.5" };
        AnswerValueProjector.Apply(answer);

        Assert.Equal(42.5m, answer.ValueNumber);
        Assert.Null(answer.ValueDate);
        Assert.Null(answer.ValueBoolean);
        Assert.Equal("42.5", answer.Value);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("False")]
    [InlineData("yes")]
    [InlineData("NO")]
    public void Apply_BooleanAndYesNo_SetValueBooleanOnly(string raw)
    {
        var answer = new Answer { Value = raw };
        AnswerValueProjector.Apply(answer);

        Assert.Null(answer.ValueNumber);
        Assert.Null(answer.ValueDate);
        Assert.Equal(raw is "true" or "yes", answer.ValueBoolean);
        Assert.Equal(raw, answer.Value);
    }

    [Fact]
    public void Apply_IsoDate_SetsValueDateOnly()
    {
        var answer = new Answer { Value = "2024-03-01" };
        AnswerValueProjector.Apply(answer);

        Assert.Null(answer.ValueNumber);
        Assert.Null(answer.ValueBoolean);
        Assert.Equal(new DateTime(2024, 3, 1), answer.ValueDate.Value.Date);
        Assert.Equal("2024-03-01", answer.Value);
    }

    [Fact]
    public void Apply_ArbitraryText_ClearsAllTypedColumns()
    {
        var answer = new Answer { Value = "N/A" };
        AnswerValueProjector.Apply(answer);

        Assert.Null(answer.ValueNumber);
        Assert.Null(answer.ValueDate);
        Assert.Null(answer.ValueBoolean);
    }

    [Fact]
    public void Apply_NullOrWhitespace_ClearsAllTypedColumns()
    {
        foreach (var raw in new[] { null, "", "   " })
        {
            var answer = new Answer { Value = "123", ValueNumber = 123m, ValueDate = DateTime.Today, ValueBoolean = true };
            answer.Value = raw;
            AnswerValueProjector.Apply(answer);

            Assert.Null(answer.ValueNumber);
            Assert.Null(answer.ValueDate);
            Assert.Null(answer.ValueBoolean);
        }
    }

    [Fact]
    public void Apply_ClearsStaleValues_WhenNewValueDoesNotParse()
    {
        var answer = new Answer { Value = "123", ValueNumber = 123m };
        answer.Value = "not-a-number";
        AnswerValueProjector.Apply(answer);

        Assert.Null(answer.ValueNumber);
        Assert.Null(answer.ValueDate);
        Assert.Null(answer.ValueBoolean);
        Assert.Equal("not-a-number", answer.Value);
    }
}
