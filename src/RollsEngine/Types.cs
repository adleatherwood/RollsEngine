using FunctionalLink;

namespace RollsEngine
{
	// --- BASIC TYPES --------------------------------------------------------------------------

	public class NumberLiteral
    {
		public decimal Value;
    }

	public class TextLiteral
	{
		public string Value;
	}

	public class BoolLiteral
	{
		public bool Value;
	}

	public class NullLiteral
	{
        public readonly object Value = null;
	}

	public class LiteralExpression1
	{
		public NumberLiteral Number;
		public TextLiteral Text;
		public BoolLiteral Bool;
		public NullLiteral Null;
	}

	public class Identifier
    {
		public string Value;
    }

	public class Path
    {
		public string Value;
    }

	public class ValueExpression1
	{
		public Path Path;
		public LiteralExpression1 Literal;

		public FunctionExpression Function;
	}

	// --- FUNCTION TYPES --------------------------------------------------------------------------

	public class FunctionExpression
    {
		public Identifier Identifier;
		public ValueExpression1[] Parameters;
    }

	// --- COMPARISON TYPES --------------------------------------------------------------------------

	public enum CompareOperator
    {
		Gt, Gte, E, Ne, Lte, Lt
    }

	public class CompareExpression
    {
		public ValueExpression1 Left;
		public Option<(CompareOperator Op, ValueExpression1 Value)> Right;
    }

	// --- LOGICAL TYPES --------------------------------------------------------------------------

	public class LogicalFactor1
	{
		public CompareExpression Comparison;
		public LogicalExpression Expression;
	}

	public class LogicalArgument
	{
		public bool Invert;
		public LogicalFactor1 Factor;
	}

	public class LogicalTerm
	{
		public LogicalArgument Argument;
		public LogicalArgument[] Ands;
	}

	public class LogicalExpression
	{
		public LogicalTerm Term;
		public LogicalTerm[] Ors;
	}

    // --- AGGREGATE TYPES -----------------------------------------------------------------------

    public enum AggregateType { Count, Sum, Min, Max, Avg }

    public class AggregateFunction
    {
        public Option<TextLiteral> Name;
        public AggregateType Function;
        public ValueExpression1 Value;
    }

	// --- SELECT TYPES --------------------------------------------------------------------------

    public class SelectStatement
    {
        public Option<TextLiteral> Name;
        public ValueExpression1 Value;
    }

	public class SelectExpression1
    {
		public SelectStatement[] Statements;
        public AggregateFunction[] Aggregates;
    }

	// --- FROM TYPES --------------------------------------------------------------------------

	public class FromExpression
	{
		public Identifier Name;
	}

	// --- KEYS TYPES --------------------------------------------------------------------------

	public class SimpleKey : TextLiteral
    {
    }

	public class ComplexKey
    {
		public SimpleKey[] Keys;
    }

	public class KeyExpression1
    {
		public SimpleKey SimpleKey;
		public ComplexKey CompundKey;
    }

	public class KeysExpression
    {
		public LiteralExpression1[] Keys;
    }

	// --- WHERE TYPES --------------------------------------------------------------------------

    public class WhereExpression
    {
        public LogicalExpression Expression;
    }

	// --- ORDER TYPES --------------------------------------------------------------------------

    public enum OrderOperator { Asc, Desc }

    public class OrderStatement
    {
        public ValueExpression1 Value;
        public Option<OrderOperator> Op;
    }

    public class OrderExpression
    {
        public OrderStatement[] Statements;
    }

	// --- LIMIT TYPES --------------------------------------------------------------------------

	public class LimitExpression
    {
		public NumberLiteral Amount;
    }

	// --- QUERY TYPES --------------------------------------------------------------------------

	public class QueryExpression
    {
		public SelectExpression1 Select;
		public Option<FromExpression> From;
		public Option<KeysExpression> Keys;
        public Option<WhereExpression> Where;
        public Option<OrderExpression> OrderBy;
		public Option<LimitExpression> Limit;
    }

    public class QueriesExpression
    {
        public QueriesExpression[] Queries;
    }
}