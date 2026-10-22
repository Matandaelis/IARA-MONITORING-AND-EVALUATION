using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models
{
    public partial class ApplicationDbContext
    {
        public DbSet<ImportJob> ImportJobs { get; set; }
        public DbSet<ImportJobContent> ImportJobContents { get; set; }

        public void ConfigureImportModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ImportJob>(entity =>
            {
                entity.HasKey(e => e.ImportJobId);
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_ImportJobs_Status");
                entity.HasIndex(e => e.ImportType).HasDatabaseName("IX_ImportJobs_ImportType");
                entity.HasIndex(e => e.CreatedOnUtc).HasDatabaseName("IX_ImportJobs_CreatedOnUtc");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CommittedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CommittedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ImportJobContent)
                    .WithOne(c => c.ImportJob)
                    .HasForeignKey<ImportJobContent>(c => c.ImportJobId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ImportJobContent>(entity =>
            {
                entity.HasKey(e => e.ImportJobId);
            });
        }
    }
}
