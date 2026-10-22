using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class TagDTO
    {
        [Required]
        public Guid TagId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }

        public DateTime? CreatedOnUtc { get; set; }

        public Guid? CreatedById { get; set; }

        public UserDTO CreatedBy { get; set; }

        public int DocumentCount { get; set; }
    }

    public static partial class ModelFactory
    {
        public static TagDTO Create(Tag tag, bool includeParents = true)
        {
            if (tag == null) return null;

            var dto = new TagDTO
            {
                TagId = tag.TagId,
                Name = tag.Name,
                Description = tag.Description,
                CreatedOnUtc = tag.CreatedOnUtc,
                CreatedById = tag.CreatedById,
                DocumentCount = tag.DocumentTags?.Count ?? 0
            };

            if (includeParents)
            {
                dto.CreatedBy = Create(tag.CreatedBy);
            }

            return dto;
        }

        public static void Hydrate(Tag tag, TagDTO tagDTO)
        {
            tag.Name = tagDTO.Name;
            tag.Description = tagDTO.Description;
        }
    }
}
