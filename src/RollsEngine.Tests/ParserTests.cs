using FunctionalLink;
using Xunit;

namespace RollsEngine.Tests
{
    public class ParserTests
    {
        [Theory]
        [InlineData("123")]
        [InlineData("-123")]
        [InlineData("+123")]
        [InlineData("123.45")]
        [InlineData("-123.45")]
        public void NumberLiteralPositiveTest(string text)
        {
            var input = Input.Create(text);
            var expected = decimal.Parse(text);
            var actual = Parsers.Number.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.Value.Value.Value);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData(" 123")]
        public void NumberLiteralNegativeTest(string text)
        {
            var input = Input.Create(text);
            var actual = Parsers.Number.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("abc def")]
        public void TextLiteralPositiveTest(string expected)
        {
            var input = Input.Create($"'{expected}'");
            var actual = Parsers.Text.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.Value.Value.Value);
        }

        [Fact]
        public void TextLiteralNegativeTest()
        {
            var input = Input.Create("abc");
            var actual = Parsers.Text.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("True")]
        public void BoolLiteralPositiveTest(string text)
        {
            var input = Input.Create(text);
            var expected = bool.Parse(text);
            var actual = Parsers.Bool.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.Value.Value.Value);
        }

        [Fact]
        public void BoolLiteralNegativeTest()
        {
            var input = Input.Create("abc");
            var actual = Parsers.Bool.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("NULL")]
        [InlineData("Null")]
        public void NullLiteralPositiveTest(string text)
        {
            var input = Input.Create(text);
            var actual = Parsers.Null.Parse(input);

            Assert.True(actual.IsSuccess);
        }

        [Fact]
        public void NullLiteralNegativeTest()
        {
            var input = Input.Create("abc");
            var actual = Parsers.Null.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("true")]
        [InlineData("null")]
        [InlineData("'test'")]
        public void LiteralPositiveTest(string text)
        {
            var input = Input.Create(text);
            var actual = Parsers.Literal.Parse(input);

            Assert.True(actual.IsSuccess);
        }

        [Fact]
        public void LiteralNegativeTest()
        {
            var input = Input.Create("abc");
            var actual = Parsers.Literal.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("dostuff1")]
        [InlineData("do_stuff1")]
        [InlineData("do-stuff1")]
        public void IdentifierPositiveTest(string expected)
        {
            var input = Input.Create(expected);
            var actual = Parsers.Identifier.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.Value.Value.Value);
        }

        [Fact]
        public void IdentifierNegativeTest()
        {
            var input = Input.Create("$.abc");
            var actual = Parsers.Literal.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("$.Manufacturers[?(@.Name == 'Acme Co')],")]
        [InlineData("$..Products[?(@.Price >= 50)].Name,")]
        [InlineData("$.phoneNumbers[:1].type,")]
        [InlineData("$.phoneNumbers[1,2].type,")]
        [InlineData("$.value,")]
        [InlineData("$,")]
        public void PathPositiveTest(string expected)
        {
            var input = Input.Create(expected);
            var actual = Parsers.Path.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected.Substring(0, expected.Length-1), actual.Value.Value.Value);
        }

        [Fact]
        public void PathNegativeTest()
        {
            var input = Input.Create("abc");
            var actual = Parsers.Path.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("true")]
        [InlineData("null")]
        [InlineData("'test'")]
        [InlineData("$.value,", "$.value")]
        [InlineData("$.Manufacturers[?(@.Name == 'Acme Co')],", "$.Manufacturers[?(@.Name == 'Acme Co')]")]
        [InlineData("utc()")]
        public void ValuePositiveTest(string text, string expected = null)
        {
            var input = Input.Create(text);
            var actual = Parsers.Value.Parse(input).Match(
                success => success.Value.ToText(),
                failure => "fail");

            Assert.Equal(expected ?? text, actual);
        }

        [Theory]
        [InlineData("test")]
        [InlineData("  TEST")]
        [InlineData("Test  ")]
        [InlineData("  test  ")]
        public void TokenPositiveTest(string expected)
        {
            var input = Input.Create(expected);
            var actual = Parsers.Token("test").Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal("test", actual.Value.Value.ToLower());
        }

        [Theory]
        [InlineData("nowUtc()", "nowUtc()")]
        [InlineData("2int('123')", "2int('123')")]
        [InlineData("multiguy(1, true)", "multiguy(1,true)")]
        [InlineData("isTrue($.value)", "isTrue($.value)")]
        public void FunctionPositiveTest(string text, string expected)
        {
            var input = Input.Create(text);
            var actual = Parsers.Function.Parse(input).Match(
                success =>  success.Value.ToText(),
                failure => "fail");

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("$")]
        [InlineData("123")]
        [InlineData("'test'")]
        [InlineData("null")]
        public void FunctionNegativeTest(string text)
        {
            var input = Input.Create(text);
            var actual = Parsers.Function.Parse(input);

            Assert.False(actual.IsSuccess);
        }

        [Theory]
        [InlineData(">", CompareOperator.Gt)]
        [InlineData(">=", CompareOperator.Gte)]
        [InlineData("=", CompareOperator.E)]
        [InlineData("<=", CompareOperator.Lte)]
        [InlineData("<", CompareOperator.Lt)]
        public void CompareOperatorPositiveTest(string text, CompareOperator expected)
        {
            var input = Input.Create(text);
            var actual = Parsers.CompareOp.Parse(input);

            Assert.Equal(expected, actual.Value.Value);
        }

        [Theory]
        [InlineData("2 > 1")]
        [InlineData("$.value <= 3")]
        [InlineData("isThing($.value)")]
        [InlineData("isThing(1,$.value) = true")]
        public void ComparePositiveTest(string expected)
        {
            var input = Input.Create(expected);
            var actual = Parsers.Compare.Parse(input);

            Assert.Equal(expected, actual.Value.Value.ToText());
        }

        [Theory]
        [InlineData("(2 > 1 AND 3 > 2)")]
        [InlineData("(2 > 1 AND 3 > 2 AND null > 3)")]
        [InlineData("(2 > 1 OR 3 > 2)")]
        [InlineData("(2 > 1 OR 3 > 2 OR 4.23 > 3)")]
        [InlineData("(2 > 1 AND 3 > 2 OR 4 > 3)")]
        [InlineData("(2 > 1 OR 3 > 2 AND 4 > 'test')")]
        [InlineData("(2 > 1 AND (3 > 2 OR 2 < 3))")]
        [InlineData("(2 > 1 OR (3 > 2 AND 2 < true))")]
        [InlineData("((true AND false) OR (true AND true))")]
        [InlineData("($.value <= 3)")]
        [InlineData("(isThing($.value))")]
        [InlineData("(isThing(1,$.value) = true AND 1 = 1)")]
        public void LogicalPositiveTest(string expected)
        {
            var input = Input.Create(expected);
            var actual = Parsers.Logical.Parse(input);
            var text = actual.Value.Value.ToText();

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, text);
        }

        // MORE TESTS HERE ...

        // [Fact]
        // public void SimpleKeyPositiveTest()
        // {
        //     var input = Input.Create("'123'");
        //     var actual = Parsers.SimpleKey.Parse(input);

        //     Assert.True(actual.IsSuccess);
        //     Assert.Equal("123", actual.Value.Value.Value);
        // }

        // [Fact]
        // public void CompoundKeyPositiveTest()
        // {
        //     var input = Input.Create("['123','456']");
        //     var actual = Parsers.CompoundKey.Parse(input);

        //     Assert.True(actual.IsSuccess);
        //     Assert.Equal(2, actual.Value.Value.Keys.Length);
        //     Assert.Equal("123", actual.Value.Value.Keys[0].Value);
        //     Assert.Equal("456", actual.Value.Value.Keys[1].Value);
        // }

        // [Theory]
        // [InlineData("123|456", "['123','456']")]
        // [InlineData("123", "'123'")]
        // public void KeyPositiveTest(string expected, string keys)
        // {
        //     var input = Input.Create(keys);
        //     var result = Parsers.Key.Parse(input);

        //     Assert.True(result.IsSuccess);

        //     var actual = result.Value.Value.CompundKey != null
        //         ? String.Join("|", result.Value.Value.CompundKey.Keys.Select(k => k.Value))
        //         : result.Value.Value.SimpleKey.Value;

        //     Assert.Equal(expected, actual);
        // }

