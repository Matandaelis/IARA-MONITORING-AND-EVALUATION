using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models
{
    /// <summary>
    /// Monitoring alignment schema: project indicator assignment, the monitoring plan/matrix,
    /// risk &amp; assumption register, monitoring events/actions, compliance checks and lessons learned.
    /// </summary>
    public partial class ApplicationDbContext
    {
        public DbSet<ProjectIndicator> ProjectIndicators { get; set; }
        public DbSet<MonitoringPlan> MonitoringPlans { get; set; }
        public DbSet<MonitoringPlanItem> MonitoringPlanItems { get; set; }
        public DbSet<RiskAssumption> RiskAssumptions { get; set; }
        public DbSet<RiskAssumptionUpdate> RiskAssumptionUpdates { get; set; }
        public DbSet<MonitoringEvent> MonitoringEvents { get; set; }
        public DbSet<MonitoringAction> MonitoringActions { get; set; }
        public DbSet<MonitoringCheck> MonitoringChecks { get; set; }
        public DbSet<LessonLearned> LessonsLearned { get; set; }

        public void ConfigureMonitoringModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectIndicator>(entity =>
            {
                entity.HasKey(e => new { e.ProjectId, e.IndicatorId });
                entity.HasIndex(e => e.IndicatorId).HasDatabaseName("IX_ProjectIndicators_IndicatorId");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Indicator).WithMany()
                    .HasForeignKey(e => e.IndicatorId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany()
                    .HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MonitoringPlan>(entity =>
            {
                entity.HasKey(e => e.MonitoringPlanId);
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_MonitoringPlans_ProjectId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_MonitoringPlans_Status");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FromDate).WithMany()
                    .HasForeignKey(e => e.FromDateId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ToDate).WithMany()
                    .HasForeignKey(e => e.ToDateId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OwnerUser).WithMany()
                    .HasForeignKey(e => e.OwnerUserId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany()
                    .HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MonitoringPlanItem>(entity =>
            {
                entity.HasKey(e => e.MonitoringPlanItemId);
                entity.HasIndex(e => e.MonitoringPlanId).HasDatabaseName("IX_MonitoringPlanItems_PlanId");
                entity.HasIndex(e => e.IndicatorId).HasDatabaseName("IX_MonitoringPlanItems_IndicatorId");
                entity.HasIndex(e => new { e.MonitoringPlanId, e.IndicatorId }).IsUnique()
                    .HasDatabaseName("UX_MonitoringPlanItems_Plan_Indicator");

                entity.HasOne(e => e.MonitoringPlan).WithMany(p => p.Items)
                    .HasForeignKey(e => e.MonitoringPlanId).OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Indicator).WithMany()
                    .HasForeignKey(e => e.IndicatorId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ResponsibleUser).WithMany()
                    .HasForeignKey(e => e.ResponsibleUserId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ResponsibleEntity).WithMany()
                    .HasForeignKey(e => e.ResponsibleEntityId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RiskAssumption>(entity =>
            {
                entity.HasKey(e => e.RiskAssumptionId);
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_RiskAssumptions_ProjectId");
                entity.HasIndex(e => e.LogFrameNodeId).HasDatabaseName("IX_RiskAssumptions_LogFrameNodeId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_RiskAssumptions_Status");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LogFrameNode).WithMany()
                    .HasForeignKey(e => e.LogFrameNodeId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OwnerUser).WithMany()
                    .HasForeignKey(e => e.OwnerUserId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany()
                    .HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RiskAssumptionUpdate>(entity =>
            {
                entity.HasKey(e => e.RiskAssumptionUpdateId);
                entity.HasIndex(e => e.RiskAssumptionId).HasDatabaseName("IX_RiskAssumptionUpdates_RiskId");
                entity.HasIndex(e => e.DateId).HasDatabaseName("IX_RiskAssumptionUpdates_DateId");

                entity.HasOne(e => e.RiskAssumption).WithMany(r => r.Updates)
                    .HasForeignKey(e => e.RiskAssumptionId).OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Date).WithMany()
                    .HasForeignKey(e => e.DateId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.EvidenceDocument).WithMany()
                    .HasForeignKey(e => e.EvidenceDocumentId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RecordedBy).WithMany()
                    .HasForeignKey(e => e.RecordedById).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MonitoringEvent>(entity =>
            {
                entity.HasKey(e => e.MonitoringEventId);
                entity.HasIndex(e => new { e.ProjectId, e.EventDate }).HasDatabaseName("IX_MonitoringEvents_Project_Date");
                entity.HasIndex(e => e.EntityId).HasDatabaseName("IX_MonitoringEvents_EntityId");
                entity.HasIndex(e => e.Type).HasDatabaseName("IX_MonitoringEvents_Type");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Entity).WithMany()
                    .HasForeignKey(e => e.EntityId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PeriodDate).WithMany()
                    .HasForeignKey(e => e.PeriodDateId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ConductedBy).WithMany()
                    .HasForeignKey(e => e.ConductedById).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.MeReview).WithMany()
                    .HasForeignKey(e => e.MeReviewId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Document).WithMany()
                    .HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany()
                    .HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MonitoringAction>(entity =>
            {
                entity.HasKey(e => e.MonitoringActionId);
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_MonitoringActions_ProjectId");
                entity.HasIndex(e => e.MonitoringEventId).HasDatabaseName("IX_MonitoringActions_EventId");
                entity.HasIndex(e => e.RiskAssumptionId).HasDatabaseName("IX_MonitoringActions_RiskId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_MonitoringActions_Status");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.MonitoringEvent).WithMany(ev => ev.Actions)
                    .HasForeignKey(e => e.MonitoringEventId).OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RiskAssumption).WithMany()
                    .HasForeignKey(e => e.RiskAssumptionId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OwnerUser).WithMany()
                    .HasForeignKey(e => e.OwnerUserId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LinkedTask).WithMany()
                    .HasForeignKey(e => e.LinkedTaskId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany()
                    .HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MonitoringCheck>(entity =>
            {
                entity.HasKey(e => e.MonitoringCheckId);
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_MonitoringChecks_ProjectId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_MonitoringChecks_Status");
                entity.HasIndex(e => e.Type).HasDatabaseName("IX_MonitoringChecks_Type");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Entity).WithMany()
                    .HasForeignKey(e => e.EntityId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PeriodDate).WithMany()
                    .HasForeignKey(e => e.PeriodDateId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.EvidenceDocument).WithMany()
                    .HasForeignKey(e => e.EvidenceDocumentId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany()
                    .HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LessonLearned>(entity =>
            {
                entity.HasKey(e => e.LessonLearnedId);
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_LessonsLearned_ProjectId");
                entity.HasIndex(e => e.MonitoringType).HasDatabaseName("IX_LessonsLearned_MonitoringType");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Component).WithMany()
                    .HasForeignKey(e => e.ComponentId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LogFrameNode).WithMany()
                    .HasForeignKey(e => e.LogFrameNodeId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RecordedBy).WithMany()
                    .HasForeignKey(e => e.RecordedById).OnDelete(DeleteBehavior.Restrict);
            });

            // New nullable links on existing entities
            modelBuilder.Entity<Indicator>(entity =>
            {
                entity.HasIndex(e => e.MonitoringType).HasDatabaseName("IX_Indicators_MonitoringType");
                entity.HasIndex(e => e.ResponsibleUserId).HasDatabaseName("IX_Indicators_ResponsibleUserId");

                entity.HasOne(e => e.ResponsibleUser).WithMany()
                    .HasForeignKey(e => e.ResponsibleUserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BudgetLine>(entity =>
            {
                entity.HasIndex(e => e.LogFrameNodeId).HasDatabaseName("IX_BudgetLines_LogFrameNodeId");
                entity.HasIndex(e => e.IndicatorId).HasDatabaseName("IX_BudgetLines_IndicatorId");

                entity.HasOne(e => e.LogFrameNode).WithMany()
                    .HasForeignKey(e => e.LogFrameNodeId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Indicator).WithMany()
                    .HasForeignKey(e => e.IndicatorId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LogFrame>(entity =>
            {
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_LogFrames_ProjectId");
                entity.HasIndex(e => e.TheoryOfChangeId).HasDatabaseName("IX_LogFrames_TheoryOfChangeId");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TheoryOfChange).WithMany()
                    .HasForeignKey(e => e.TheoryOfChangeId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TheoryOfChange>(entity =>
            {
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_TheoriesOfChange_ProjectId");

                entity.HasOne(e => e.Project).WithMany()
                    .HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Datum>(entity =>
            {
                entity.HasIndex(e => new { e.EntityId, e.DateId }).HasDatabaseName("IX_Data_Entity_Date");
            });
        }
    }
}
