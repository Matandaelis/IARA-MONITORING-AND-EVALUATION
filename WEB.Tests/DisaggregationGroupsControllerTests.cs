using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Controllers;
using Monic.Web.Models;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Monic.Web.Tests
{
    public class DisaggregationGroupsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext db;
        private readonly SimpleDbContextFactory factory;

        private class SimpleDbContextFactory : IDbContextFactory<ApplicationDbContext>
        {
            private readonly DbContextOptions<ApplicationDbContext> options;
            public SimpleDbContextFactory(DbContextOptions<ApplicationDbContext> options) => this.options = options;
            public ApplicationDbContext CreateDbContext() => new ApplicationDbContext(options);
        }

        public DisaggregationGroupsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"disaggregation-test-{Guid.NewGuid()}")
                .Options;

            factory = new SimpleDbContextFactory(options);
            db = factory.CreateDbContext();
        }

        private DisaggregationGroupsController CreateController() =>
            new DisaggregationGroupsController(factory, null, new AppSettings());

        public void Dispose()
        {
            db.Database.EnsureDeleted();
            db.Dispose();
        }

        [Fact]
        public async Task GetGroups_Default_ReturnsOnlyActiveGroups()
        {
            var active = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Gender", IsActive = true };
            var inactive = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Archived Category", IsActive = false };
            db.DisaggregationGroups.AddRange(active, inactive);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.GetGroups(includeInactive: false) as OkObjectResult;
            Assert.NotNull(result);
            var list = Assert.IsAssignableFrom<List<DisaggregationGroupDTO>>(result.Value);
            Assert.Single(list);
            Assert.Equal("Gender", list[0].Name);
        }

        [Fact]
        public async Task GetGroups_IncludeInactiveTrue_ReturnsAllGroups()
        {
            var active = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Gender", IsActive = true };
            var inactive = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Archived Category", IsActive = false };
            db.DisaggregationGroups.AddRange(active, inactive);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.GetGroups(includeInactive: true) as OkObjectResult;
            Assert.NotNull(result);
            var list = Assert.IsAssignableFrom<List<DisaggregationGroupDTO>>(result.Value);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetGroups_ReturnsOptionsOrderedBySortOrder()
        {
            var group = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Age Group", IsActive = true };
            // Add options out-of-order
            group.Options.Add(new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), Name = "36+", Code = "OLD", SortOrder = 3 });
            group.Options.Add(new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), Name = "0-17", Code = "YNG", SortOrder = 1 });
            group.Options.Add(new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), Name = "18-35", Code = "MID", SortOrder = 2 });
            db.DisaggregationGroups.Add(group);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.GetGroups() as OkObjectResult;
            Assert.NotNull(result);
            var list = Assert.IsAssignableFrom<List<DisaggregationGroupDTO>>(result.Value);
            var options = list[0].Options;
            Assert.Equal(3, options.Count);
            Assert.Equal("0-17", options[0].Name);
            Assert.Equal("18-35", options[1].Name);
            Assert.Equal("36+", options[2].Name);
        }

        [Fact]
        public async Task GetGroup_ExistingId_ReturnsGroupWithSortedOptions()
        {
            var group = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Residence", IsActive = true };
            group.Options.Add(new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), Name = "Rural", Code = "RUR", SortOrder = 2 });
            group.Options.Add(new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), Name = "Urban", Code = "URB", SortOrder = 1 });
            db.DisaggregationGroups.Add(group);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.GetGroup(group.DisaggregationGroupId) as OkObjectResult;
            Assert.NotNull(result);
            var dto = Assert.IsType<DisaggregationGroupDTO>(result.Value);
            Assert.Equal("Residence", dto.Name);
            Assert.Equal(2, dto.Options.Count);
            Assert.Equal("Urban", dto.Options[0].Name);
            Assert.Equal("Rural", dto.Options[1].Name);
        }

        [Fact]
        public async Task GetGroup_NonExistentId_ReturnsNotFound()
        {
            var controller = CreateController();
            var result = await controller.GetGroup(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateGroup_ValidDto_CreatesGroupAndOptionsWithNewGuidsAndSortOrder()
        {
            var dto = new DisaggregationGroupDTO
            {
                Name = "Facility Type",
                Description = "Primary, secondary or tertiary health facilities",
                IsActive = true,
                Options = new List<DisaggregationOptionDTO>
                {
                    new DisaggregationOptionDTO { Name = "Primary Clinic", Code = "PRI", SortOrder = 1 },
                    new DisaggregationOptionDTO { Name = "General Hospital", Code = "SEC", SortOrder = 2 }
                }
            };

            var controller = CreateController();
            var result = await controller.CreateGroup(dto) as CreatedAtActionResult;
            Assert.NotNull(result);
            var created = Assert.IsType<DisaggregationGroupDTO>(result.Value);
            Assert.NotEqual(Guid.Empty, created.DisaggregationGroupId);
            Assert.Equal(2, created.Options.Count);
            Assert.All(created.Options, o => Assert.NotEqual(Guid.Empty, o.DisaggregationOptionId));

            // Verify stored in DB using fresh context
            await using var verifyDb = factory.CreateDbContext();
            var stored = await verifyDb.DisaggregationGroups.Include(g => g.Options).FirstOrDefaultAsync(g => g.DisaggregationGroupId == created.DisaggregationGroupId);
            Assert.NotNull(stored);
            Assert.Equal("Facility Type", stored.Name);
            Assert.Equal(2, stored.Options.Count);
        }

        [Fact]
        public async Task CreateGroup_InvalidModelState_ReturnsBadRequest()
        {
            var controller = CreateController();
            controller.ModelState.AddModelError("Name", "Name is required");
            var result = await controller.CreateGroup(new DisaggregationGroupDTO());
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateGroup_ValidDto_UpdatesPropertiesAndSyncsOptions()
        {
            var group = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Old Name", IsActive = true };
            var opt1 = new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), DisaggregationGroupId = group.DisaggregationGroupId, Name = "Option 1", Code = "O1", SortOrder = 1 };
            var opt2 = new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), DisaggregationGroupId = group.DisaggregationGroupId, Name = "Option 2 (To Remove)", Code = "O2", SortOrder = 2 };
            group.Options.Add(opt1);
            group.Options.Add(opt2);
            db.DisaggregationGroups.Add(group);
            await db.SaveChangesAsync();

            var updateDto = new DisaggregationGroupDTO
            {
                DisaggregationGroupId = group.DisaggregationGroupId,
                Name = "New Name",
                Description = "Updated description",
                IsActive = true,
                Options = new List<DisaggregationOptionDTO>
                {
                    // Existing option 1 updated
                    new DisaggregationOptionDTO { DisaggregationOptionId = opt1.DisaggregationOptionId, Name = "Option 1 Updated", Code = "O1U", SortOrder = 1 },
                    // New option added (opt2 omitted, should be deleted)
                    new DisaggregationOptionDTO { DisaggregationOptionId = Guid.Empty, Name = "Option 3 New", Code = "O3", SortOrder = 2 }
                }
            };

            var controller = CreateController();
            var result = await controller.UpdateGroup(group.DisaggregationGroupId, updateDto) as OkObjectResult;
            Assert.NotNull(result);
            var updated = Assert.IsType<DisaggregationGroupDTO>(result.Value);
            Assert.Equal("New Name", updated.Name);

            await using var verifyDb = factory.CreateDbContext();
            var stored = await verifyDb.DisaggregationGroups.Include(g => g.Options).FirstOrDefaultAsync(g => g.DisaggregationGroupId == group.DisaggregationGroupId);
            Assert.NotNull(stored);
            Assert.Equal(2, stored.Options.Count);
            Assert.Contains(stored.Options, o => o.Name == "Option 1 Updated" && o.Code == "O1U");
            Assert.Contains(stored.Options, o => o.Name == "Option 3 New" && o.Code == "O3");
            Assert.DoesNotContain(stored.Options, o => o.Name == "Option 2 (To Remove)");
        }

        [Fact]
        public async Task DeleteGroup_LinkedToIndicator_SoftDeletesMarkingInactive()
        {
            var group = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Gender", IsActive = true };
            var indicator = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Name = "Patients Tested",
                Code = "PT_IND",
                Prefix = "",
                Suffix = "",
                Units = "pts",
                DisaggregationGroupId = group.DisaggregationGroupId
            };
            db.DisaggregationGroups.Add(group);
            db.Indicators.Add(indicator);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.DeleteGroup(group.DisaggregationGroupId) as OkObjectResult;
            Assert.NotNull(result);

            // Group should still exist in DB, but with IsActive = false
            await using var verifyDb = factory.CreateDbContext();
            var stored = await verifyDb.DisaggregationGroups.FindAsync(group.DisaggregationGroupId);
            Assert.NotNull(stored);
            Assert.False(stored.IsActive);
        }

        [Fact]
        public async Task DeleteGroup_WithOptionsHavingRecordedData_SoftDeletesMarkingInactive()
        {
            var group = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Setting", IsActive = true };
            var opt = new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), DisaggregationGroupId = group.DisaggregationGroupId, Name = "Inpatient", Code = "IN", SortOrder = 1 };
            group.Options.Add(opt);
            db.DisaggregationGroups.Add(group);

            // Add recorded data referencing this option
            var datum = new DisaggregationDatum
            {
                DisaggregationDatumId = Guid.NewGuid(),
                DisaggregationOptionId = opt.DisaggregationOptionId,
                Value = 42m
            };
            db.DisaggregationData.Add(datum);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.DeleteGroup(group.DisaggregationGroupId) as OkObjectResult;
            Assert.NotNull(result);

            // Group must not be deleted from DB to avoid FK violation; must be marked inactive
            await using var verifyDb = factory.CreateDbContext();
            var stored = await verifyDb.DisaggregationGroups.FindAsync(group.DisaggregationGroupId);
            Assert.NotNull(stored);
            Assert.False(stored.IsActive);
        }

        [Fact]
        public async Task DeleteGroup_NoLinksOrData_HardDeletesGroupAndOptions()
        {
            var group = new DisaggregationGroup { DisaggregationGroupId = Guid.NewGuid(), Name = "Unused Group", IsActive = true };
            group.Options.Add(new DisaggregationOption { DisaggregationOptionId = Guid.NewGuid(), Name = "Unused Opt", Code = "U1", SortOrder = 1 });
            db.DisaggregationGroups.Add(group);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.DeleteGroup(group.DisaggregationGroupId);
            Assert.IsType<NoContentResult>(result);

            // Should be completely removed from DB
            await using var verifyDb = factory.CreateDbContext();
            var stored = await verifyDb.DisaggregationGroups.FindAsync(group.DisaggregationGroupId);
            Assert.Null(stored);
        }
    }
}
