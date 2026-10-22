using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Models;
using Monic.Web.Code;

namespace Monic.Web.Controllers
{
    [Route("api/[Controller]"), Authorize]
    public class FxRatesController : BaseApiController
    {
        public FxRatesController(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<User> um, AppSettings appSettings)
            : base(dbFactory, um, appSettings)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string fromCurrency, string toCurrency)
        {
            IQueryable<FxRate> results = db.FxRates;

            if (!string.IsNullOrWhiteSpace(fromCurrency))
                results = results.Where(o => o.FromCurrency == fromCurrency.ToUpperInvariant());

            if (!string.IsNullOrWhiteSpace(toCurrency))
                results = results.Where(o => o.ToCurrency == toCurrency.ToUpperInvariant());

            results = results.OrderByDescending(o => o.EffectiveFrom).ThenBy(o => o.FromCurrency);

            var searchOptions = new SearchOptions { PageSize = 0 };
            return Ok(await GetPaginatedResponse(results, searchOptions));
        }

        [HttpPost("{fxRateId:Guid}"), AuthorizeRoles(Roles.Manager, Roles.Finance)]
        public async Task<IActionResult> Save(Guid fxRateId, [FromBody] FxRateDTO fxRateDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (fxRateDTO.FxRateId != fxRateId) return BadRequest("Id mismatch");

            if (string.Equals(fxRateDTO.FromCurrency, fxRateDTO.ToCurrency, StringComparison.OrdinalIgnoreCase))
                return BadRequest("From and To currencies must differ");

            if (fxRateDTO.Rate <= 0) return BadRequest("Rate must be greater than zero");

            var isNew = fxRateDTO.FxRateId == Guid.Empty;

            FxRate fxRate;
            object beforeSnapshot = null;
            if (isNew)
            {
                fxRate = new FxRate { CreatedById = CurrentUser.Id };

                db.Entry(fxRate).State = EntityState.Added;
            }
            else
            {
                fxRate = await db.FxRates
                    .FirstOrDefaultAsync(o => o.FxRateId == fxRateDTO.FxRateId);

                if (fxRate == null)
                    return NotFound();

                beforeSnapshot = ModelFactory.Create(fxRate);

                db.Entry(fxRate).State = EntityState.Modified;
            }

            ModelFactory.Hydrate(fxRate, fxRateDTO);

            if (await db.FxRates.AnyAsync(o =>
                o.FromCurrency == fxRate.FromCurrency
                && o.ToCurrency == fxRate.ToCurrency
                && o.EffectiveFrom == fxRate.EffectiveFrom
                && o.FxRateId != fxRate.FxRateId))
                return BadRequest("A rate for this currency pair and effective date already exists");

            await db.SaveChangesAsync();

            AuditHelper.Write(db, HttpContext, CurrentUser, "FxRate", fxRate.FxRateId,
                isNew ? "Create" : "Update", beforeSnapshot, ModelFactory.Create(fxRate));
            await db.SaveChangesAsync();

            return Ok(ModelFactory.Create(fxRate));
        }

        [HttpDelete("{fxRateId:Guid}"), AuthorizeRoles(Roles.Manager, Roles.Finance)]
        public async Task<IActionResult> Delete(Guid fxRateId)
        {
            var fxRate = await db.FxRates
                .FirstOrDefaultAsync(o => o.FxRateId == fxRateId);

            if (fxRate == null)
                return NotFound();

            var beforeSnapshot = ModelFactory.Create(fxRate);

            db.Entry(fxRate).State = EntityState.Deleted;

            await db.SaveChangesAsync();

            AuditHelper.Write(db, HttpContext, CurrentUser, "FxRate", fxRateId, "Delete", beforeSnapshot, null);
            await db.SaveChangesAsync();

            return Ok();
        }

    }
}
