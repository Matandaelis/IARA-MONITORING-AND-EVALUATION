using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models;

// Immutable ledger row produced by a form submission. One row per response x mapping x option.
public class IndicatorContribution
{
    [Key] public Guid IndicatorContributionId { get; set; } = Guid.NewGuid();

    [Required] public Guid FormIndicatorMappingId { get; set; }
    [Required] public Guid ResponseId { get; set; }
    [Required] public Guid IndicatorId { get; set; }
    [Required] public Guid EntityId { get; set; }
    [Required] public Guid DateId { get; set; }

    // null = total row; set = one row per disaggregation option
    public Guid? DisaggregationOptionId { get; set; }

    [Column(TypeName = "decimal(20,8)")] public decimal Value { get; set; }
    [Column(TypeName = "decimal(20,8)")] public decimal? Numerator { get; set; }
    [Column(TypeName = "decimal(20,8)")] public decimal? Denominator { get; set; }

    [Required] public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; }

    public virtual FormIndicatorMapping Mapping { get; set; }
    public virtual Response Response { get; set; }
}

// Form -> indicator rule. Multiple rules per form are allowed; one per indicator per section.
public class FormIndicatorMapping
{
    [Key] public Guid FormIndicatorMappingId { get; set; } = Guid.NewGuid();

    [Required] public Guid QuestionnaireId { get; set; }
    [Required] public Guid IndicatorId { get; set; }
    public Guid? SectionId { get; set; }

    [Required] public MappingAggregation Aggregation { get; set; } = MappingAggregation.Sum;

    // Safe, non-eval expression tree, e.g. {"op":"sum","args":[{"op":"q","code":"Q1"},{"op":"q","code":"Q2"}]}
    [Required] public string ValueExpressionJson { get; set; } = "{\"op\":\"sum\",\"args\":[]}";

    // {"source":"response"} or {"questionCode":"Q_DATE"}
    public string DateBindingJson { get; set; } = "{\"source\":\"response\"}";
    public string EntityBindingJson { get; set; } = "{\"source\":\"response\"}";

    // Optional predicate; false = skip the response
    public string FilterJson { get; set; }

    public Guid? DisaggregationGroupId { get; set; }
    [Required] public bool CountOnlySubmitted { get; set; } = true;
    [Required] public bool IsActive { get; set; } = true;

    [Required] public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedById { get; set; }

    public virtual Questionnaire Questionnaire { get; set; }
    public virtual Indicator Indicator { get; set; }
    public virtual ICollection<QuestionIndicatorLink> QuestionLinks { get; set; }
        = new List<QuestionIndicatorLink>();
}

public enum MappingAggregation { Sum, Count, CountDistinct, Average, Min, Max, MostRecent }
public enum QuestionIndicatorRole { Value, Numerator, Denominator, Disaggregation, Filter, Date, Entity }

public class QuestionIndicatorLink
{
    [Key] public Guid QuestionIndicatorLinkId { get; set; } = Guid.NewGuid();
    [Required] public Guid FormIndicatorMappingId { get; set; }
    [Required] public Guid QuestionId { get; set; }
    [Required] public QuestionIndicatorRole Role { get; set; }

    // For Role = Disaggregation: which option this question value represents.
    public Guid? DisaggregationOptionId { get; set; }

    // {"yes":1,"no":0} for qualitative/option scoring; keyed by QuestionOptionId
    public string ValueMapJson { get; set; }
    [Required] public int SortOrder { get; set; }

    public virtual FormIndicatorMapping Mapping { get; set; }
    public virtual Question Question { get; set; }
}
