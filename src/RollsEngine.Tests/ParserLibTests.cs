using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RollsEngine.Tests
{
    public class ParserLibTests
    {
        [Theory]
        [InlineData("a")]
        [InlineData("A")]
        public void SatisfyStringPositiveTest(string expected)
        {
            var input = Input.Create(expected);
            var parser = Parser.Satisfy("Letter", expected, true);
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.Value.Value);
        }

        [Fact]
        public void SatisfyStringNegativeTest()
        {
            var input = Input.Create("a");
            var parser = Parser.Satisfy("Letter", "A", false);
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("A")]
        public void SatisfyRegexPositiveTest(string expected)
        {
            var input = Input.Create(expected);
            var regex = new Regex(expected, RegexOptions.IgnoreCase);
            var parser = Parser.Satisfy("Letter", regex);
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.Value.Value.Value);
        }

        [Fact]
        public void SatisfyRegexNegativeTest()
        {
            var input = Input.Create("b");
            var regex = new Regex("A");
            var parser = Parser.Satisfy("Letter", regex);
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("test")]
        [InlineData("Test")]
        [InlineData("TEST")]
        public void ExactPositiveTest(string value)
        {
            var input = Input.Create("test");
            var parser = Parser.Exact(value);
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
        }

        [Fact]
        public void ExactNegativeTest()
        {
            var input = Input.Create("fail");
            var parser = Parser.Exact("test");
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Fact]
        public void AndPositiveTest()
        {
            var input = Input.Create("thisthat");
            var parser = Parser.Exact("this").And(Parser.Exact("that"));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal("this", actual.SuccessValue.Value.Item1);
            Assert.Equal("that", actual.SuccessValue.Value.Item2);
        }

        [Fact]
        public void AndNegativeTest()
        {
            var input = Input.Create("thisthose");
            var parser = Parser.Exact("this").And(Parser.Exact("that"));
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("this")]
        [InlineData("that")]
        public void OrPositiveTest(string value)
        {
            var input = Input.Create(value);
            var parser = Parser.Exact("this").Or(Parser.Exact("that"));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(value, actual.SuccessValue.Value);
        }

        [Fact]
        public void OrNegativeTest()
        {
            var input = Input.Create("those");
            var parser = Parser.Exact("this").Or(Parser.Exact("that"));
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Fact]
        public void SkipPositiveTest()
        {
            var input = Input.Create("thisthat");
            var parser = Parser.Exact("this").Skip(Parser.Exact("that"));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal("this", actual.SuccessValue.Value);
        }

        [Fact]
        public void SkipNegativeTest()
        {
            var input = Input.Create("those");
            var parser = Parser.Exact("this").Skip(Parser.Exact("that"));
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Fact]
        public void TakePositiveTest()
        {
            var input = Input.Create("thisthat");
            var parser = Parser.Exact("this").Take(Parser.Exact("that"));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal("that", actual.SuccessValue.Value);
        }

        [Fact]
        public void TakeNegativeTest()
        {
            var input = Input.Create("those");
            var parser = Parser.Exact("this").Take(Parser.Exact("that"));
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Fact]
        public void MapPositiveTest()
        {
            var input = Input.Create("123");
            var parser = Parser.Exact("123").Map(decimal.Parse);
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(123, actual.SuccessValue.Value);
        }

        [Theory]
        [InlineData(3, "111")]
        [InlineData(1, "1")]
        [InlineData(0, "2")]
        public void ManyPositiveTest(int found, string text)
        {
            var input = Input.Create(text);
            var parser = Parser.Many(Parser.Exact("1"));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(found, actual.SuccessValue.Value.Length);
        }

        [Theory]
        [InlineData(3, "111")]
        [InlineData(1, "1")]
        public void Many1PositiveTest(int expected, string text)
        {
            var input = Input.Create(text);
            var parser = Parser.Many1(Parser.Exact("1"));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.SuccessValue.Value.Length);
        }

        [Fact]
        public void Many1NegativeTest()
        {
            var input = Input.Create("2");
            var parser = Parser.Many1(Parser.Exact("1"));
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("1", "1")]
        [InlineData(null, "2")]
        public void OptionalPositiveTest(string expected, string text)
        {
            var input = Input.Create(text);
            var parser = Parser.Optional(Parser.Exact("1"));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.SuccessValue.Value.FirstOrDefault());
        }

        [Theory]
        [InlineData(3, "1,1,1")]
        [InlineData(1, "1")]
        public void Separated1PositiveTest(int expected, string text)
        {
            var input = Input.Create(text);
            var parser = Parser.Separated1(Parser.Exact("1"), Parser.Exact(","));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.SuccessValue.Value.Length);
        }

        [Fact]
        public void Separated1NegativeTest()
        {
            var input = Input.Create("2");
            var parser = Parser.Separated1(Parser.Exact("1"), Parser.Exact(","));
            var actual = parser.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData(3, "1,1,1")]
        [InlineData(1, "1")]
        [InlineData(0, "")]
        public void SeparatedPositiveTest(int expected, string text)
        {
            var input = Input.Create(text);
            var parser = Parser.Separated(Parser.Exact("1"), Parser.Exact(","));
            var actual = parser.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.SuccessValue.Value.Length);
        }
     }
}
