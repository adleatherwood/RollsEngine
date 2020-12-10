using FunctionalLink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static FunctionalLink.GlobalLink;

namespace RollsEngine
{
	public delegate Result<(Input Remaining, T Value)> Parse<T>(Input input);

	public class Input
    {
		public Input(string source, int index) =>
			(Source, Index) = (source, index);

		private readonly string Source;
        private readonly int Index;

		public bool IsEmpty => Index > Source.Length;

		public bool TryTake(string expected, out (Input Remaining, string Found) found, bool ignoreCase = false)
		{
			var length = Util.Lesser(expected.Length, Source.Length - Index);

			var value = length >= expected.Length && String.Compare(Source, Index, expected, 0, length, ignoreCase) == 0
				? Source.Substring(Index, length)
				: "";

			var remaining = value != ""
				? new Input(Source, Index + value.Length)
				: this;

			found = (remaining, value);

			return value != "";
		}

		public bool TryTake(Regex expected, out (Input Remaining, Match Found) found) {
			var match = expected.Match(Source, Index);

			var value = match.Success && match.Index == Index
				? match
				: null;

			var remaining = value != null
				? new Input(Source, Index + value.Length)
				: this;

			found = (remaining, value);

			return value != null;
		}

		public bool TryRead(Func<string,int, int> reader, out (Input Remaining, string Found) found) {
			var index = reader(Source, Index);

			var value = index > Index
				? Source.Substring(Index, index - Index)
				: null;

			var remaining = value != null
				? new Input(Source, index)
				: this;

			found = (remaining, value);

			return value != null;
		}

		public static Input Create(string value) =>
			new Input(value, 0);
	}

	public class Parser<T>
	{
		private Parse<T> _parse;

		public Parser(string l, Parse<T> p) =>
			(Label, _parse) = (l, Default(p));

		public readonly string Label;
		public Parse<T> Parse => _parse;

		private static Parse<T> Default(Parse<T> p)
		{
			return (input) =>
			{
				if (input.IsEmpty)
					return Failure("End of input");

				return p(input);
			};
		}

		// NOTE: to help manage circular referencing parsers
		internal void Inject(Parse<T> p)
		{
			_parse = p;
		}
	}

	public static class Parser
	{
		public static Parser<Match> Satisfy(string label, Regex expected) =>
			new Parser<Match>(label, (input) =>  {
				return input.TryTake(expected, out var found)
					? Success(found)
					: Failure($"{label} not found");
			});

		public static Parser<string> Satisfy(string label, string expected, bool ignoreCase = false) =>
			new Parser<string>(label, (input) =>  {
				return input.TryTake(expected, out var found, ignoreCase)
					? Success(found)
					: Failure($"{label} not found");
			});

		public static Parser<string> Read(string label, Func<string,int,int> reader) =>
			new Parser<string>(label, (input) => {
				return input.TryRead(reader, out var found)
					? Success(found)
					: Failure($"{label} not found");
			});

		public static Parser<T> Create<T>(string label, Parse<T> parse) =>
			new Parser<T>(label, parse);

		public static Parser<T> Label<T>(this Parser<T> parser, string label) =>
			String.IsNullOrWhiteSpace(label)
				? parser
				: new Parser<T>(label, parser.Parse);

		public static Parser<string> Exact(string expected, string label = "" ) =>
			Satisfy(label, expected, true);

		public static Parser<(A,B)> And<A,B>(this Parser<A> a, Parser<B> b, string label = "") =>
			new Parser<(A,B)>(label, (input) =>
				a.Parse(input).Match(
					success1 => b.Parse(success1.Remaining).Match(
						success2 => Success((success2.Remaining, (success1.Value, success2.Value))),
						failure2 => Failure(failure2)),
					failure => Failure(failure)));

		public static Parser<T> Or<T>(this Parser<T> a, Parser<T> b, string label = "") =>
			new Parser<T>(label, (input) =>
				a.Parse(input).Match(
					success => Success(success),
					failure => b.Parse(input).Match(
						success => Success(success),
						failure => Failure(failure))));

