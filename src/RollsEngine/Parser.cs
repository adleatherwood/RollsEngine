using System.Text.RegularExpressions;
using FunctionalLink;
using static FunctionalLink.GlobalLink;

namespace RollsEngine
{
	public static class Parsers
	{
		// --- BASIC PARSERS --------------------------------------------------------------------------

		public static readonly Regex NumberEx = new Regex(@"[+-]?(\d*\.)?\d+");
		public static readonly Regex TextEx = new Regex(@"[""|'](.*?)[""|']");
		public static readonly Regex BoolEx = new Regex(@"true|false", RegexOptions.IgnoreCase);
		public static readonly Regex NullEx = new Regex(@"null", RegexOptions.IgnoreCase);
		public static readonly Regex IdentifierEx = new Regex(@"[\w\-]+");
		public static readonly Regex PathEx = new Regex(@"\$.*[,| FROM]", RegexOptions.IgnoreCase);

		public static readonly Parser<NumberLiteral> Number =
			Parser.Satisfy("Number", NumberEx)
				.Map(m => new NumberLiteral { Value = decimal.Parse(m.Value) });

		public static readonly Parser<TextLiteral> Text =
			Parser.Satisfy("Text", TextEx)
				.Map(m => new TextLiteral { Value = m.Groups[1].Value });

		public static readonly Parser<BoolLiteral> Bool =
			Parser.Satisfy("Bool", BoolEx)
				.Map(m => new BoolLiteral { Value = bool.Parse(m.Value) });

		public static readonly Parser<NullLiteral> Null =
			Parser.Satisfy("Null", NullEx)
				.Map(m => new NullLiteral { });

		public static readonly Parser<LiteralExpression1> Literal =
			Number.Map(v => Success(new LiteralExpression1 { Number = v }))
				.Or(Text.Map(v => Success(new LiteralExpression1 { Text = v })))
				.Or(Bool.Map(v => Success(new LiteralExpression1 { Bool = v })))
				.Or(Null.Map(v => Success(new LiteralExpression1 { Null = v })))
				.Label("Literal Expression");

		public static readonly Parser<Identifier> Identifier =
			Parser.Satisfy("Identifier", IdentifierEx)
				.Map(m => new Identifier { Value = m.Value });

		public static readonly Parser<Path> Path =
			Parser.Read("Path", (source, start) => {
				if (start >= source.Length || source[start] != '$')
					return start;

				var nested = 0;

				for(var i = start; i < source.Length; i++) {
					if (source[i] == '(' || source[i] == '[') nested++;
					if (source[i] == ')' || source[i] == ']') nested--;
					if (nested <= 0) {
						if (source[i] == ',') return i;
						if (source[i] == '<') return i;
						if (source[i] == '>') return i;
						if (source[i] == '=') return i;
						if (source[i] == ')') return i;
						if (source[i] == '}') return i;
						// note: this getting out of hand, but the alternative is make a full JsonPath parser
						if (source.Length - i >= 2 && string.Compare("AS", 0, source, i, 2, true) == 0) return i;
						if (source.Length - i >= 4 && string.Compare("FROM", 0, source, i, 4, true) == 0) return i;
						if (source.Length - i >= 4 && string.Compare("DESC", 0, source, i, 4, true) == 0) return i;
						if (source.Length - i >= 5 && string.Compare("ORDER", 0, source, i, 5, true) == 0) return i;
						if (source.Length - i >= 5 && string.Compare("LIMIT", 0, source, i, 5, true) == 0) return i;
					}
				}

				return start;
			})
			.Map(v => new Path() { Value = v.Trim() })
			.Label("Path");

		private static readonly Parser<FunctionExpression> FunctionPtr =
			Parser.Create<FunctionExpression>("Function", (input) => null);

		public static readonly Parser<ValueExpression1> Value =
			Path.Map(v => new ValueExpression1 { Path = v })
				.Or(Literal.Map(v => new ValueExpression1 { Literal = v }))
				.Or(FunctionPtr.Map(v => new ValueExpression1 { Function = v }))
				.Label("Value");

