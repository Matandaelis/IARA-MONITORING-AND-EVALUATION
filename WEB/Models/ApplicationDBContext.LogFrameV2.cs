using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models;

// Adds the LogFrame v2 schema alongside the flat LogFrameRow / LogFrameLevel configuration.
public partial class ApplicationDbContext
{
    public DbSet<LogFrameNode> LogFrameNodes { get; set; }
    public DbSet<LogFrameLevel> LogFrameLevels { get; set; }
    public DbSet<LogFrameNodeIndicator> LogFrameNodeIndicators { get; set; }

    private void ConfigureLogFrameV2Model(ModelBuilder b)
    {
        b.Entity<LogFrameNode>(e =>
        {
            e.HasKey(x => x.LogFrameNodeId);

            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Title).HasMaxLength(500);
            e.Property(x => x.CustomFieldsJson).HasColumnType("nvarchar(max)");
            e.Property(x => x.RowVersion).IsRowVersion();

            e.HasOne(x => x.LogFrame).WithMany()
                .HasForeignKey(x => x.LogFrameId);

            e.HasOne(x => x.Level).WithMany(l => l.Nodes)
                .HasForeignKey(x => x.LogFrameLevelId);

            e.HasOne(x => x.ParentNode).WithMany(x => x.ChildNodes)
                .HasForeignKey(x => x.ParentNodeId);

            e.HasIndex(x => new { x.LogFrameId, x.ParentNodeId, x.SortOrder })
                .HasDatabaseName("IX_LogFrameNodes_Tree");

            e.HasIndex(x => new { x.LogFrameId, x.Code })
                .IsUnique().HasFilter("[Code] <> ''")
                .HasDatabaseName("UX_LogFrameNodes_Code");

            e.HasIndex(x => x.LegacyLogFrameRowId)
                .HasDatabaseName("IX_LogFrameNodes_LegacyRow");
        });

        b.Entity<LogFrameLevel>(e =>
        {
            e.HasKey(x => x.LogFrameLevelId);

            e.HasIndex(x => new { x.LogFrameId, x.Rank })
                .IsUnique().HasFilter("[LogFrameId] IS NOT NULL")
                .HasDatabaseName("UX_LogFrameLevels_LogFrame_Rank");

            e.HasIndex(x => new { x.OrganisationId, x.Rank })
                .IsUnique().HasFilter("[OrganisationId] IS NOT NULL AND [LogFrameId] IS NULL")
                .HasDatabaseName("UX_LogFrameLevels_Org_Rank");
        });

        b.Entity<LogFrameNodeIndicator>(e =>
        {
            e.HasKey(x => new { x.LogFrameNodeId, x.IndicatorId });
            e.Property(x => x.Weight).HasColumnType("decimal(9,4)");

            e.HasOne(x => x.Node).WithMany(n => n.Indicators)
                .HasForeignKey(x => x.LogFrameNodeId);

            e.HasOne(x => x.Indicator).WithMany()
                .HasForeignKey(x => x.IndicatorId);

            e.HasIndex(x => x.IndicatorId)
                .HasDatabaseName("IX_LogFrameNodeIndicators_Indicator");
        });
    }
}
