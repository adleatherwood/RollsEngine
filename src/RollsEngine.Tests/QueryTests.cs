using System;
using System.Collections.Generic;
using System.Linq;
using FunctionalLink;
using Newtonsoft.Json.Linq;
using Xunit;
using RollsEngine.Newtonsoft;

namespace RollsEngine.Tests
{
    public class QueryTests
    {
        private static readonly INewtonsoftSource Source = new DataSource();
        private static readonly IDataService<JObject> Db = new NewtonsoftService(Source);

        [Fact]
        public void KitchenSinkExample()
        {
            var actual = RollsQuery.Execute(Db, @"
                SELECT {
                    -- select a JsonPath value
                      $.name

                    -- call a function with a JsonPath and return the result with a custom name
                    , isManager: any($.roles[?(@.title == 'manager')])

                    -- an alternate way to rename a field
                    , $.name AS 'the dudes name'

                    -- selecting and filtering an array
                    , $.roles[?(@.title == 'employee' || @.title == 'manager')]

                    -- ignoring commented lines
                    --, $.age

                    -- calling other functions
                    , utc()
                }
                FROM people
                KEYS 1, 2, 3
                WHERE starts($.name, 'Fred') OR starts($.name, 'Tim') OR starts($.name, 'Steve') AND 1=1
                ORDER BY $.name DESC
                LIMIT 2")
                .ToArray();

            actual.Iterate(Util.Dump).Evaluate();

            Assert.Equal(2, actual.Length);
        }

        [Theory]
        [InlineData("name", "Fred Thimbleberry", "$.name")]
        [InlineData("name0", "Fred Thimbleberry", "$.name AS \"name0\"")]
        [InlineData("name1", "Fred Thimbleberry", "$.name AS 'name1'")]
        [InlineData("name2", "Fred Thimbleberry", "$.name AS name2")]
        [InlineData("name3", "Fred Thimbleberry", "name3: $.name")]
        [InlineData("name4", "Fred Thimbleberry", "'name4': $.name")]
        [InlineData("name5", "Fred Thimbleberry", "\"name5\": $.name")]
        [InlineData("any", "False", "any($.roles[?(@.title == 'manager')])")]
        public void SelectPositiveTest(string name, string expected, string expression)
        {
            var actual = RollsQuery.Execute(Db, $@"
                SELECT {{ {expression} }}
                FROM people
                KEYS 1")
                .Select(j => j[name].Value<string>())
                .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1", "1")]
        [InlineData("1,3", "1,3")]
        [InlineData("1,2,3", "1,2,3")]
        [InlineData("1,2,3", "'1','2','3'")]
        public void KeysPositiveTest(string expected, string expression)
        {
            var actual = String.Join(",",
                RollsQuery.Execute(Db, $@"
                    SELECT $.id
                    FROM people
                    KEYS {expression}")
                    .Select(j => j["id"].Value<int>()));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1",   "starts($.name, 'Fred')")]
        [InlineData("1",   "starts($.name, 'Fred') OR starts($.name, 'Satan')")]
        [InlineData("1,3", "starts($.name, 'Fred') OR starts($.name, 'Steve')")]
        [InlineData("2",   "starts($.name, 'Tim') AND $.age > 18")]
        public void WherePositiveTest(string expected, string expression)
        {
            var actual = String.Join(",",
                RollsQuery.Execute(Db, $@"
                    SELECT $.id
                    FROM people
                    WHERE {expression}")
                    .Select(j => j["id"].Value<int>()));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1,2,3", "$.id")]
        [InlineData("1,2,3", "$.id ASC")]
        [InlineData("3,2,1", "$.id DESC")]
        public void OrderPositiveTest(string expected, string expression)
        {
            var actual = String.Join(",",
                RollsQuery.Execute(Db, $@"
                    SELECT $.id
                    FROM people
                    ORDER BY {expression}")
                    .Select(j => j["id"].Value<int>())
                    .ToArray());

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1,3,2", "$.age, $.name")]
        [InlineData("1,2,3", "$.age, $.name DESC")]
        [InlineData("3,2,1", "$.age DESC, $.name")]
        [InlineData("2,3,1", "$.age DESC, $.name DESC")]
        public void MultiOrderPositiveTest(string expected, string expression)
        {
            var actual = String.Join(",",
                RollsQuery.Execute(Db, $@"
                    SELECT $.id
                    FROM people
                    ORDER BY {expression}")
                    .Select(j => j["id"].Value<int>())
                    .ToArray());

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1, "1")]
        [InlineData(2, "2")]
        [InlineData(3, "10")]
        public void LimitPositiveTest(int expected, string expression)
        {
            var actual = RollsQuery.Execute(Db, $@"
                SELECT $
                FROM people
                LIMIT {expression}")
                .Count();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(3,   "COUNT($)")]
        [InlineData(113, "SUM($.age)")]
        [InlineData(23,  "MIN($.age)")]
        [InlineData(45,  "MAX($.age)")]
        [InlineData(38,  "AVG($.age)")]
        public void AggregatePositiveTest(int expected, string expression)
        {
            var result = RollsQuery.Execute(Db, $@"
                SELECT {expression} as 'X' FROM people")
                .Single();

            var actual = result["X"].Value<int>();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("A",  "'ABC', 0, 1")]
        [InlineData("AB", "'ABC', 0, 2")]
        [InlineData("BC", "'ABC', 1, 2")]
        [InlineData("BC", "'ABC', 1")]
        [InlineData("C",  "'ABC', 2, 1")]
        [InlineData("C",  "'ABC', 2")]
        public void SubFunctionPositiveTest(string expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT SUB({expression}) as 'X' ")
                    .Select(j => j["X"].Value<string>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("A",  "'A  '")]
        [InlineData("  A",  "'  A  '")]
        public void RTrimFunctionPositiveTest(string expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT RTRIM({expression}) as 'X' ")
                    .Select(j => j["X"].Value<string>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("A",  "'  A'")]
        [InlineData("A  ",  "'  A  '")]
        public void LTrimFunctionPositiveTest(string expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT LTRIM({expression}) as 'X' ")
                    .Select(j => j["X"].Value<string>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("A",  "'  A  '")]
        public void TrimFunctionPositiveTest(string expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT TRIM({expression}) as 'X' ")
                    .Select(j => j["X"].Value<string>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("TEST", "'Test'")]
        [InlineData("FRED THIMBLEBERRY","$.name")]
        public void UpperFunctionPositiveTest(string expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT UPPER({expression}) as 'X'
                    FROM people
                    KEYS 1")
                    .Select(j => j["X"].Value<string>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("test", "'Test'")]
        [InlineData("fred thimbleberry","$.name")]
        public void LowerFunctionPositiveTest(string expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT LOWER({expression}) as 'X'
                    FROM people
                    KEYS 1")
                    .Select(j => j["X"].Value<string>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("t,e,s,t", "'t e s t', ' '")]
        [InlineData("Fr,d Thimbl,b,rry", "$.name, 'e'")]
        public void SplitFunctionPositiveTest(string expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT JOIN(',', SPLIT({expression})) as 'X'
                    FROM people
                    KEYS 1")
                    .Select(j => j["X"].Value<string>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        // [Theory]
        // [InlineData(true, "")]
        // public void AnyFunctionPositiveTest(bool expected, string expression)
        // {
        //     var actual =
        //         Query.Execute(Db, $@"
        //             SELECT ANY({expression}) as 'X'
        //             FROM people
        //             LIMIT 1")
        //             .Select(j => j["X"].Value<bool>())
        //             .Single();

        //     Assert.Equal(expected, actual);
        // }

        [Theory]
        [InlineData(2008, "'2008-10-01T17:04:32'")]
        [InlineData(2010, "'2010-10-01T17:04:32'")]
        [InlineData(2020, "'2020-10-01T17:04:32'")]
        public void YearFunctionPositiveTest(int expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT YEAR({expression}) as 'X' ")
                    .Select(j => j["X"].Value<int>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(10, "'2008-10-01T17:04:32'")]
        [InlineData(11, "'2010-11-01T17:04:32'")]
        [InlineData(12, "'2020-12-01T17:04:32'")]
        public void MonthFunctionPositiveTest(int expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT MONTH({expression}) as 'X' ")
                    .Select(j => j["X"].Value<int>())
                    .Single();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("t", "'t e s t', ' '")]
        [InlineData("Fred", "$.name, ' '")]
        public void FirstFunctionPositiveTest(string expected, string expression)
        {
            var actual =
                RollsQuery.Execute(Db, $@"
                    SELECT FIRST(SPLIT({expression})) as 'X'
                    FROM people
                    KEYS 1")
                    .Select(j => j["X"].Value<string>())
                    .Single();

            Assert.Equal(expected, actual);
        }


        [Theory]
        [InlineData(true, "1 = 1")]
        [InlineData(false, "1 = 2")]
        [InlineData(true, "1 != 2")]
        [InlineData(false, "1 != 1")]
        [InlineData(true, "2 > 1")]
        [InlineData(false, "1 > 2")]
        [InlineData(true, "2 >= 2")]
        [InlineData(false, "1 >= 2")]
        [InlineData(true, "2 >= 1")]
        [InlineData(true, "1 < 2")]
        [InlineData(false, "2 < 1")]
        [InlineData(true, "1 <= 2")]
        [InlineData(true, "2 <= 2")]
        [InlineData(false, "2 <= 1")]
        [InlineData(true, "'ted' = 'ted'")]
        [InlineData(false, "'ted' = 'tim'")]
        [InlineData(true, "true = true")]
        [InlineData(true, "false = false")]
        [InlineData(false, "true = false")]
        public static void CompareEvalTests(bool expected, string expression)
        {
            var input = Input.Create(expression);
            var compare = Parsers.Compare.Parse(input);
            var actual = compare.Match(
                success => success.Value.Eval(Db, null),
                failure => false);
            var text = compare.Match(
                success => success.Value.ToText(),
                failure => "fail");

            Assert.Equal(expected, actual);
            Assert.Equal(expression, text);
        }

        [Theory]
        [InlineData(true, "1 = 1")]
        [InlineData(true, "true")]
        [InlineData(true, "NOT 1 = 2")]
        [InlineData(true, "NOT false")]
        public static void ArgumentEvalTests(bool expected, string expression)
        {
            var input = Input.Create(expression);
            var compare = Parsers.Argument.Parse(input);
            var actual = compare.Match(
                success => success.Value.Eval(Db, null),
                failure => false);
            var text = compare.Match(
                success => success.Value.ToText(),
                failure => "fail");

            Assert.Equal(expected, actual);
            Assert.Equal(expression, text);
        }

        [Theory]
        [InlineData(true, "1 = 1")]
        [InlineData(false, "1 = 2")]
        [InlineData(true, "1 = 1 AND 2 = 2")]
        [InlineData(false, "1 = 1 AND 2 = 3")]
        public static void TermEvalTests(bool expected, string expression)
        {
            var input = Input.Create(expression);
            var compare = Parsers.Term.Parse(input);
            var actual = compare.Match(
                success => success.Value.Eval(Db, null),
                failure => false);
            var text = compare.Match(
                success => success.Value.ToText(),
                failure => "fail");

            Assert.Equal(expected, actual);
            Assert.Equal(expression, text);
        }

        [Theory]
        [InlineData(true, "(1 = 1)")]
        [InlineData(false, "(1 = 2)")]
        [InlineData(true, "(1 = 2 OR 1 = 1)")]
        [InlineData(true, "(1 = 2 OR 1 = 3 OR 1 = 1)")]
        [InlineData(true, "(('a' = 'b' AND 1 = 2) OR (1 = 1 AND 'a' = 'a'))")]
        [InlineData(true, "((true AND false) OR (true AND true))")]
        public static void LogicalEvalTests(bool expected, string expression)
        {
            var input = Input.Create(expression);
            var compare = Parsers.Logical.Parse(input);
            var actual = compare.Match(
                success => success.Value.Eval(Db, null),
                failure => false);
            var text = compare.Match(
                success => success.Value.ToText(),
                failure => "fail");

            Assert.Equal(expected, actual);
            Assert.Equal(expression, text);
        }


        // --------------------------------------------------------------------------------------\\\
        // --- SERVICES -----------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------///

        private static readonly JObject[] Dataset = new JObject[] {
            JObject.Parse("{ id: 1, name: 'Fred Thimbleberry', age: 23, roles: [ { title: 'employee' } ] }"),
            JObject.Parse("{ id: 2, name: 'Tim Burklebunny',   age: 45, roles: [ { title: 'manager'  }, { title: 'employee' } ] }"),
            JObject.Parse("{ id: 3, name: 'Steve Forklemeyer', age: 45, roles: [ { title: 'employee' } ] }"),
        };

        private class DataSource : INewtonsoftSource
        {
            public IEnumerable<JObject> Read(string from) =>
                Dataset;

            public IEnumerable<JObject> Read(string from, string[] keys) =>
                Dataset.Where(d =>
                {
                    var id = (d.SelectToken("$.id")).Value<string>();
                    return keys.Contains(id);
                });
        }
    }
}