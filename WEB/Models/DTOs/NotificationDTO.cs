using System;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class NotificationDTO
    {
        [Required]
        public Guid NotificationId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Body { get; set; }

        [MaxLength(300)]
        public string Link { get; set; }

        public bool IsRead { get; set; }

        public DateTime? CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public UserDTO CreatedBy { get; set; }
    }

    public static partial class ModelFactory
    {
        public static NotificationDTO Create(Notification notification, bool includeParents = true)
        {
            if (notification == null) return null;

            var dto = new NotificationDTO
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Type = notification.Type,
                Title = notification.Title,
                Body = notification.Body,
                Link = notification.Link,
                IsRead = notification.IsRead,
                CreatedOnUtc = notification.CreatedOnUtc,
                CreatedById = notification.CreatedById
            };

            if (includeParents)
            {
                dto.CreatedBy = Create(notification.CreatedBy);
            }

            return dto;
        }

        public static void Hydrate(Notification notification, NotificationDTO notificationDTO)
        {
            notification.IsRead = notificationDTO.IsRead;
        }
    }
}