		public static Parser<A> Skip<A,B>(this Parser<A> a, Parser<B> b, string label = "") =>
			new Parser<A>(label, (input) =>
				 a.Parse(input).Match(
					 success1 => b.Parse(success1.Remaining).Match(
						 success2 => Success((success2.Remaining, success1.Value)),
						 failure2 => Failure(failure2)),
					 failure => Failure(failure)));

		public static Parser<B> Take<A, B>(this Parser<A> a, Parser<B> b, string label = "") =>
			new Parser<B>(label, (input) =>
				 a.Parse(input).Match(
					 success1 => b.Parse(success1.Remaining).Match(
						 success2 => Success((success2.Remaining, success2.Value)),
						 failure2 => Failure(failure2)),
					 failure => Failure(failure)));

		public static Parser<B> Map<A, B>(this Parser<A> p, Func<A, B> mapper, string label = "") =>
			Map(p, (value) => Success(mapper(value)), label);

		public static Parser<B> Map<A, B>(this Parser<A> p, Func<A,Result<B>> mapper, string label = "") =>
			Create(label, (input) => p.Parse(input).Match(
				success => mapper(success.Value).Match(
					success1 => Success((success.Remaining, success1)),
					failure => Failure(failure)),
				failure => Failure(failure)));

		public static Parser<T[]> Many<T>(this Parser<T> parser, string label = "") =>
			Create(label, (input) =>
			{
				var result = new List<T>();
				var keepon = true;
				var remaining = input;

				while (keepon)
					parser.Parse(remaining).Match(
						success => { remaining = success.Remaining; result.Add(success.Value); return Success(""); },
						failure => { keepon = false; return Failure("Not found"); });

				return Success((remaining, result.ToArray()));
			});

		public static Parser<T[]> Many1<T>(this Parser<T> parser, string label = "") =>
			parser
				.And(Many(parser))
				.Map((t) => Success(new [] { t.Item1 }.Concat(t.Item2).ToArray()))
				.Label(label);

		public static Parser<Option<T>> Optional<T>(this Parser<T> parser, string label = "") =>
			Create(label, (input) => parser.Parse(input).Match(
				success => Success((success.Remaining, Some(success.Value))),
				failure => Success((input, Option.None<T>()))));

		public static Parser<T[]> Separated<T, D>(this Parser<T> parser, Parser<D> delimiter, string label = "") =>
			Many(parser.Skip(Optional(delimiter)))
				.Map(a => Success(a))
				.Label(label);

		public static Parser<T[]> Separated1<T, D>(this Parser<T> parser, Parser<D> delimiter, string label = "") =>
			Many1(parser.Skip(Optional(delimiter)))
				.Map(a => Success(a))
				.Label(label);

		public static (A, B, C) Flatten3<A, B, C>(((A, B), C) value) =>
			(value.Item1.Item1, value.Item1.Item2, value.Item2);

		public static (A, B, C, D) Flatten4<A, B, C, D>((((A, B), C), D) value) =>
			(value.Item1.Item1.Item1, value.Item1.Item1.Item2, value.Item1.Item2, value.Item2);

		public static (A, B, C, D, E) Flatten5<A, B, C, D, E>(((((A, B), C), D), E) value) =>
			(value.Item1.Item1.Item1.Item1, value.Item1.Item1.Item1.Item2, value.Item1.Item1.Item2, value.Item1.Item2, value.Item2);

		public static (A, B, C, D, E, F) Flatten6<A, B, C, D, E, F>((((((A, B), C), D), E), F) value) =>
			(value.Item1.Item1.Item1.Item1.Item1, value.Item1.Item1.Item1.Item1.Item2, value.Item1.Item1.Item1.Item2, value.Item1.Item1.Item2, value.Item1.Item2, value.Item2);

		public static Parser<string> Whitespace =
			Exact(" ", "Whitespace")
				.Or(Exact("\r"))
				.Or(Exact("\n"));

		public static Parser<string> Whitespaces =
			Many(Whitespace, "Whitespaces")
				.Map(v => String.Join("", v));

		public static Parser<string> Whitespaces1 =
			Many1(Whitespace, "Whitespaces")
				.Map(v => String.Join("", v));
	}
}