		public static Parser<string> Token(string token) =>
			Parser.Whitespaces
				.Take(Parser.Exact(token))
				.Skip(Parser.Whitespaces)
				.Label(token);

		// --- FUNCTION PARSERS --------------------------------------------------------------------------

		public static readonly Parser<FunctionExpression> Function =
			Identifier
				.Skip(Token("("))
				.And(Value.Separated(Token(",")))
				.Skip(Token(")"))
				.Map(t => new FunctionExpression
				{
					Identifier = t.Item1,
					Parameters = t.Item2,
				})
				.Label("Function");

		// --- COMPARISON PARSERS --------------------------------------------------------------------------

		public static readonly Parser<CompareOperator> CompareOp =
			Token(">=").Map(v => RollsEngine.CompareOperator.Gte)
				.Or(Token(">").Map(v => RollsEngine.CompareOperator.Gt))
				.Or(Token("!=").Map(v => RollsEngine.CompareOperator.Ne))
				.Or(Token("=").Map(v => RollsEngine.CompareOperator.E))
				.Or(Token("<=").Map(v => RollsEngine.CompareOperator.Lte))
				.Or(Token("<").Map(v => RollsEngine.CompareOperator.Lt))
				.Label("Compare operator");

		public static readonly Parser<CompareExpression> Compare =
			Value.And(CompareOp.And(Value).Optional())
				.Map(t => new CompareExpression
				{
					Left = t.Item1,
					Right = t.Item2
				})
				.Label("Compare expression");

		// --- LOGICAL PARSERS --------------------------------------------------------------------------

		public static readonly Parser<LogicalExpression> LogicalPtr =
			Parser.Create<LogicalExpression>("Logical Expression", (_) => null);

		public static readonly Parser<LogicalFactor1> Factor =
			Compare.Map(v => new LogicalFactor1 { Comparison = v })
				.Or(LogicalPtr.Map(v => new LogicalFactor1 { Expression = v}))
				.Label("Logical factor");

		public static readonly Parser<LogicalArgument> Argument =
			Token("NOT").Optional().And(Factor)
				.Map(v => new LogicalArgument
				{
					Invert = v.Item1.IsSome,
					Factor = v.Item2
				})
				.Label("Logical argument");

		public static readonly Parser<LogicalTerm> Term =
			Argument.And(Parser.Many(Token("AND").Take(Argument)))
				.Map(v => new LogicalTerm
				{
					Argument = v.Item1,
					Ands = v.Item2
				})
				.Label("Logical term");

		public static readonly Parser<LogicalExpression> Logical =
			Token("(").Optional()
				// note: this explicit check for multiple ors is undesirable
				.Take(Term.And(Parser.Many((Token("OR(").Or(Token("OR "))).Take(Term))))
				.Skip(Token(")").Optional())
				.Map(v => new LogicalExpression
				{
					Term = v.Item1,
					Ors = v.Item2
				})
				.Label("Logical Expression");

		// --- AGGREGATE PARSERS -----------------------------------------------------------------------

		public static readonly Parser<AggregateFunction> Aggregate =
			Identifier.Map(i => new TextLiteral { Value = i.Value })
				.Or(Text)
				.Skip(Token(":")).Optional()
				.And(Parser.Exact("COUNT").Map(v => AggregateType.Count)
					.Or(Parser.Exact("SUM").Map(v => AggregateType.Sum))
					.Or(Parser.Exact("MIN").Map(v => AggregateType.Min))
					.Or(Parser.Exact("MAX").Map(v => AggregateType.Max))
					.Or(Parser.Exact("AVG").Map(v => AggregateType.Avg)))
				.Skip(Token("("))
				.And(Value)
				.Skip(Token(")"))
				.And(Token("AS").Take(
					Identifier.Map(i => new TextLiteral { Value = i.Value })
					.Or(Text)
				).Optional())
				.Map(Parser.Flatten4)
				.Map(v => new AggregateFunction
				{
					Name = v.Item1.IsSome ? v.Item1 : v.Item4,
					Function = v.Item2,
					Value = v.Item3
				});

