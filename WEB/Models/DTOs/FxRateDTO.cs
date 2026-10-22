using System;
using System.ComponentModel.DataAnnotations;

namespace Monic.Web.Models
{
    public class FxRateDTO
    {
        [Required]
        public Guid FxRateId { get; set; }

        [Required, MaxLength(3), MinLength(3)]
        public string FromCurrency { get; set; }

        [Required, MaxLength(3), MinLength(3)]
        public string ToCurrency { get; set; }

        [Required]
        public decimal Rate { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? CreatedOnUtc { get; set; }
    }

    public static partial class ModelFactory
    {
        public static FxRateDTO Create(FxRate fxRate)
        {
            if (fxRate == null) return null;

            return new FxRateDTO
            {
                FxRateId = fxRate.FxRateId,
                FromCurrency = fxRate.FromCurrency,
                ToCurrency = fxRate.ToCurrency,
                Rate = fxRate.Rate,
                EffectiveFrom = fxRate.EffectiveFrom,
                CreatedOnUtc = fxRate.CreatedOnUtc
            };
        }

        public static void Hydrate(FxRate fxRate, FxRateDTO fxRateDTO)
        {
            fxRate.FromCurrency = fxRateDTO.FromCurrency.ToUpperInvariant();
            fxRate.ToCurrency = fxRateDTO.ToCurrency.ToUpperInvariant();
            fxRate.Rate = fxRateDTO.Rate;
            fxRate.EffectiveFrom = fxRateDTO.EffectiveFrom.Date;
        }
    }
}
