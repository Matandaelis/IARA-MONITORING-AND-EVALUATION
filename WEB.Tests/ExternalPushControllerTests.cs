using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Controllers;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class ExternalPushControllerTests : IDisposable
    {
        private readonly ApplicationDbContext db;
        private readonly SimpleDbContextFactory factory;
        private readonly string testApiKey = "kobo-test-api-key-12345";

        private class SimpleDbContextFactory : IDbContextFactory<ApplicationDbContext>
        {
            private readonly DbContextOptions<ApplicationDbContext> options;
            public SimpleDbContextFactory(DbContextOptions<ApplicationDbContext> options) => this.options = options;
            public ApplicationDbContext CreateDbContext() => new ApplicationDbContext(options);
        }

        public ExternalPushControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"kobo-test-{Guid.NewGuid()}")
                .Options;

            factory = new SimpleDbContextFactory(options);
            db = factory.CreateDbContext();

            db.Settings.Add(new Monic.Web.Models.Settings { ExternalApiKey = testApiKey });
            db.SaveChanges();
        }

        private ExternalPushController CreateController(string apiKeyHeader = null)
        {
            var controller = new ExternalPushController(factory);
            var httpContext = new DefaultHttpContext();
            if (!string.IsNullOrWhiteSpace(apiKeyHeader))
            {
                httpContext.Request.Headers["X-Api-Key"] = apiKeyHeader;
            }
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        public void Dispose()
        {
            db.Database.EnsureDeleted();
            db.Dispose();
        }

        [Fact]
        public async Task IngestKoboWebhook_ValidPayload_CreatesResponseAndAnswers()
        {
            var entityType = new EntityType { EntityTypeId = Guid.NewGuid(), Name = "Community Site", Plural = "Community Sites" };
            var entity = new Entity { EntityId = Guid.NewGuid(), EntityTypeId = entityType.EntityTypeId, Code = "SITE-01", Name = "Kilosa Village", ShortName = "Kilosa" };
            var date = new Date { DateId = Guid.NewGuid(), Code = "2026-Q1", Name = "2026-Q1", SortOrder = 1, OpenFrom = DateTime.UtcNow, OpenTo = DateTime.UtcNow.AddYears(1) };

            var questionnaire = new Questionnaire
            {
                QuestionnaireId = Guid.NewGuid(),
                Name = "Water Point Functionality Survey",
                EntityTypeId = entityType.EntityTypeId,
                CompletionText = "Thank you for completing this survey."
            };

            var section = new Section { SectionId = Guid.NewGuid(), QuestionnaireId = questionnaire.QuestionnaireId, Name = "General" };
            var q1 = new Question
            {
                QuestionId = Guid.NewGuid(),
                SectionId = section.SectionId,
                Code = "Q_PUMP_STATUS",
                Text = "Is the pump functional?",
                QuestionType = QuestionType.Text
            };
            var q2 = new Question
            {
                QuestionId = Guid.NewGuid(),
                SectionId = section.SectionId,
                Code = "Q_DAILY_VOL",
                Text = "Daily volume in cubic meters",
                QuestionType = QuestionType.Number
            };

            section.Questions.Add(q1);
            section.Questions.Add(q2);
            questionnaire.Sections.Add(section);

            db.EntityTypes.Add(entityType);
            db.Entities.Add(entity);
            db.Dates.Add(date);
            db.Questionnaires.Add(questionnaire);
            await db.SaveChangesAsync();

            var controller = CreateController(testApiKey);

            var koboJson = @"{
                ""_id"": 98765,
                ""entity_code"": ""SITE-01"",
                ""date_code"": ""2026-Q1"",
                ""Q_PUMP_STATUS"": ""Fully Operational"",
                ""Q_DAILY_VOL"": ""150""
            }";

            using var jsonDoc = JsonDocument.Parse(koboJson);
            var result = await controller.IngestKoboWebhook(questionnaire.QuestionnaireId, jsonDoc.RootElement) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            // Verify Response was saved in database
            var savedResponse = await db.Responses
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.ClientGeneratedId == "98765");

            Assert.NotNull(savedResponse);
            Assert.Equal(entity.EntityId, savedResponse.EntityId);
            Assert.Equal(date.DateId, savedResponse.DateId);
            Assert.Equal(2, savedResponse.Answers.Count);

            var ansPump = savedResponse.Answers.FirstOrDefault(a => a.QuestionId == q1.QuestionId);
            Assert.NotNull(ansPump);
            Assert.Equal("Fully Operational", ansPump.Value);

            var ansVol = savedResponse.Answers.FirstOrDefault(a => a.QuestionId == q2.QuestionId);
            Assert.NotNull(ansVol);
            Assert.Equal("150", ansVol.Value);
        }

        [Fact]
        public async Task IngestKoboWebhook_DuplicateSubmissionId_ReturnsAlreadyProcessedIdempotently()
        {
            var entityType = new EntityType { EntityTypeId = Guid.NewGuid(), Name = "Site", Plural = "Sites" };
            var entity = new Entity { EntityId = Guid.NewGuid(), EntityTypeId = entityType.EntityTypeId, Code = "SITE-DUP", Name = "Dup Site", ShortName = "SITE-DUP" };
            var date = new Date { DateId = Guid.NewGuid(), Code = "2026", Name = "2026", SortOrder = 1, OpenFrom = DateTime.UtcNow, OpenTo = DateTime.UtcNow.AddYears(1) };
            var questionnaire = new Questionnaire { QuestionnaireId = Guid.NewGuid(), Name = "Survey", EntityTypeId = entityType.EntityTypeId, CompletionText = "Thank you." };

            db.EntityTypes.Add(entityType);
            db.Entities.Add(entity);
            db.Dates.Add(date);
            db.Questionnaires.Add(questionnaire);

            // Pre-seed an existing response with ClientGeneratedId = "dup-12345"
            db.Responses.Add(new Response
            {
                ResponseId = Guid.NewGuid(),
                QuestionnaireId = questionnaire.QuestionnaireId,
                EntityId = entity.EntityId,
                DateId = date.DateId,
                ClientGeneratedId = "dup-12345",
                CreatedOnUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var controller = CreateController(testApiKey);
            var koboJson = @"{ ""_id"": ""dup-12345"", ""entity_code"": ""SITE-DUP"" }";
            using var doc = JsonDocument.Parse(koboJson);

            var result = await controller.IngestKoboWebhook(questionnaire.QuestionnaireId, doc.RootElement) as OkObjectResult;
            Assert.NotNull(result);

            var statusProp = result.Value.GetType().GetProperty("status")?.GetValue(result.Value)?.ToString();
            Assert.Equal("AlreadyProcessed", statusProp);
        }

        [Fact]
        public async Task OfflineSync_IngestsBatchOfOfflineSubmissions_AndAvoidsDuplicates()
        {
            var entityType = new EntityType { EntityTypeId = Guid.NewGuid(), Name = "Clinic", Plural = "Clinics" };
            var entity = new Entity { EntityId = Guid.NewGuid(), EntityTypeId = entityType.EntityTypeId, Code = "CLN-01", Name = "Clinic A", ShortName = "CLN-01" };
            var date = new Date { DateId = Guid.NewGuid(), Code = "2026-M01", Name = "2026-M01", SortOrder = 1, OpenFrom = DateTime.UtcNow, OpenTo = DateTime.UtcNow.AddYears(1) };
            var questionnaire = new Questionnaire { QuestionnaireId = Guid.NewGuid(), Name = "Health Visit", EntityTypeId = entityType.EntityTypeId, CompletionText = "Thank you." };
            var question = new Question { QuestionId = Guid.NewGuid(), Text = "Patients Treated", Code = "PAT_COUNT" };

            db.EntityTypes.Add(entityType);
            db.Entities.Add(entity);
            db.Dates.Add(date);
            db.Questionnaires.Add(questionnaire);
            db.Questions.Add(question);
            await db.SaveChangesAsync();

            var controller = CreateController(testApiKey);
            var clientDeviceId = Guid.NewGuid();

            var request = new OfflineSyncRequest
            {
                ClientDeviceId = clientDeviceId,
                Submissions = new List<OfflineSurveySubmission>
                {
                    new OfflineSurveySubmission
                    {
                        ClientGeneratedId = "OFFLINE-SUB-001",
                        QuestionnaireId = questionnaire.QuestionnaireId,
                        EntityCode = "CLN-01",
                        DateCode = "2026-M01",
                        CreatedOnUtc = DateTime.UtcNow.AddHours(-3),
                        SubmittedOnUtc = DateTime.UtcNow.AddHours(-1),
                        Answers = new List<OfflineAnswerItem>
                        {
                            new OfflineAnswerItem { QuestionId = question.QuestionId, Value = "42" }
                        }
                    },
                    new OfflineSurveySubmission
                    {
                        ClientGeneratedId = "OFFLINE-SUB-002",
                        QuestionnaireId = questionnaire.QuestionnaireId,
                        EntityCode = "CLN-01",
                        DateCode = "2026-M01",
                        CreatedOnUtc = DateTime.UtcNow.AddHours(-2),
                        SubmittedOnUtc = DateTime.UtcNow.AddHours(-1),
                        Answers = new List<OfflineAnswerItem>
                        {
                            new OfflineAnswerItem { QuestionId = question.QuestionId, Value = "58" }
                        }
                    }
                }
            };

            var result = await controller.OfflineSync(request) as OkObjectResult;
            Assert.NotNull(result);

            int synced = (int)(result.Value.GetType().GetProperty("synced")?.GetValue(result.Value) ?? 0);
            Assert.Equal(2, synced);

            // Verify Responses saved in database with ClientDeviceId
            var savedList = await db.Responses.Where(r => r.ClientDeviceId == clientDeviceId).ToListAsync();
            Assert.Equal(2, savedList.Count);

            // Re-sync the exact same batch: should report 2 duplicates and 0 synced
            var reSyncResult = await controller.OfflineSync(request) as OkObjectResult;
            Assert.NotNull(reSyncResult);
            int duplicateCount = (int)(reSyncResult.Value.GetType().GetProperty("duplicates")?.GetValue(reSyncResult.Value) ?? 0);
            int reSynced = (int)(reSyncResult.Value.GetType().GetProperty("synced")?.GetValue(reSyncResult.Value) ?? 0);
            Assert.Equal(2, duplicateCount);
            Assert.Equal(0, reSynced);
        }
    }
}
