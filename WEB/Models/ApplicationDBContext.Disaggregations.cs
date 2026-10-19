using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models
{
    public partial class ApplicationDbContext
    {
        public DbSet<DisaggregationGroup> DisaggregationGroups { get; set; }
        public DbSet<DisaggregationOption> DisaggregationOptions { get; set; }
        public DbSet<DisaggregationDatum> DisaggregationData { get; set; }
        public DbSet<IndicatorTarget> IndicatorTargets { get; set; }

        public void ConfigureDisaggregationModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DisaggregationGroup>(entity =>
            {
                entity.HasKey(e => e.DisaggregationGroupId);
                entity.HasIndex(e => e.Name).HasDatabaseName("IX_DisaggregationGroups_Name");

                entity.HasMany(g => g.Options)
                    .WithOne(o => o.DisaggregationGroup)
                    .HasForeignKey(o => o.DisaggregationGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(g => g.Indicators)
                    .WithOne(i => i.DisaggregationGroup)
                    .HasForeignKey(i => i.DisaggregationGroupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DisaggregationOption>(entity =>
            {
                entity.HasKey(e => e.DisaggregationOptionId);
                entity.HasIndex(e => new { e.DisaggregationGroupId, e.Code })
                    .HasDatabaseName("IX_DisaggregationOptions_GroupId_Code");

                entity.HasMany(o => o.DisaggregationData)
                    .WithOne(d => d.DisaggregationOption)
                    .HasForeignKey(d => d.DisaggregationOptionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DisaggregationDatum>(entity =>
            {
                entity.HasKey(e => e.DisaggregationDatumId);

                entity.Property(e => e.Value).HasColumnType("decimal(20, 8)");

                entity.HasIndex(e => new { e.IndicatorId, e.EntityId, e.DateId, e.DisaggregationOptionId })
                    .HasDatabaseName("IX_DisaggregationData_Datum_OptionId")
                    .IsUnique();

                entity.HasOne(d => d.Datum)
                    .WithMany(p => p.DisaggregationData)
                    .HasForeignKey(d => new { d.IndicatorId, d.EntityId, d.DateId })
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<IndicatorTarget>(entity =>
            {
                entity.HasKey(e => e.IndicatorTargetId);

                entity.Property(e => e.TargetValue).HasColumnType("decimal(20, 8)");
                entity.Property(e => e.BaselineValue).HasColumnType("decimal(20, 8)");

                entity.HasIndex(e => new { e.IndicatorId, e.DateId, e.EntityId })
                    .HasDatabaseName("IX_IndicatorTargets_Indicator_Date_Entity");

                entity.HasOne(e => e.Indicator)
                    .WithMany(i => i.IndicatorTargets)
                    .HasForeignKey(e => e.IndicatorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Date)
                    .WithMany()
                    .HasForeignKey(e => e.DateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Entity)
                    .WithMany()
                    .HasForeignKey(e => e.EntityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
