using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class RecordsImportTests : IDisposable
    {
        private readonly ApplicationDbContext db;
        private readonly Guid questionnaireId = Guid.NewGuid();
        private readonly Guid entityTypeId = Guid.NewGuid();
        private readonly Guid entityTypeIdOther = Guid.NewGuid();
        private readonly Guid entityAId = Guid.NewGuid();
        private readonly Guid dateJanId = Guid.NewGuid();
        private readonly Guid optionYesId = Guid.NewGuid();
        private readonly Guid optionNoId = Guid.NewGuid();

        public RecordsImportTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"records-import-{Guid.NewGuid()}")
                .Options;

            db = new ApplicationDbContext(options);

            var sectionId = Guid.NewGuid();
            var optionGroupId = Guid.NewGuid();

            db.Questionnaires.Add(new Questionnaire
            {
                QuestionnaireId = questionnaireId,
                Name = "Beneficiary Survey",
                EntityTypeId = entityTypeId,
                UseSubmit = true,
                CreationText = "Start",
                CompletionText = "Done",
                Sections = { new Section { SectionId = sectionId, QuestionnaireId = questionnaireId, Name = "Main", SortOrder = 1 } }
            });

            db.Questions.Add(new Question
            {
                QuestionId = Guid.NewGuid(),
                SectionId = sectionId,
                Code = "FULL_NAME",
                Text = "Full name",
                Explanation = "",
                QuestionType = QuestionType.Text,
                Required = true,
                SortOrder = 1
            });
            db.Questions.Add(new Question
            {
                QuestionId = Guid.NewGuid(),
                SectionId = sectionId,
                Code = "SATISFIED",
                Text = "Satisfied?",
                Explanation = "",
                QuestionType = QuestionType.OptionList,
                QuestionOptionGroupId = optionGroupId,
                Required = true,
                SortOrder = 2
            });
            db.Questions.Add(new Question
            {
                QuestionId = Guid.NewGuid(),
                SectionId = sectionId,
                Code = "PHOTO",
                Text = "Photo",
                Explanation = "",
                QuestionType = QuestionType.Document,
                SortOrder = 3
            });

            db.QuestionOptions.Add(new QuestionOption { QuestionOptionId = optionYesId, QuestionOptionGroupId = optionGroupId, Label = "Yes", Color = "", SortOrder = 1 });
            db.QuestionOptions.Add(new QuestionOption { QuestionOptionId = optionNoId, QuestionOptionGroupId = optionGroupId, Label = "No", Color = "", SortOrder = 2 });

            db.Entities.Add(new Entity { EntityId = entityAId, Name = "Dakar Office", ShortName = "DKR", Code = "DKR", EntityTypeId = entityTypeId });
            db.Entities.Add(new Entity { EntityId = Guid.NewGuid(), Name = "Wrong Type Office", ShortName = "WRONG", Code = "WRONG", EntityTypeId = entityTypeIdOther });

            db.Dates.Add(new Date
            {
                DateId = dateJanId,
                Name = "Jan 2026",
                Code = "2026-01",
                DateType = DateType.Month,
                OpenFrom = new DateTime(2026, 1, 1),
                OpenTo = new DateTime(2026, 1, 31),
                SortOrder = 1
            });

            db.SaveChanges();
        }

        private static Dictionary<string, string> Row(params (string key, string value)[] cells)
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in cells) row[key] = value;
            return row;
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidateRecordsRowsAsync_ValidRow_Passes()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "DKR"), ("DateCode", "2026-01"), ("ResponseCode", "R1"), ("FULL_NAME", "Awa Diop"), ("SATISFIED", "Yes"))
            };

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows);

            Assert.Equal(1, report.ValidCount);
            Assert.Equal(0, report.ErrorCount);
            Assert.Empty(report.Rows[0].Errors);
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidateRecordsRowsAsync_UnknownEntityAndOption_FlagsErrors()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "MISSING"), ("DateCode", "2026-01"), ("SATISFIED", "Maybe"))
            };

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows);

            Assert.Equal(0, report.ValidCount);
            Assert.Equal(1, report.ErrorCount);
            Assert.Contains(report.Rows[0].Errors, e => e.Contains("Entity 'MISSING' was not found"));
            Assert.Contains(report.Rows[0].Errors, e => e.Contains("'Maybe' is not a valid option"));
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidateRecordsRowsAsync_WrongEntityTypeId_Rejected()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "WRONG"), ("DateCode", "2026-01"), ("FULL_NAME", "X"))
            };

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows);

            Assert.Equal(0, report.ValidCount);
            Assert.Contains(report.Rows[0].Errors, e => e.Contains("wrong type"));
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidateRecordsRowsAsync_DocumentQuestion_Rejected()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "DKR"), ("DateCode", "2026-01"), ("PHOTO", "photo.jpg"))
            };

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows);

            Assert.Equal(0, report.ValidCount);
            Assert.Contains(report.Rows[0].Errors, e => e.Contains("document question"));
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidateRecordsRowsAsync_DuplicateResponseCode_Rejected()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "DKR"), ("DateCode", "2026-01"), ("ResponseCode", "DUP"), ("FULL_NAME", "A")),
                Row(("EntityCode", "DKR"), ("DateCode", "2026-01"), ("ResponseCode", "DUP"), ("FULL_NAME", "B"))
            };

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows);

            Assert.Equal(1, report.ValidCount);
            Assert.Equal(1, report.ErrorCount);
            Assert.Contains(report.Rows[1].Errors, e => e.Contains("duplicated"));
        }

        [Fact]
        public async System.Threading.Tasks.Task CommitRecordsAsync_CreatesResponseAnswersAndProgress()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "DKR"), ("DateCode", "2026-01"), ("ResponseCode", "R-100"), ("FULL_NAME", "Awa Diop"), ("SATISFIED", "Yes"))
            };

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows);
            Assert.Equal(1, report.ValidCount);

            var importJob = new ImportJob { ImportType = ImportType.Records, TargetId = questionnaireId, FileName = "records-test.csv" };
            db.ImportJobs.Add(importJob);
            db.SaveChanges();

            await ImportProcessor.CommitRecordsAsync(db, questionnaireId, report, Guid.NewGuid(), submitAfterCommit: false);

            var response = await db.Responses
                .Include(o => o.Answers)
                .ThenInclude(o => o.AnswerOptions)
                .FirstAsync(o => o.QuestionnaireId == questionnaireId && o.ClientGeneratedId == "R-100");

            Assert.Equal(entityAId, response.EntityId);
            Assert.Equal(dateJanId, response.DateId);
            Assert.True(response.Answers.Count == 2, "answers: " + string.Join(",", response.Answers.Select(a => a.Question.Code)));

            var textAnswer = response.Answers.First(o => o.Question.Code == "FULL_NAME");
            Assert.Equal("Awa Diop", textAnswer.Value);

            var optionAnswer = response.Answers.First(o => o.Question.Code == "SATISFIED");
            Assert.Null(optionAnswer.Value); // canonical survey behaviour for option answers
            Assert.Single(optionAnswer.AnswerOptions);
            Assert.Equal(optionYesId, optionAnswer.AnswerOptions.First().QuestionOptionId);

            // progress was recalculated through the real engine:
            // TotalQuestions = all non-Note questions (incl. the unanswered PHOTO), applicable/completed = required + answered
            Assert.Equal(3, response.TotalQuestions);
            Assert.Equal(2, response.ApplicableQuestions);
            Assert.Equal(2, response.CompletedQuestions);
        }

        [Fact]
        public async System.Threading.Tasks.Task CommitRecordsAsync_SubmitAfterCommit_MarksSubmitted()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "DKR"), ("DateCode", "2026-01"), ("FULL_NAME", "Awa Diop"), ("SATISFIED", "No"))
            };

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows);

            await ImportProcessor.CommitRecordsAsync(db, questionnaireId, report, Guid.NewGuid(), submitAfterCommit: true);

            var response = await db.Responses.FirstAsync(o => o.QuestionnaireId == questionnaireId);
            Assert.NotNull(response.SubmittedOnUtc);
        }

        [Fact]
        public async System.Threading.Tasks.Task CommitRecordsAsync_ExistingResponseCode_UpdatesInsteadOfCreating()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "DKR"), ("DateCode", "2026-01"), ("ResponseCode", "R-200"), ("FULL_NAME", "First"))
            };

            var report = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows);
            await ImportProcessor.CommitRecordsAsync(db, questionnaireId, report, Guid.NewGuid(), submitAfterCommit: false);

            // second import, same ResponseCode → same response updated
            var rows2 = new List<Dictionary<string, string>>
            {
                Row(("EntityCode", "DKR"), ("DateCode", "2026-01"), ("ResponseCode", "R-200"), ("FULL_NAME", "Updated"))
            };
            var report2 = await ImportProcessor.ValidateRecordsRowsAsync(db, questionnaireId, rows2);
            await ImportProcessor.CommitRecordsAsync(db, questionnaireId, report2, Guid.NewGuid(), submitAfterCommit: false);

            var responses = await db.Responses.Where(o => o.QuestionnaireId == questionnaireId).ToListAsync();
            Assert.Single(responses);

            var answer = await db.Answers.FirstAsync(o => o.ResponseId == responses[0].ResponseId && o.Question.Code == "FULL_NAME");
            Assert.Equal("Updated", answer.Value);
        }

        public void Dispose()
        {
            db.Database.EnsureDeleted();
            db.Dispose();
        }
    }
}
