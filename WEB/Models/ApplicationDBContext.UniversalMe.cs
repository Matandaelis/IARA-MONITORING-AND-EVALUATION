using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models
{
    public partial class ApplicationDbContext
    {
        public DbSet<BeneficiaryFeedback> BeneficiaryFeedbacks { get; set; }
        public DbSet<LogFrameRowRiskAssumption> LogFrameRowRiskAssumptions { get; set; }
        public DbSet<LogFrameRowTask> LogFrameRowTasks { get; set; }
        public DbSet<LogFrameRowBudgetLine> LogFrameRowBudgetLines { get; set; }

        public void ConfigureUniversalMeModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BeneficiaryFeedback>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.ProjectId, e.Status })
                    .HasDatabaseName("IX_BeneficiaryFeedbacks_Project_Status");

                entity.HasIndex(e => e.TrackingCode)
                    .HasDatabaseName("UX_BeneficiaryFeedbacks_TrackingCode")
                    .IsUnique();

                entity.HasOne(e => e.Project)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LogFrameRowRiskAssumption>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.LogFrameRowId, e.RiskAssumptionId })
                    .HasDatabaseName("UX_LogFrameRowRiskAssumptions_Row_Risk")
                    .IsUnique();

                entity.HasOne(e => e.LogFrameRow)
                    .WithMany(r => r.LogFrameRowRiskAssumptions)
                    .HasForeignKey(e => e.LogFrameRowId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RiskAssumption)
                    .WithMany()
                    .HasForeignKey(e => e.RiskAssumptionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LogFrameRowTask>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.LogFrameRowId, e.TaskId })
                    .HasDatabaseName("UX_LogFrameRowTasks_Row_Task")
                    .IsUnique();

                entity.HasOne(e => e.LogFrameRow)
                    .WithMany(r => r.LogFrameRowTasks)
                    .HasForeignKey(e => e.LogFrameRowId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Task)
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LogFrameRowBudgetLine>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.LogFrameRowId, e.BudgetLineId })
                    .HasDatabaseName("UX_LogFrameRowBudgetLines_Row_BudgetLine")
                    .IsUnique();

                entity.HasOne(e => e.LogFrameRow)
                    .WithMany(r => r.LogFrameRowBudgetLines)
                    .HasForeignKey(e => e.LogFrameRowId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.BudgetLine)
                    .WithMany()
                    .HasForeignKey(e => e.BudgetLineId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<IndicatorTarget>(entity =>
            {
                entity.HasOne(e => e.DisaggregationOption)
                    .WithMany()
                    .HasForeignKey(e => e.DisaggregationOptionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IndicatorId, e.DateId, e.EntityId, e.DisaggregationOptionId })
                    .HasDatabaseName("IX_IndicatorTargets_Indicator_Date_Entity_Option");
            });

            modelBuilder.Entity<Indicator>(entity =>
            {
                entity.HasOne(e => e.NumeratorIndicator)
                    .WithMany()
                    .HasForeignKey(e => e.NumeratorIndicatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.DenominatorIndicator)
                    .WithMany()
                    .HasForeignKey(e => e.DenominatorIndicatorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
