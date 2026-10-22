using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models
{
    public partial class ApplicationDbContext
    {
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentReaction> CommentReactions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<DocumentTag> DocumentTags { get; set; }
        public DbSet<IndicatorTag> IndicatorTags { get; set; }

        public void ConfigureCollaborationModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.CommentId);
                entity.HasIndex(e => new { e.SubjectType, e.SubjectId }).HasDatabaseName("IX_Comments_Subject");
                entity.HasIndex(e => e.CreatedOnUtc).HasDatabaseName("IX_Comments_CreatedOnUtc");

                entity.HasOne(e => e.ParentComment)
                    .WithMany()
                    .HasForeignKey(e => e.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ModifiedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ModifiedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Reactions)
                    .WithOne(r => r.Comment)
                    .HasForeignKey(r => r.CommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommentReaction>(entity =>
            {
                entity.HasKey(e => e.CommentReactionId);
                entity.HasIndex(e => new { e.CommentId, e.UserId }).HasDatabaseName("IX_CommentReactions_CommentId_UserId");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.HasIndex(e => new { e.UserId, e.IsRead }).HasDatabaseName("IX_Notifications_UserId_IsRead");
                entity.HasIndex(e => e.CreatedOnUtc).HasDatabaseName("IX_Notifications_CreatedOnUtc");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.TagId);
                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("IX_Tags_Name");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DocumentTag>(entity =>
            {
                entity.HasKey(e => e.DocumentTagId);
                entity.HasIndex(e => e.DocumentId).HasDatabaseName("IX_DocumentTags_DocumentId");
                entity.HasIndex(e => e.TagId).HasDatabaseName("IX_DocumentTags_TagId");
                entity.HasIndex(e => new { e.DocumentId, e.TagId }).IsUnique().HasDatabaseName("IX_DocumentTags_DocumentId_TagId");

                entity.HasOne(e => e.Document)
                    .WithMany(d => d.DocumentTags)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Tag)
                    .WithMany(t => t.DocumentTags)
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NotificationPreference>(entity =>
            {
                entity.HasKey(e => e.NotificationPreferenceId);
                entity.HasIndex(e => new { e.UserId, e.Type }).IsUnique().HasDatabaseName("IX_NotificationPreferences_UserId_Type");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<IndicatorTag>(entity =>
            {
                entity.HasKey(e => e.IndicatorTagId);
                entity.HasIndex(e => e.IndicatorId).HasDatabaseName("IX_IndicatorTags_IndicatorId");
                entity.HasIndex(e => e.TagId).HasDatabaseName("IX_IndicatorTags_TagId");
                entity.HasIndex(e => new { e.IndicatorId, e.TagId }).IsUnique().HasDatabaseName("IX_IndicatorTags_IndicatorId_TagId");

                entity.HasOne(e => e.Indicator)
                    .WithMany(i => i.IndicatorTags)
                    .HasForeignKey(e => e.IndicatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Tag)
                    .WithMany()
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
