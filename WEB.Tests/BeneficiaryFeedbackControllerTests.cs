using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Controllers;
using Monic.Web.Models;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Monic.Web.Tests
{
    public class BeneficiaryFeedbackControllerTests : IDisposable
    {
        private readonly ApplicationDbContext db;
        private readonly SimpleDbContextFactory factory;

        private class SimpleDbContextFactory : IDbContextFactory<ApplicationDbContext>
        {
            private readonly DbContextOptions<ApplicationDbContext> options;
            public SimpleDbContextFactory(DbContextOptions<ApplicationDbContext> options) => this.options = options;
            public ApplicationDbContext CreateDbContext() => new ApplicationDbContext(options);
        }

        public BeneficiaryFeedbackControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"cfm-test-{Guid.NewGuid()}")
                .Options;

            factory = new SimpleDbContextFactory(options);
            db = factory.CreateDbContext();
        }

        private BeneficiaryFeedbackController CreateController()
        {
            var controller = new BeneficiaryFeedbackController(factory, null, new AppSettings());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        public void Dispose()
        {
            db.Database.EnsureDeleted();
            db.Dispose();
        }

        [Fact]
        public async Task Create_GeneratesTrackingCode_AndSetsReceivedStatus()
        {
            var project = new Project { ProjectId = Guid.NewGuid(), Name = "WASH Clean Water" };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var dto = new BeneficiaryFeedbackDTO
            {
                ProjectId = project.ProjectId,
                Channel = "SMS Hotline",
                Category = "Quality of Service",
                Severity = FeedbackSeverity.Medium,
                Description = "Water pump in Zone 3 has low pressure.",
                IsAnonymous = false,
                ContactDetails = "+254700000000"
            };

            var result = await controller.Create(dto) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);

            var created = Assert.IsType<BeneficiaryFeedbackDTO>(result.Value);
            Assert.StartsWith("CFM-", created.TrackingCode);
            Assert.Equal(FeedbackStatus.Received, created.Status);
            Assert.Equal("SMS Hotline", created.Channel);

            // Assert saved in DB
            var inDb = await db.BeneficiaryFeedbacks.FirstOrDefaultAsync(o => o.Id == created.Id);
            Assert.NotNull(inDb);
            Assert.Equal(created.TrackingCode, inDb.TrackingCode);
        }

        [Fact]
        public async Task Create_CriticalSeverity_AutoTriggersCorrectiveActionPlan()
        {
            var project = new Project { ProjectId = Guid.NewGuid(), Name = "Food Assistance" };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var dto = new BeneficiaryFeedbackDTO
            {
                ProjectId = project.ProjectId,
                Channel = "Community Meeting",
                Category = "Staff Conduct / Safeguarding",
                Severity = FeedbackSeverity.Critical,
                Description = "Aid distribution diverted by local leaders.",
                IsAnonymous = true
            };

            var result = await controller.Create(dto) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);

            // Verify that a Corrective Action Plan (MonitoringAction) was created in DB
            var cap = await db.MonitoringActions.FirstOrDefaultAsync(o => o.ProjectId == project.ProjectId && o.TriggerSource == "CFM");
            Assert.NotNull(cap);
            Assert.Contains("Critical Beneficiary Complaint", cap.Description);
            Assert.Equal(MonitoringActionStatus.Open, cap.Status);
            Assert.NotNull(cap.DueDate);
            // 3-day SLA for Critical
            Assert.True(cap.DueDate.Value <= DateTime.UtcNow.AddDays(4));
        }

        [Fact]
        public async Task Update_StatusToResolved_RecordsResolvedAtUtc()
        {
            var project = new Project { ProjectId = Guid.NewGuid(), Name = "Health Nutrition" };
            db.Projects.Add(project);

            var feedback = new BeneficiaryFeedback
            {
                Id = Guid.NewGuid(),
                ProjectId = project.ProjectId,
                TrackingCode = "CFM-20261024-TEST01",
                Channel = "Help Desk",
                Category = "Resource Allocation",
                Severity = FeedbackSeverity.Low,
                Description = "Clinic wait times are long.",
                Status = FeedbackStatus.Investigating
            };
            db.BeneficiaryFeedbacks.Add(feedback);
            await db.SaveChangesAsync();

            var controller = CreateController();
            var updateDto = new BeneficiaryFeedbackDTO
            {
                ProjectId = project.ProjectId,
                Channel = feedback.Channel,
                Category = feedback.Category,
                Severity = feedback.Severity,
                Description = feedback.Description,
                Status = FeedbackStatus.Resolved,
                ResolutionNotes = "Added additional triage nurse to morning shift."
            };

            var result = await controller.Update(feedback.Id, updateDto) as OkObjectResult;
            Assert.NotNull(result);

            var updated = Assert.IsType<BeneficiaryFeedbackDTO>(result.Value);
            Assert.Equal(FeedbackStatus.Resolved, updated.Status);
            Assert.NotNull(updated.ResolvedAtUtc);
            Assert.Equal("Added additional triage nurse to morning shift.", updated.ResolutionNotes);
        }

        [Fact]
        public async Task GetStats_CalculatesAggregatesCorrectly()
        {
            var project = new Project { ProjectId = Guid.NewGuid(), Name = "Multi-sector" };
            db.Projects.Add(project);

            db.BeneficiaryFeedbacks.AddRange(
                new BeneficiaryFeedback { Id = Guid.NewGuid(), ProjectId = project.ProjectId, TrackingCode = "C1", Status = FeedbackStatus.Received, Severity = FeedbackSeverity.Low, Description = "D1" },
                new BeneficiaryFeedback { Id = Guid.NewGuid(), ProjectId = project.ProjectId, TrackingCode = "C2", Status = FeedbackStatus.Investigating, Severity = FeedbackSeverity.Medium, Description = "D2" },
                new BeneficiaryFeedback { Id = Guid.NewGuid(), ProjectId = project.ProjectId, TrackingCode = "C3", Status = FeedbackStatus.Resolved, Severity = FeedbackSeverity.High, Description = "D3" },
                new BeneficiaryFeedback { Id = Guid.NewGuid(), ProjectId = project.ProjectId, TrackingCode = "C4", Status = FeedbackStatus.Closed, Severity = FeedbackSeverity.Critical, Description = "D4" }
            );
            await db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.GetStats(project.ProjectId) as OkObjectResult;
            Assert.NotNull(result);

            dynamic stats = result.Value;
            Assert.Equal(4, (int)stats.Total);
            Assert.Equal(1, (int)stats.Received);
            Assert.Equal(1, (int)stats.Investigating);
            Assert.Equal(1, (int)stats.Resolved);
            Assert.Equal(1, (int)stats.Closed);
            Assert.Equal(1, (int)stats.Critical);
            Assert.Equal(1, (int)stats.High);
        }
    }
}
