using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monic.Web.Models;

// Append-only audit trace for approvals, value changes and GDPR-relevant actions.
public class AuditLog
{
    [Key] public Guid AuditLogId { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)] public string EntityType { get; set; } = "";
    [Required] public Guid EntityId { get; set; }
    [Required, MaxLength(50)] public string Action { get; set; } = "";
    public Guid? UserId { get; set; }
    public string BeforeJson { get; set; }
    public string AfterJson { get; set; }
    [MaxLength(64)] public string IpAddress { get; set; }
    [MaxLength(300)] public string UserAgent { get; set; }

    [Required] public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
