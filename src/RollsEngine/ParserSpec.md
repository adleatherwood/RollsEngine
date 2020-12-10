# Query Language Specification

### Conventions

	|   alternation
	()  grouping
	[]  option (zero or one time)
	{}  repetition (any number of times)
	\   literal expression
	*   circular reference
	regex ...sometimes it's just easier


# Basic Parsers

NumberLiteral:

	regex [+-]?(\d*\.)?\d+

TextLiteral:

	regex ["|'][^\n\r]*["|']

BoolLiteral:

	"TRUE" | "FALSE"

NullLiteral:

	"NULL"

LiteralExpression:

	| NumberLiteral
	| TextLiteral
	| BoolLiteral
	| NullLiteral

Identifier:

	regex [\w\-]+

JsonPath: (no spaces)

	regex \$.*[,| FROM]

ValueExpression:

	| JsonPath
	| LiteralExpression
	| FuncExpression*

# Function Parsers

FuncExpression:

	Identifier "(" [ ValueExpression { "," ValueExpression } ] ")"

# Comparison Parsers

CompareOperator:

	">" | ">=" | "=" | "!=" | "<=" | "<"

CompareExpression:

	ValueExpression [ CompareOperator ValueExpression ]

# Logical Parsers


LogicalFactor:

	CompareExpression | "(" LogicalExpression* ")"

LogicalArgument:

	[ "NOT" ] LogicalFactor

LogicalTerm:

	LogicalArgument { "AND" LogicalArgument  }

LogicalExpression:

	LogicalTerm { "OR" LogicalTerm }

# Aggregate Parsers

AggregateFunction

	[ ( Identifier | Text ) ":" ] ( COUNT | SUM | MIN | MAX | AVG ) "(" ValueExpression ")" [ "AS" ( Identifier | Text ) ]

AggregateFunctions

	AggregateFunction { "," AggregateFunction }

# Select Parsers

SelectStatement:

	[ ( Identifier | Text ) ":" ] ValueExpression [ "AS" ( Identifier | Text ) ]

SelectStatments:

	ValueExpression { "," SelectStatement }

SelectExpression:

	"SELECT" [ "{" ] (AggregateExpression | SelectStatements )  [ "}" ]

# From Parsers

FromExpression:

	"FROM" Identifier

# Key Parsers

KeysExpression:

	"KEYS" LiteralExpression { "," LiteralExpression }

# Where Parsers

WhereExpression: (post-query)

	"WHERE" LogicalExpression

# Order Parsers

OrderOperator:

	"ASC" | "DESC"

OrderStatement:

	ValueExpression [ OrderOperator ]

OrderExpression:

	"ORDER" "BY" OrderStatement { "," OrderStatement }

# Limit Parsers

LimitExpression:

	"LIMIT" NumberLit

# Query Parsers

QueryExpression:

	SelectExpression FromExpression [ KeysExpression ] [ WhereExpression ] [ OrderExpression ] [ LimitExpression ] ";"

QueriesExpression:

	QueryExpression { ";" QueryExpression }

# Examples

	SELECT $.property, $.bids[@.price>100000]
	FROM some-index
	KEYS '123', ['456', '789']
	WHERE $.property = 'abc'
	ORDER BY $.property
	LIMIT 10