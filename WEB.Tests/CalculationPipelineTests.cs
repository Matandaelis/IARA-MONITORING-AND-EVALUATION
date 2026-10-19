using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class CalculationPipelineTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static Indicator CreateIndicator(Guid id, string code, IndicatorType type, List<Token> tokens = null) => new Indicator
        {
            IndicatorId = id,
            Code = code,
            Name = code,
            Prefix = "",
            Suffix = "",
            Units = "",
            IndicatorType = type,
            Tokens = tokens ?? new List<Token>()
        };

        [Fact]
        public async Task GetTokenStacksToCalculateAsync_ChainedCalculatedIndicators_DiscoversAllLevels()
        {
            using var db = CreateInMemoryDbContext();

            // Chain: indA (collected) -> indB (calculated) -> indC (calculated) -> indD (calculated)
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var idC = Guid.NewGuid();
            var idD = Guid.NewGuid();

            var indA = CreateIndicator(idA, "IND_A", IndicatorType.Collected);
            var indB = CreateIndicator(idB, "IND_B", IndicatorType.Calculated, new List<Token>
            {
                new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = idA },
                new Token { TokenNumber = 2, TokenType = TokenType.Operator, OperatorType = OperatorType.Add },
                new Token { TokenNumber = 3, TokenType = TokenType.Number, Number = 1 }
            });
            var indC = CreateIndicator(idC, "IND_C", IndicatorType.Calculated, new List<Token>
            {
                new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = idB },
                new Token { TokenNumber = 2, TokenType = TokenType.Operator, OperatorType = OperatorType.Multiply },
                new Token { TokenNumber = 3, TokenType = TokenType.Number, Number = 2 }
            });
            var indD = CreateIndicator(idD, "IND_D", IndicatorType.Calculated, new List<Token>
            {
                new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = idC },
                new Token { TokenNumber = 2, TokenType = TokenType.Operator, OperatorType = OperatorType.Subtract },
                new Token { TokenNumber = 3, TokenType = TokenType.Number, Number = 5 }
            });

            db.Indicators.AddRange(indA, indB, indC, indD);
            await db.SaveChangesAsync();

            var calculation = new Calculation(db, null, Guid.NewGuid());
            var levels = await calculation.GetTokenStacksToCalculateAsync(new[] { indA.IndicatorId });

            // Must discover all 3 chained calculated indicators across 3 sequential levels
            Assert.Equal(3, levels.Count);

            // Level 1: indB
            Assert.True(levels[0].ContainsKey(indB.IndicatorId));
            Assert.Equal(3, levels[0][indB.IndicatorId].Count);

            // Level 2: indC
            Assert.True(levels[1].ContainsKey(indC.IndicatorId));
            Assert.Equal(3, levels[1][indC.IndicatorId].Count);

            // Level 3: indD
            Assert.True(levels[2].ContainsKey(indD.IndicatorId));
            Assert.Equal(3, levels[2][indD.IndicatorId].Count);
        }

        [Fact]
        public async Task GetTokenStacksToCalculateAsync_DiamondGraph_DiscoversAndGroupsCorrectly()
        {
            using var db = CreateInMemoryDbContext();

            // Diamond:
            //       indA (collected)
            //      /     \
            //   indB     indC (both calculated from indA)
            //      \     /
            //       indD (calculated from indB and indC)
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var idC = Guid.NewGuid();
            var idD = Guid.NewGuid();

            var indA = CreateIndicator(idA, "IND_A", IndicatorType.Collected);
            var indB = CreateIndicator(idB, "IND_B", IndicatorType.Calculated, new List<Token>
            {
                new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = idA }
            });
            var indC = CreateIndicator(idC, "IND_C", IndicatorType.Calculated, new List<Token>
            {
                new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = idA }
            });
            var indD = CreateIndicator(idD, "IND_D", IndicatorType.Calculated, new List<Token>
            {
                new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = idB },
                new Token { TokenNumber = 2, TokenType = TokenType.Operator, OperatorType = OperatorType.Add },
                new Token { TokenNumber = 3, TokenType = TokenType.Indicator, SourceIndicatorId = idC }
            });

            db.Indicators.AddRange(indA, indB, indC, indD);
            await db.SaveChangesAsync();

            var calculation = new Calculation(db, null, Guid.NewGuid());
            var levels = await calculation.GetTokenStacksToCalculateAsync(new[] { idA });

            Assert.Equal(2, levels.Count);

            // Level 0 should contain both B and C
            Assert.Equal(2, levels[0].Count);
            Assert.True(levels[0].ContainsKey(idB));
            Assert.True(levels[0].ContainsKey(idC));

            // Level 1 should contain D
            Assert.Single(levels[1]);
            Assert.True(levels[1].ContainsKey(idD));
        }

        [Fact]
        public async Task GetTokenStacksToCalculateAsync_WithMemoryCache_ServesFromCache()
        {
            using var db = CreateInMemoryDbContext();

            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();

            var indA = CreateIndicator(idA, "IND_A", IndicatorType.Collected);
            var indB = CreateIndicator(idB, "IND_B", IndicatorType.Calculated, new List<Token>
            {
                new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = idA }
            });

            db.Indicators.AddRange(indA, indB);
            await db.SaveChangesAsync();

            var cache = new MemoryCache(new MemoryCacheOptions());
            var calculation = new Calculation(db, null, Guid.NewGuid(), cache);

            var levels1 = await calculation.GetTokenStacksToCalculateAsync(new[] { idA });
            Assert.Single(levels1);
            Assert.True(levels1[0].ContainsKey(idB));

            // Second call with cache
            var levels2 = await calculation.GetTokenStacksToCalculateAsync(new[] { idA });
            Assert.Single(levels2);
            Assert.True(levels2[0].ContainsKey(idB));
        }

        [Fact]
        public async Task GetTokenStacksToCalculateAsync_UnrelatedIndicators_ReturnsEmpty()
        {
            using var db = CreateInMemoryDbContext();

            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();

            var indA = CreateIndicator(idA, "IND_A", IndicatorType.Collected);
            var indB = CreateIndicator(idB, "IND_B", IndicatorType.Calculated, new List<Token>
            {
                new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = Guid.NewGuid() }
            });

            db.Indicators.AddRange(indA, indB);
            await db.SaveChangesAsync();

            var calculation = new Calculation(db, null, Guid.NewGuid());
            var levels = await calculation.GetTokenStacksToCalculateAsync(new[] { idA });

            Assert.Empty(levels);
        }
    }
}
