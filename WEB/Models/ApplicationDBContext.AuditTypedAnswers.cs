using Microsoft.EntityFrameworkCore;

namespace Monic.Web.Models;

// Audit log, typed answer projections and response sync columns.
public partial class ApplicationDbContext
{
    public DbSet<AuditLog> AuditLogs { get; set; }

    private void ConfigureAuditTypedAnswersModel(ModelBuilder b)
    {
        b.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.AuditLogId);
            e.Property(x => x.EntityType).HasMaxLength(50);
            e.Property(x => x.Action).HasMaxLength(50);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(300);

            e.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc })
                .HasDatabaseName("IX_AuditLogs_Entity_CreatedAt");
        });

        // Typed analytics index: numeric answers per question.
        b.Entity<Answer>(e =>
        {
            e.HasIndex(x => new { x.QuestionId, x.ValueNumber })
                .HasFilter("[ValueNumber] IS NOT NULL")
                .HasDatabaseName("IX_Answers_Question_Number");
        });

        // Idempotent offline submission: only non-deleted rows carrying a client id
        // participate in the unique constraint, so a re-submit with the same id is safe.
        b.Entity<Response>(e =>
        {
            e.HasIndex(x => new { x.QuestionnaireId, x.ClientGeneratedId })
                .IsUnique()
                .HasFilter("[ClientGeneratedId] IS NOT NULL AND [IsDeleted] = 0")
                .HasDatabaseName("UX_Responses_ClientGeneratedId");
        });
    }
}
