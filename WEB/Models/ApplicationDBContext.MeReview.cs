using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models
{
    public partial class ApplicationDbContext
    {
        public DbSet<MeReview> MeReviews { get; set; }
        public DbSet<MeReviewSection> MeReviewSections { get; set; }
        public DbSet<MeReviewFinding> MeReviewFindings { get; set; }
        public DbSet<MeReviewRecommendation> MeReviewRecommendations { get; set; }

        public void ConfigureMeReviewModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MeReview>(entity =>
            {
                entity.HasKey(e => e.MeReviewId);
                entity.HasIndex(e => e.Domain).HasDatabaseName("IX_MeReviews_Domain");
                entity.HasIndex(e => e.TargetEntityId).HasDatabaseName("IX_MeReviews_TargetEntityId");
                entity.HasIndex(e => e.CreatedDateUtc).HasDatabaseName("IX_MeReviews_CreatedDateUtc");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MeReviewSection>(entity =>
            {
                entity.HasKey(e => e.MeReviewSectionId);
                entity.HasIndex(e => e.MeReviewId).HasDatabaseName("IX_MeReviewSections_MeReviewId");

                entity.HasOne(e => e.MeReview)
                    .WithMany(r => r.Sections)
                    .HasForeignKey(e => e.MeReviewId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MeReviewFinding>(entity =>
            {
                entity.HasKey(e => e.MeReviewFindingId);
                entity.HasIndex(e => e.MeReviewId).HasDatabaseName("IX_MeReviewFindings_MeReviewId");
                entity.HasIndex(e => e.MeReviewSectionId).HasDatabaseName("IX_MeReviewFindings_MeReviewSectionId");

                entity.HasOne(e => e.MeReview)
                    .WithMany(r => r.Findings)
                    .HasForeignKey(e => e.MeReviewId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Section)
                    .WithMany(s => s.Findings)
                    .HasForeignKey(e => e.MeReviewSectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MeReviewRecommendation>(entity =>
            {
                entity.HasKey(e => e.MeReviewRecommendationId);
                entity.HasIndex(e => e.MeReviewId).HasDatabaseName("IX_MeReviewRecommendations_MeReviewId");
                entity.HasIndex(e => e.MeReviewFindingId).HasDatabaseName("IX_MeReviewRecommendations_MeReviewFindingId");

                entity.HasOne(e => e.MeReview)
                    .WithMany(r => r.Recommendations)
                    .HasForeignKey(e => e.MeReviewId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Finding)
                    .WithMany(f => f.Recommendations)
                    .HasForeignKey(e => e.MeReviewFindingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
