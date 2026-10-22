using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models
{
    public partial class ApplicationDbContext
    {
        public DbSet<BudgetCategory> BudgetCategories { get; set; }
        public DbSet<BudgetLine> BudgetLines { get; set; }
        public DbSet<FxRate> FxRates { get; set; }

        public void ConfigureBudgetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BudgetCategory>(entity =>
            {
                entity.HasKey(e => e.BudgetCategoryId);
                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("IX_BudgetCategories_Name");
                entity.HasIndex(e => e.SortOrder).HasDatabaseName("IX_BudgetCategories_SortOrder");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BudgetLine>(entity =>
            {
                entity.HasKey(e => e.BudgetLineId);
                entity.HasIndex(e => e.ProjectId).HasDatabaseName("IX_BudgetLines_ProjectId");
                entity.HasIndex(e => e.BudgetCategoryId).HasDatabaseName("IX_BudgetLines_BudgetCategoryId");
                entity.HasIndex(e => e.DateId).HasDatabaseName("IX_BudgetLines_DateId");
                entity.HasIndex(e => e.TaskId).HasDatabaseName("IX_BudgetLines_TaskId");
                entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_BudgetLines_IsDeleted");

                entity.HasOne(e => e.Project)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.BudgetCategory)
                    .WithMany(c => c.BudgetLines)
                    .HasForeignKey(e => e.BudgetCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Date)
                    .WithMany()
                    .HasForeignKey(e => e.DateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Task)
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ModifiedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FxRate>(entity =>
            {
                entity.HasKey(e => e.FxRateId);
                entity.HasIndex(e => new { e.FromCurrency, e.ToCurrency, e.EffectiveFrom })
                    .IsUnique()
                    .HasDatabaseName("IX_FxRates_From_To_EffectiveFrom");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
