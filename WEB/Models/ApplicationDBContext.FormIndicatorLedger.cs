using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models;

// Form -> indicator ledger schema (Phase 2 engine builds on this; schema only in Phase 1).
public partial class ApplicationDbContext
{
    public DbSet<IndicatorContribution> IndicatorContributions { get; set; }
    public DbSet<FormIndicatorMapping> FormIndicatorMappings { get; set; }
    public DbSet<QuestionIndicatorLink> QuestionIndicatorLinks { get; set; }

    private void ConfigureFormIndicatorLedgerModel(ModelBuilder b)
    {
        b.Entity<IndicatorContribution>(e =>
        {
            e.HasKey(x => x.IndicatorContributionId);
            e.Property(x => x.Value).HasColumnType("decimal(20,8)");
            e.Property(x => x.Numerator).HasColumnType("decimal(20,8)");
            e.Property(x => x.Denominator).HasColumnType("decimal(20,8)");

            // One contribution per response/mapping/option. SQL Server treats NULLs as equal in
            // unique indexes, so two filtered indexes handle the NULL vs non-NULL option cases.
            e.HasIndex(x => new { x.FormIndicatorMappingId, x.ResponseId, x.DisaggregationOptionId })
                .IsUnique().HasFilter("[DisaggregationOptionId] IS NOT NULL")
                .HasDatabaseName("UX_Contributions_Response_Option");

            e.HasIndex(x => new { x.FormIndicatorMappingId, x.ResponseId })
                .IsUnique().HasFilter("[DisaggregationOptionId] IS NULL")
                .HasDatabaseName("UX_Contributions_Response_Total");

            e.HasIndex(x => new { x.IndicatorId, x.EntityId, x.DateId })
                .HasDatabaseName("IX_Contributions_Aggregate");

            e.HasOne(x => x.Response).WithMany()
                .HasForeignKey(x => x.ResponseId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Mapping).WithMany()
                .HasForeignKey(x => x.FormIndicatorMappingId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<FormIndicatorMapping>(e =>
        {
            e.HasKey(x => x.FormIndicatorMappingId);

            e.HasIndex(x => new { x.QuestionnaireId, x.IndicatorId })
                .IsUnique().HasFilter("[SectionId] IS NULL")
                .HasDatabaseName("UX_FormIndicatorMappings_Form_Indicator");

            e.HasIndex(x => new { x.QuestionnaireId, x.IndicatorId, x.SectionId })
                .IsUnique().HasFilter("[SectionId] IS NOT NULL")
                .HasDatabaseName("UX_FormIndicatorMappings_Form_Section_Indicator");

            e.HasOne(x => x.Questionnaire).WithMany()
                .HasForeignKey(x => x.QuestionnaireId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Indicator).WithMany()
                .HasForeignKey(x => x.IndicatorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<QuestionIndicatorLink>(e =>
        {
            e.HasKey(x => x.QuestionIndicatorLinkId);

            e.HasOne(x => x.Mapping).WithMany(m => m.QuestionLinks)
                .HasForeignKey(x => x.FormIndicatorMappingId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Question).WithMany()
                .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.FormIndicatorMappingId, x.Role })
                .HasDatabaseName("IX_QuestionIndicatorLinks_Mapping");
        });
    }
}
