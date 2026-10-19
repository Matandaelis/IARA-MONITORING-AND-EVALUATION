# Complete Example: ASP.NET Core REST API

## Controller Implementation

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class IndicatorsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<IndicatorsController> _logger;

        public IndicatorsController(ApplicationDbContext db, ILogger<IndicatorsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// List indicators with pagination and filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<IndicatorDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIndicators([FromQuery] IndicatorFilterQuery query, CancellationToken ct)
        {
            var page = Math.Max(1, query.Page ?? 1);
            var pageSize = Math.Clamp(query.PageSize ?? 20, 1, 100);

            var dbQuery = _db.Indicators
                .AsNoTracking()
                .Where(i => !i.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                dbQuery = dbQuery.Where(i => i.Name.Contains(query.Search));
            }

            var totalItems = await dbQuery.CountAsync(ct);
            var items = await dbQuery
                .OrderBy(i => i.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new IndicatorDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Code = i.Code,
                    Unit = i.Unit,
                    IndicatorType = i.IndicatorType
                })
                .ToListAsync(ct);

            var response = new PagedResult<IndicatorDto>
            {
                Data = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Ok(response);
        }

        /// <summary>
        /// Get a single indicator by ID
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(IndicatorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id, CancellationToken ct)
        {
            var indicator = await _db.Indicators
                .AsNoTracking()
                .Where(i => i.Id == id && !i.IsDeleted)
                .Select(i => new IndicatorDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Code = i.Code,
                    Unit = i.Unit,
                    IndicatorType = i.IndicatorType
                })
                .FirstOrDefaultAsync(ct);

            if (indicator == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = $"Indicator with ID {id} was not found."
                });
            }

            return Ok(indicator);
        }

        /// <summary>
        /// Create a new indicator
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(IndicatorDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateIndicatorRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var indicator = new Indicator
            {
                Name = request.Name,
                Code = request.Code,
                Unit = request.Unit,
                IndicatorType = request.IndicatorType
            };

            _db.Indicators.Add(indicator);
            await _db.SaveChangesAsync(ct);

            var dto = new IndicatorDto
            {
                Id = indicator.Id,
                Name = indicator.Name,
                Code = indicator.Code,
                Unit = indicator.Unit,
                IndicatorType = indicator.IndicatorType
            };

            return CreatedAtAction(nameof(GetById), new { id = indicator.Id }, dto);
        }

        /// <summary>
        /// Update an existing indicator
        /// </summary>
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateIndicatorRequest request, CancellationToken ct)
        {
            var indicator = await _db.Indicators.FindAsync(new object[] { id }, ct);
            if (indicator == null || indicator.IsDeleted)
            {
                return NotFound();
            }

            indicator.Name = request.Name;
            indicator.Code = request.Code;
            indicator.Unit = request.Unit;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        /// <summary>
        /// Soft delete an indicator
        /// </summary>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            var indicator = await _db.Indicators.FindAsync(new object[] { id }, ct);
            if (indicator == null || indicator.IsDeleted)
            {
                return NotFound();
            }

            indicator.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
```

## DTO Models

```csharp
namespace MyApp.DTOs
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class IndicatorFilterQuery
    {
        public string? Search { get; set; }
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 20;
    }

    public class CreateIndicatorRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Unit { get; set; }

        public string IndicatorType { get; set; } = "Collected";
    }

    public class UpdateIndicatorRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Unit { get; set; }
    }

    public class IndicatorDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string IndicatorType { get; set; } = string.Empty;
    }
}
```
