using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class DisaggregationGroupDTO
    {
        public Guid DisaggregationGroupId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<DisaggregationOptionDTO> Options { get; set; } = new List<DisaggregationOptionDTO>();
    }

    public class DisaggregationOptionDTO
    {
        public Guid DisaggregationOptionId { get; set; }

        public Guid DisaggregationGroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Code { get; set; }

        public int SortOrder { get; set; } = 0;
    }

    public class DisaggregationDatumDTO
    {
        public Guid DisaggregationDatumId { get; set; }

        public Guid DatumId { get; set; }

        [Required]
        public Guid DisaggregationOptionId { get; set; }

        public string OptionName { get; set; }

        public string OptionCode { get; set; }

        [Required]
        public decimal Value { get; set; }
    }

    public static partial class ModelFactory
    {
        public static DisaggregationGroupDTO Create(DisaggregationGroup group, bool includeChildren = true)
        {
            if (group == null) return null;

            var dto = new DisaggregationGroupDTO
            {
                DisaggregationGroupId = group.DisaggregationGroupId,
                Name = group.Name,
                Description = group.Description,
                IsActive = group.IsActive,
                CreatedAt = group.CreatedAt
            };

            if (includeChildren && group.Options != null)
            {
                foreach (var opt in group.Options)
                {
                    dto.Options.Add(Create(opt));
                }
            }

            return dto;
        }

        public static DisaggregationOptionDTO Create(DisaggregationOption option)
        {
            if (option == null) return null;

            return new DisaggregationOptionDTO
            {
                DisaggregationOptionId = option.DisaggregationOptionId,
                DisaggregationGroupId = option.DisaggregationGroupId,
                Name = option.Name,
                Code = option.Code,
                SortOrder = option.SortOrder
            };
        }

        public static DisaggregationDatumDTO Create(DisaggregationDatum datum)
        {
            if (datum == null) return null;

            return new DisaggregationDatumDTO
            {
                DisaggregationDatumId = datum.DisaggregationDatumId,
                DatumId = datum.DatumId,
                DisaggregationOptionId = datum.DisaggregationOptionId,
                OptionName = datum.DisaggregationOption?.Name,
                OptionCode = datum.DisaggregationOption?.Code,
                Value = datum.Value
            };
        }

        public static void Hydrate(DisaggregationGroup group, DisaggregationGroupDTO dto)
        {
            group.Name = dto.Name;
            group.Description = dto.Description;
            group.IsActive = dto.IsActive;
        }

        public static void Hydrate(DisaggregationOption option, DisaggregationOptionDTO dto)
        {
            option.Name = dto.Name;
            option.Code = dto.Code;
            option.SortOrder = dto.SortOrder;
        }
    }
}