//         [Theory]
//         [InlineData("123|321,456|654", "KEYS ['123', '321'], ['456', '654']")]
//         [InlineData("123,456", "KEYS '123', '456'")]
//         [InlineData("123|321,456", "KEYS ['123', '321'], '456'")]
//         public void KeysPositiveTest(string expected, string keys)
//         {
//             var tokens = Lexer.Parse(keys);
//             var input = Input.Create(tokens);
//             var result = Parsers.Keys.Parse(input);

//             Assert.True(result.IsSuccess);

//             var actual = String.Join(",",
//                 result.Value.Value.Keys
//                     .Select(k => k.CompundKey != null
//                     ? String.Join("|", k.CompundKey.Keys.Select(k => k.Value))
//                     : k.SimpleKey.Value));

//             Assert.Equal(expected, actual);
//         }

        [Fact]
        public void FromPositiveTest()
        {
            var input = Input.Create("FROM some-index");
            var actual = Parsers.From.Parse(input);

            Assert.True(actual.IsSuccess);
            Assert.Equal("some-index", actual.Value.Value.Name.Value);
        }

        [Theory]
        [InlineData("ORDER BY $.name LIMIT 1")]
        [InlineData("ORDER BY $.name, '1', true LIMIT 1")]
        public void OrderPositiveTest(string expression)
        {
            var input = Input.Create(expression);
            var actual = Parsers.OrderBy.Parse(input);

            Assert.True(actual.IsSuccess);
            //Assert.Equal("some-index", actual.Value.Value.Name.Value);
        }

        [Fact]
        public void ImpatientTest()
        {
            var input = Input.Create(@"
                SELECT $
                FROM some-index
                KEYS '123'
                LIMIT 10;");

            var actual = Parsers.Query.Parse(input);

            Assert.True(actual.IsSuccess);
        }
    }
}