		public static readonly Parser<AggregateFunction[]> Aggregates =
			Aggregate.Separated1(Token(","));

		// --- SELECT PARSERS --------------------------------------------------------------------------

		public static readonly Parser<SelectStatement> SelectStatement =
			Identifier.Map(i => new TextLiteral { Value = i.Value })
				.Or(Text)
				.Skip(Token(":")).Optional()
				.And(Value)
				.And(Token("AS").Take(
					Identifier.Map(i => new TextLiteral { Value = i.Value })
					.Or(Text)
				).Optional())
				.Map(Parser.Flatten3)
				.Map(v => new SelectStatement
				{
					Name = v.Item1.IsSome ? v.Item1 : v.Item3,
					Value = v.Item2,
				});

		public static readonly Parser<SelectStatement[]> SelectStatements =
			SelectStatement.Separated1(Token(","));

		public static readonly Parser<SelectExpression1> Select =
			Token("SELECT")
				.Skip(Token("{").Optional())
				.Take(Aggregates.Map(a => new SelectExpression1{ Aggregates = a })
					.Or(SelectStatements.Map(s => new SelectExpression1{ Statements = s })))
				.Skip(Token("}").Optional());

		// --- FROM PARSERS --------------------------------------------------------------------------

		public static readonly Parser<FromExpression> From =
			Token("FROM")
				.Take(Identifier)
				.Map(v => new FromExpression
				{
					Name = v
				});

		// --- KEY PARSERS --------------------------------------------------------------------------

		public static readonly Parser<KeysExpression> Keys =
			Token("KEYS")
				.Take(Literal.Separated1(Token(",")))
				.Map(v => new KeysExpression
				{
					Keys = v
				});

		// --- WHERE PARSERS --------------------------------------------------------------------------

		public static readonly Parser<WhereExpression> Where =
			Token("WHERE")
				.Take(Logical)
				.Map(v => new WhereExpression
				{
					Expression = v
				});

		// --- ORDER PARSERS --------------------------------------------------------------------------

		public static readonly Parser<OrderOperator> OrderOp =
			Token("ASC").Map(v => OrderOperator.Asc)
				.Or(Token("DESC").Map(v => OrderOperator.Desc));

		public static readonly Parser<OrderStatement> OrderStmt =
			Value.And(OrderOp.Optional())
				.Map(v => new OrderStatement
				{
					Value = v.Item1,
					Op = v.Item2,
				});

		public static readonly Parser<OrderExpression> OrderBy =
			Token("ORDER").And(Token("BY"))
				.Take(OrderStmt.Separated1(Token(",")))
				.Map(v => new OrderExpression
				{
					Statements = v
				});

		// --- LIMIT PARSERS --------------------------------------------------------------------------

		public static readonly Parser<LimitExpression> Limit =
			Token("LIMIT")
				.Take(Number)
				.Map(v => new LimitExpression
				{
					Amount = v
				});

		// --- QUERY PARSERS --------------------------------------------------------------------------

		public static readonly Parser<QueryExpression> Query =
			Select
				.And(From.Optional())
				.And(Keys.Optional())
				.And(Where.Optional())
				.And(OrderBy.Optional())
				.And(Limit.Optional())
				.Map(Parser.Flatten6)
				.Map(t => new QueryExpression
				{
					Select  = t.Item1,
					From    = t.Item2,
					Keys    = t.Item3,
					Where   = t.Item4,
					OrderBy = t.Item5,
					Limit   = t.Item6,
				});

		static Parsers()
		{
			FunctionPtr.Inject(Function.Parse);
			LogicalPtr.Inject(Logical.Parse);
		}
	}
}

