using FunctionalLink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RollsEngine
{
    public static class RollsQuery
    {
        private static readonly Regex Commments = new Regex(@"--(.*?)$", RegexOptions.Multiline);

        public static IEnumerable<TObject> Execute<TObject>(IDataService<TObject> db, string query)
            where TObject: class
        {
            var uncommented = Commments.Replace(query, "");

            var input = Input.Create(uncommented);

            var parsed = Parsers.Query.Parse(input).Match(
                success => success.Value,
                failure => throw new InvalidOperationException(failure));

            var name = parsed.From.Match(
                some => some.Name.Value,
                none => "");

            var keys = parsed.Keys.Match(
                some => some.Keys.Select(l => l.ToObject().ToString()).ToArray(),
                none => new string[0]);

            var data = parsed.From.IsSome
                ? keys.Any()
                    ? db.Read(name, keys)
                    : db.Read(name)
                : new TObject[] { db.New() };

            var filtered = parsed.Where.Match(
                some => data.Where(some.Eval(db)),
                none => data);

            var ordered = parsed.OrderBy.Match(
                some => filtered.OrderBy(some.Eval(db)),
                none => filtered);

            // todo: and distinct by?

            var limited = parsed.Limit.Match(
                some => ordered.Take((int) some.Amount.Value),
                none => ordered);

            var selected = parsed.Select.Statements != null
                ? limited.Select(parsed.Select.Statements.Eval(db))
                : limited.Aggregate(parsed.Select.Aggregates.Init(), parsed.Select.Aggregates.Eval(db))
                    .Singleton()
                    .Select(d => d.ToObject(db));

            return selected;
        }
    }
}
