using System;
using System.Collections.Generic;
using System.Linq;
using Monic.Web.Code;
using Monic.Web.Models;
using Xunit;

namespace Monic.Web.Tests
{
    public class CalculationTests
    {
        [Fact]
        public void ToPostFixStack_SimpleAddition_ReturnsCorrectPostfixOrder()
        {
            var indA = Guid.NewGuid();
            var indB = Guid.NewGuid();

            var indicator = new Indicator
            {
                Code = "IND_ADD",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = indA },
                    new Token { TokenNumber = 2, TokenType = TokenType.Operator, OperatorType = OperatorType.Add },
                    new Token { TokenNumber = 3, TokenType = TokenType.Indicator, SourceIndicatorId = indB }
                }
            };

            var stack = Calculation.ToPostFixStack(indicator);
            Assert.NotNull(stack);

            var list = stack.ToList();
            Assert.Equal(3, list.Count);
            // Postfix order: A, B, +
            Assert.Equal(TokenType.Indicator, list[0].TokenType);
            Assert.Equal(indA, list[0].IndicatorId);

            Assert.Equal(TokenType.Indicator, list[1].TokenType);
            Assert.Equal(indB, list[1].IndicatorId);

            Assert.Equal(TokenType.Operator, list[2].TokenType);
            Assert.Equal(OperatorType.Add, list[2].OperatorType);
        }

        [Fact]
        public void ToPostFixStack_OperatorPrecedence_MultipliesBeforeAdd()
        {
            // A + B * C should evaluate as A B C * +
            var indA = Guid.NewGuid();
            var indB = Guid.NewGuid();
            var indC = Guid.NewGuid();

            var indicator = new Indicator
            {
                Code = "IND_PRECEDENCE",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenNumber = 1, TokenType = TokenType.Indicator, SourceIndicatorId = indA },
                    new Token { TokenNumber = 2, TokenType = TokenType.Operator, OperatorType = OperatorType.Add },
                    new Token { TokenNumber = 3, TokenType = TokenType.Indicator, SourceIndicatorId = indB },
                    new Token { TokenNumber = 4, TokenType = TokenType.Operator, OperatorType = OperatorType.Multiply },
                    new Token { TokenNumber = 5, TokenType = TokenType.Indicator, SourceIndicatorId = indC }
                }
            };

            var stack = Calculation.ToPostFixStack(indicator);
            var list = stack.ToList();

            Assert.Equal(5, list.Count);
            Assert.Equal(indA, list[0].IndicatorId);
            Assert.Equal(indB, list[1].IndicatorId);
            Assert.Equal(indC, list[2].IndicatorId);
            Assert.Equal(OperatorType.Multiply, list[3].OperatorType);
            Assert.Equal(OperatorType.Add, list[4].OperatorType);
        }

        [Fact]
        public void ToPostFixStack_ParenthesesOverride_EvaluatesGroupFirst()
        {
            // (A + B) * C should evaluate as A B + C *
            var indA = Guid.NewGuid();
            var indB = Guid.NewGuid();
            var indC = Guid.NewGuid();

            var indicator = new Indicator
            {
                Code = "IND_PAREN",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenNumber = 1, TokenType = TokenType.Parenthesis, ParenthesisType = ParenthesisType.Open },
                    new Token { TokenNumber = 2, TokenType = TokenType.Indicator, SourceIndicatorId = indA },
                    new Token { TokenNumber = 3, TokenType = TokenType.Operator, OperatorType = OperatorType.Add },
                    new Token { TokenNumber = 4, TokenType = TokenType.Indicator, SourceIndicatorId = indB },
                    new Token { TokenNumber = 5, TokenType = TokenType.Parenthesis, ParenthesisType = ParenthesisType.Close },
                    new Token { TokenNumber = 6, TokenType = TokenType.Operator, OperatorType = OperatorType.Multiply },
                    new Token { TokenNumber = 7, TokenType = TokenType.Indicator, SourceIndicatorId = indC }
                }
            };

            var stack = Calculation.ToPostFixStack(indicator);
            var list = stack.ToList();

            Assert.Equal(5, list.Count);
            Assert.Equal(indA, list[0].IndicatorId);
            Assert.Equal(indB, list[1].IndicatorId);
            Assert.Equal(OperatorType.Add, list[2].OperatorType);
            Assert.Equal(indC, list[3].IndicatorId);
            Assert.Equal(OperatorType.Multiply, list[4].OperatorType);
        }

        [Fact]
        public void ToPostFixStack_UnbalancedParenthesis_ThrowsArgumentException()
        {
            var indA = Guid.NewGuid();

            var indicator = new Indicator
            {
                Code = "IND_ERR",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenNumber = 1, TokenType = TokenType.Parenthesis, ParenthesisType = ParenthesisType.Open },
                    new Token { TokenNumber = 2, TokenType = TokenType.Indicator, SourceIndicatorId = indA }
                }
            };

            Assert.Throws<ArgumentException>(() => Calculation.ToPostFixStack(indicator));
        }

        [Fact]
        public void GetOperator_ValidSymbols_ReturnsExpectedOperator()
        {
            Assert.Equal(OperatorType.Add, Calculation.GetOperator("+"));
            Assert.Equal(OperatorType.Subtract, Calculation.GetOperator("-"));
            Assert.Equal(OperatorType.Multiply, Calculation.GetOperator("*"));
            Assert.Equal(OperatorType.Divide, Calculation.GetOperator("/"));
            Assert.Throws<NotImplementedException>(() => Calculation.GetOperator("%"));
        }

        [Fact]
        public void GetParenthesis_ValidSymbols_ReturnsExpectedParenthesis()
        {
            Assert.Equal(ParenthesisType.Open, Calculation.GetParenthesis("("));
            Assert.Equal(ParenthesisType.Close, Calculation.GetParenthesis(")"));
            Assert.Throws<NotImplementedException>(() => Calculation.GetParenthesis("["));
        }

        [Fact]
        public void OrderTopologicalLevels_IndependentIndicators_ReturnsSingleLevel()
        {
            var ind1 = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "IND_1",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>()
            };
            var ind2 = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "IND_2",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>()
            };

            var levels = Calculation.OrderTopologicalLevels(new[] { ind1, ind2 });

            Assert.Single(levels);
            Assert.Equal(2, levels[0].Count);
        }

        [Fact]
        public void OrderTopologicalLevels_LinearChain_ReturnsSequentialLevels()
        {
            // Chain: ind1 -> ind2 -> ind3
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();

            var ind1 = new Indicator
            {
                IndicatorId = id1,
                Code = "IND_1",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>()
            };
            var ind2 = new Indicator
            {
                IndicatorId = id2,
                Code = "IND_2",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = id1 }
                }
            };
            var ind3 = new Indicator
            {
                IndicatorId = id3,
                Code = "IND_3",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = id2 }
                }
            };

            var levels = Calculation.OrderTopologicalLevels(new[] { ind3, ind1, ind2 });

            Assert.Equal(3, levels.Count);
            Assert.Equal(id1, levels[0][0].IndicatorId);
            Assert.Equal(id2, levels[1][0].IndicatorId);
            Assert.Equal(id3, levels[2][0].IndicatorId);
        }

        [Fact]
        public void OrderTopologicalLevels_DiamondDependency_ReturnsExpectedLevels()
        {
            // Diamond:
            //       ind1
            //      /    \
            //    ind2   ind3
            //      \    /
            //       ind4
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var id4 = Guid.NewGuid();

            var ind1 = new Indicator
            {
                IndicatorId = id1,
                Code = "IND_ROOT",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>()
            };
            var ind2 = new Indicator
            {
                IndicatorId = id2,
                Code = "IND_LEFT",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = id1 }
                }
            };
            var ind3 = new Indicator
            {
                IndicatorId = id3,
                Code = "IND_RIGHT",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = id1 }
                }
            };
            var ind4 = new Indicator
            {
                IndicatorId = id4,
                Code = "IND_JOIN",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = id2 },
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = id3 }
                }
            };

            var levels = Calculation.OrderTopologicalLevels(new[] { ind4, ind3, ind2, ind1 });

            Assert.Equal(3, levels.Count);
            Assert.Single(levels[0]);
            Assert.Equal(id1, levels[0][0].IndicatorId);

            Assert.Equal(2, levels[1].Count);
            var level1Ids = levels[1].Select(i => i.IndicatorId).ToHashSet();
            Assert.Contains(id2, level1Ids);
            Assert.Contains(id3, level1Ids);

            Assert.Single(levels[2]);
            Assert.Equal(id4, levels[2][0].IndicatorId);
        }

        [Fact]
        public void OrderTopologicalLevels_CircularDependency_ThrowsInvalidOperationException()
        {
            // Cycle: A -> B -> A
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();

            var indA = new Indicator
            {
                IndicatorId = idA,
                Code = "IND_A",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = idB }
                }
            };
            var indB = new Indicator
            {
                IndicatorId = idB,
                Code = "IND_B",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = idA }
                }
            };

            var ex = Assert.Throws<InvalidOperationException>(() => Calculation.OrderTopologicalLevels(new[] { indA, indB }));
            Assert.Contains("Circular dependency detected", ex.Message);
        }

        [Fact]
        public void OrderTopologicalLevels_SelfReference_ThrowsInvalidOperationException()
        {
            var idA = Guid.NewGuid();
            var indA = new Indicator
            {
                IndicatorId = idA,
                Code = "IND_SELF",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenType = TokenType.Indicator, SourceIndicatorId = idA }
                }
            };

            var ex = Assert.Throws<InvalidOperationException>(() => Calculation.OrderTopologicalLevels(new[] { indA }));
            Assert.Contains("references itself", ex.Message);
        }

        [Fact]
        public void ToPostFixStack_SourceIndicatorRequiresSubmit_PropagatesToTokenStruct()
        {
            var sourceId = Guid.NewGuid();
            var sourceIndicator = new Indicator
            {
                IndicatorId = sourceId,
                Code = "SRC_SUBMIT",
                RequiresSubmit = true
            };

            var calculatedIndicator = new Indicator
            {
                IndicatorId = Guid.NewGuid(),
                Code = "CALC_SUBMIT",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token
                    {
                        TokenNumber = 1,
                        TokenType = TokenType.Indicator,
                        SourceIndicatorId = sourceId,
                        SourceIndicator = sourceIndicator
                    }
                }
            };

            var stack = Calculation.ToPostFixStack(calculatedIndicator);
            var token = Assert.Single(stack);
            Assert.True(token.RequiresSubmit);
        }

        [Fact]
        public void ToPostFixStack_ComplexSubtractDivide_ReturnsCorrectPostfixOrder()
        {
            // (A - B) / C -> A B - C /
            var indA = Guid.NewGuid();
            var indB = Guid.NewGuid();
            var indC = Guid.NewGuid();

            var indicator = new Indicator
            {
                Code = "IND_COMPLEX",
                IndicatorType = IndicatorType.Calculated,
                Tokens = new List<Token>
                {
                    new Token { TokenNumber = 1, TokenType = TokenType.Parenthesis, ParenthesisType = ParenthesisType.Open },
                    new Token { TokenNumber = 2, TokenType = TokenType.Indicator, SourceIndicatorId = indA },
                    new Token { TokenNumber = 3, TokenType = TokenType.Operator, OperatorType = OperatorType.Subtract },
                    new Token { TokenNumber = 4, TokenType = TokenType.Indicator, SourceIndicatorId = indB },
                    new Token { TokenNumber = 5, TokenType = TokenType.Parenthesis, ParenthesisType = ParenthesisType.Close },
                    new Token { TokenNumber = 6, TokenType = TokenType.Operator, OperatorType = OperatorType.Divide },
                    new Token { TokenNumber = 7, TokenType = TokenType.Indicator, SourceIndicatorId = indC }
                }
            };

            var stack = Calculation.ToPostFixStack(indicator);
            var list = stack.ToList();

            Assert.Equal(5, list.Count);
            Assert.Equal(indA, list[0].IndicatorId);
            Assert.Equal(indB, list[1].IndicatorId);
            Assert.Equal(OperatorType.Subtract, list[2].OperatorType);
            Assert.Equal(indC, list[3].IndicatorId);
            Assert.Equal(OperatorType.Divide, list[4].OperatorType);
        }
    }
}
