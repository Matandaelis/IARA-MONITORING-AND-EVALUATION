using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class FormIndicatorContributionEngineTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task ProcessResponseContributionsAsync_GeneratesIndicatorContributions_And_DatumRollup()
        {
            using var db = CreateInMemoryDbContext();

            var entityId = Guid.NewGuid();
            var dateId = Guid.NewGuid();
            var questionnaireId = Guid.NewGuid();
            var indicatorId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var responseId = Guid.NewGuid();

            var questionnaire = new Questionnaire
            {
                QuestionnaireId = questionnaireId,
                Name = "Health Service Survey",
                CompletionText = "Thank you"
            };
            db.Questionnaires.Add(questionnaire);

            var indicator = new Indicator
            {
                IndicatorId = indicatorId,
                Code = "IND_01",
                Name = "Patients Treated",
                IndicatorType = IndicatorType.Collected,
                Prefix = "",
                Suffix = "",
                Units = "Patients"
            };
            db.Indicators.Add(indicator);

            var question = new Question
            {
                QuestionId = questionId,
                Code = "Q_PATIENTS",
                Text = "Number of patients treated",
                QuestionType = QuestionType.Number
            };
            db.Questions.Add(question);

            var mapping = new FormIndicatorMapping
            {
                FormIndicatorMappingId = Guid.NewGuid(),
                QuestionnaireId = questionnaireId,
                IndicatorId = indicatorId,
                Aggregation = MappingAggregation.Sum,
                CountOnlySubmitted = true,
                IsActive = true
            };
            mapping.QuestionLinks.Add(new QuestionIndicatorLink
            {
                QuestionIndicatorLinkId = Guid.NewGuid(),
                FormIndicatorMappingId = mapping.FormIndicatorMappingId,
                QuestionId = questionId,
                Role = QuestionIndicatorRole.Value,
                SortOrder = 1
            });
            db.FormIndicatorMappings.Add(mapping);

            var response = new Response
            {
                ResponseId = responseId,
                QuestionnaireId = questionnaireId,
                EntityId = entityId,
                DateId = dateId,
                SubmittedOnUtc = DateTime.UtcNow,
                CreatedOnUtc = DateTime.UtcNow
            };
            var answer = new Answer
            {
                AnswerId = Guid.NewGuid(),
                ResponseId = responseId,
                QuestionId = questionId,
                Value = "42",
                ValueNumber = 42m
            };
            response.Answers.Add(answer);
            db.Responses.Add(response);

            await db.SaveChangesAsync();

            var contributions = await FormIndicatorContributionEngine.ProcessResponseContributionsAsync(
                db, responseId, null, null);

            Assert.NotEmpty(contributions);
            Assert.Contains(contributions, c => c.IndicatorId == indicatorId && c.Value == 42m);

            var datum = await db.Data.FirstOrDefaultAsync(d => d.IndicatorId == indicatorId && d.EntityId == entityId && d.DateId == dateId);
            Assert.NotNull(datum);
            Assert.Equal(42m, datum.Value);
            Assert.Equal(DataSourceType.Questionnaire, datum.SourceType);
        }

        [Fact]
        public async Task ProcessResponseContributionsAsync_HandlesDisaggregation_CalculatesOptionAndTotalRows()
        {
            using var db = CreateInMemoryDbContext();

            var entityId = Guid.NewGuid();
            var dateId = Guid.NewGuid();
            var questionnaireId = Guid.NewGuid();
            var indicatorId = Guid.NewGuid();
            var qMaleId = Guid.NewGuid();
            var qFemaleId = Guid.NewGuid();
            var optMaleId = Guid.NewGuid();
            var optFemaleId = Guid.NewGuid();
            var responseId = Guid.NewGuid();

            var questionnaire = new Questionnaire
            {
                QuestionnaireId = questionnaireId,
                Name = "Immunization Registry",
                CompletionText = "Done"
            };
            db.Questionnaires.Add(questionnaire);

            var indicator = new Indicator
            {
                IndicatorId = indicatorId,
                Code = "IMM_01",
                Name = "Children Immunized",
                Prefix = "",
                Suffix = "",
                Units = "Children"
            };
            db.Indicators.Add(indicator);

            var qMale = new Question { QuestionId = qMaleId, Code = "Q_MALE", Text = "Male count", QuestionType = QuestionType.Number };
            var qFemale = new Question { QuestionId = qFemaleId, Code = "Q_FEMALE", Text = "Female count", QuestionType = QuestionType.Number };
            db.Questions.AddRange(qMale, qFemale);

            var mapping = new FormIndicatorMapping
            {
                FormIndicatorMappingId = Guid.NewGuid(),
                QuestionnaireId = questionnaireId,
                IndicatorId = indicatorId,
                Aggregation = MappingAggregation.Sum,
                CountOnlySubmitted = true,
                IsActive = true
            };
            mapping.QuestionLinks.Add(new QuestionIndicatorLink
            {
                QuestionIndicatorLinkId = Guid.NewGuid(),
                FormIndicatorMappingId = mapping.FormIndicatorMappingId,
                QuestionId = qMaleId,
                Role = QuestionIndicatorRole.Disaggregation,
                DisaggregationOptionId = optMaleId,
                SortOrder = 1
            });
            mapping.QuestionLinks.Add(new QuestionIndicatorLink
            {
                QuestionIndicatorLinkId = Guid.NewGuid(),
                FormIndicatorMappingId = mapping.FormIndicatorMappingId,
                QuestionId = qFemaleId,
                Role = QuestionIndicatorRole.Disaggregation,
                DisaggregationOptionId = optFemaleId,
                SortOrder = 2
            });
            db.FormIndicatorMappings.Add(mapping);

            var response = new Response
            {
                ResponseId = responseId,
                QuestionnaireId = questionnaireId,
                EntityId = entityId,
                DateId = dateId,
                SubmittedOnUtc = DateTime.UtcNow,
                CreatedOnUtc = DateTime.UtcNow
            };
            response.Answers.Add(new Answer { AnswerId = Guid.NewGuid(), ResponseId = responseId, QuestionId = qMaleId, Value = "15", ValueNumber = 15m });
            response.Answers.Add(new Answer { AnswerId = Guid.NewGuid(), ResponseId = responseId, QuestionId = qFemaleId, Value = "25", ValueNumber = 25m });
            db.Responses.Add(response);

            await db.SaveChangesAsync();

            var contributions = await FormIndicatorContributionEngine.ProcessResponseContributionsAsync(
                db, responseId, null, null);

            Assert.Equal(3, contributions.Count); // 2 option rows + 1 total row

            var datum = await db.Data.FirstOrDefaultAsync(d => d.IndicatorId == indicatorId && d.EntityId == entityId && d.DateId == dateId);
            Assert.NotNull(datum);
            Assert.Equal(40m, datum.Value); // 15 + 25 = 40

            var maleDisagg = await db.DisaggregationData.FirstOrDefaultAsync(dd => dd.IndicatorId == indicatorId && dd.DisaggregationOptionId == optMaleId);
            var femaleDisagg = await db.DisaggregationData.FirstOrDefaultAsync(dd => dd.IndicatorId == indicatorId && dd.DisaggregationOptionId == optFemaleId);

            Assert.NotNull(maleDisagg);
            Assert.Equal(15m, maleDisagg.Value);
            Assert.NotNull(femaleDisagg);
            Assert.Equal(25m, femaleDisagg.Value);
        }

        [Fact]
        public async Task ProcessResponseContributionsAsync_IgnoresUnsubmitted_WhenCountOnlySubmittedIsTrue()
        {
            using var db = CreateInMemoryDbContext();

            var questionnaireId = Guid.NewGuid();
            var indicatorId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var responseId = Guid.NewGuid();

            var mapping = new FormIndicatorMapping
            {
                FormIndicatorMappingId = Guid.NewGuid(),
                QuestionnaireId = questionnaireId,
                IndicatorId = indicatorId,
                Aggregation = MappingAggregation.Sum,
                CountOnlySubmitted = true,
                IsActive = true
            };
            mapping.QuestionLinks.Add(new QuestionIndicatorLink
            {
                QuestionIndicatorLinkId = Guid.NewGuid(),
                FormIndicatorMappingId = mapping.FormIndicatorMappingId,
                QuestionId = questionId,
                Role = QuestionIndicatorRole.Value,
                SortOrder = 1
            });
            db.FormIndicatorMappings.Add(mapping);

            var response = new Response
            {
                ResponseId = responseId,
                QuestionnaireId = questionnaireId,
                EntityId = Guid.NewGuid(),
                DateId = Guid.NewGuid(),
                SubmittedOnUtc = null, // unsubmitted draft
                CreatedOnUtc = DateTime.UtcNow
            };
            response.Answers.Add(new Answer { AnswerId = Guid.NewGuid(), ResponseId = responseId, QuestionId = questionId, Value = "100", ValueNumber = 100m });
            db.Responses.Add(response);

            await db.SaveChangesAsync();

            var contributions = await FormIndicatorContributionEngine.ProcessResponseContributionsAsync(
                db, responseId, null, null);

            Assert.Empty(contributions);
        }
    }
}
