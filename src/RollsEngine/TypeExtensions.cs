using System;
using System.Collections.Generic;
using System.Linq;
using FunctionalLink;

namespace RollsEngine
{
    public static partial class Types
    {
        public static string ToText(this NumberLiteral v) =>
            v.Value.ToString();

        public static string ToText(this TextLiteral v) =>
            $"'{v.Value}'";

        public static string ToText(this BoolLiteral v) =>
            v.Value ? "true" : "false";

        public static string ToText(this NullLiteral v) =>
            "null";

        public static string ToText(this LiteralExpression1 v) =>
              v.Number != null ? v.Number.ToText()
            : v.Text != null   ? v.Text.ToText()
            : v.Bool != null   ? v.Bool.ToText()
            : v.Null.ToText();

        public static string ToText(this ValueExpression1 v) =>
              v.Path != null    ? v.Path.Value
            : v.Literal != null ? v.Literal.ToText()
            : v.Function.ToText();

        public static string ToText(this FunctionExpression v) =>
            v.Identifier.Value + "(" +
                String.Join(',', v.Parameters.Select(ToText)) + ")";

        public static string ToText(this CompareExpression v) =>
            v.Left.ToText()  + v.Right.Select(t => {
                var op = t.Op == CompareOperator.Gt ? ">"
                    : t.Op == CompareOperator.Gte   ? ">="
                    : t.Op == CompareOperator.E     ? "="
                    : t.Op == CompareOperator.Ne    ? "!="
                    : t.Op == CompareOperator.Lte   ? "<="
                    : "<";
                return $" {op} {t.Value.ToText()}";
                }).SingleOrDefault();

        public static string ToText(this LogicalFactor1 v) =>
              v.Comparison != null ? v.Comparison.ToText()
            : v.Expression.ToText();

        public static string ToText(this LogicalArgument v) =>
            (v.Invert ? "NOT " : "") + v.Factor.ToText();

        public static string ToText(this LogicalTerm v) =>
            v.Argument.ToText() +
                (v.Ands.Length > 0
                ? " AND " + String.Join(" AND ", v.Ands.Select(ToText))
                : "");

        public static string ToText(this LogicalExpression v) =>
            "(" + v.Term.ToText() +
                (v.Ors.Length > 0
                ? " OR " + String.Join(" OR ", v.Ors.Select(ToText))
                : "") + ")";

        public static string ToText(this SelectStatement v) =>
            (v.Name.Map(n => $"'{n}' : ").FirstOrDefault() ?? "") + v.Value.ToText();

        public static string ToText(this SelectExpression1 v) =>
            "SELECT " + String.Join(", ", v.Statements.Select(ToText));
    }

    public static partial class Type
    {
        public static IComparable ToObject(this LiteralExpression1 l) =>
              l.Number != null ? l.Number.Value
            : l.Text != null ? l.Text.Value
            : l.Bool != null ? l.Bool.Value
            : (IComparable) null;

        public static TObject ToObject<TObject>(this Dictionary<string,decimal> d, IDataService<TObject> db)
        {
            var result = db.New();

            foreach(var key in d.Keys.Where(k => k[0] != '!')) {
                var v = d[key];
                // note: this is what the user would expect to see if there are no results
                //       it's a little disconnected though
                db.Add(result, key, v == decimal.MaxValue ? 0 : v);
            }

            return result;
        }
    }

    public static partial class Type
    {
        public static string ImpliedName(this Path p)
        {
            var n = p.Value;
            var i1 = n.LastIndexOf('[');

            n = i1 == -1 ? n : n.Substring(0, i1);

            var i2 = n.LastIndexOf('.');

            n = i2 == -1 ? n : n.Substring(i2 + 1);

            return n;
        }

        public static string ImpliedName(this SelectStatement s) =>
            // explicit statement given name
            // implicit function name
            // index of statement as name
            s.Name
                .Map(n => n.Value)
                .SingleOrDefault()
                ?? s.Value.Function?.ImpliedName()
                ?? s.Value.Path?.ImpliedName();

        public static string ImpliedName(this FunctionExpression f) =>
            f.Identifier.Value;

        public static string ImpliedName(this AggregateFunction a) =>
            // explicit statement given name
            // implicit function name
            // index of statement as name
            a.Name
                .Map(n => n.Value)
                .SingleOrDefault()
                ?? a.Function.ToString() + "(" + a.Value.Path?.ImpliedName() + ")";
    }

    public static partial class Type
    {
        public static IComparable Eval<TObject>(this ValueExpression1 v, IDataService<TObject> db, TObject o)
        {
            return v.Literal != null
                ? v.Literal.ToObject()
                : v.Path != null
                    ? db.Path(o, v.Path.Value)
                    : db.Execute(o, v.Function.Identifier.Value,
                        v.Function.Parameters
                            .Select(p => p.Eval(db, o)).ToArray());
        }

        public static bool Eval(this IComparable a, CompareOperator op, IComparable b)
        {
            switch(op)
            {
                case CompareOperator.E: return a?.CompareTo(b) == 0;
                case CompareOperator.Ne: return a?.CompareTo(b) != 0;
                case CompareOperator.Gt: return a?.CompareTo(b) > 0;
                case CompareOperator.Gte: return a?.CompareTo(b) >= 0;
                case CompareOperator.Lt: return a?.CompareTo(b) < 0;
                case CompareOperator.Lte: return a?.CompareTo(b) <= 0;
                default: return false;
            }
        }

        public static bool Eval<TObject>(this CompareExpression c, IDataService<TObject> db, TObject o)
        {
            var left = c.Left.Eval(db, o);

            var op = c.Right.Match(
                some => some.Op,
                none => CompareOperator.E);

            var right = c.Right.Match(
                some => some.Value.Eval(db, o),
                none => null);

            var result = c.Right.IsNone()
                ? Convert.ToBoolean(left)
                : left.Eval(op, right);

            return result;
        }

        public static bool Eval<TObject>(this LogicalFactor1 f, IDataService<TObject> db, TObject o)
        {
            var result = f.Comparison != null
                ? f.Comparison.Eval(db, o)
                : f.Expression.Eval(db, o);

            return result;
        }

        public static bool Eval<TObject>(this LogicalArgument a, IDataService<TObject> db, TObject o)
        {
            var result = a.Factor.Comparison != null
                ? a.Factor.Comparison.Eval(db, o)
                : a.Factor.Expression.Eval(db, o);

            return a.Invert
                ? !result
                : result;
        }

        public static bool Eval<TObject>(this LogicalTerm term, IDataService<TObject> db, TObject o)
        {
            var result =
                   term.Argument.Eval(db, o)
                && (term.Ands.Length == 0 || term.Ands.All(a => a.Eval(db, o)));

            return result;
        }

        public static bool Eval<TObject>(this LogicalExpression e, IDataService<TObject> db, TObject o)
        {
            var result =
                   e.Term.Eval(db, o)
                || e.Ors.Any(a => a.Eval(db, o));

            return result;
        }

        public static Func<TObject, bool> Eval<TObject>(this WhereExpression where, IDataService<TObject> db) =>
            (TObject d) => where.Expression.Eval(db, d);

        public static Func<TObject, TObject> Eval<TObject>(this SelectStatement[] statements, IDataService<TObject> db) =>
            (TObject d) => {
                var r = db.New();
                var c = 0;
                foreach(var statement in statements)
                {
                    var n = statement.ImpliedName() ?? c.ToString();
                    var v = statement.Value;

                    if (v.Literal != null)
                        db.Add(r, n, v.Literal.ToObject());
                    else if (v.Path != null)
                        db.Add(r, n, db.Path(d, v.Path.Value));
                    else
                        db.Add(r, n, v.Eval(db, d));

                    c++;
                }
                return r;
            };

        public static Func<Dictionary<string, decimal>, TObject, Dictionary<string, decimal>> Eval<TObject>(this AggregateFunction[] aggregates, IDataService<TObject> db)
        {
            return (accumulate, d) => {
                var c = 0;

                foreach(var aggregate in aggregates)
                {
                    var n = aggregate.ImpliedName();
                    var v1 = aggregate.Value.Eval(db, d);
                    var v = db.Number(v1);

                    switch(aggregate.Function)
                    {
                        case AggregateType.Avg: {
                            accumulate[$"!{n}_count"] += 1;
                            accumulate[$"!{n}_sum"] = accumulate[$"!{n}_sum"] + v;
                            accumulate[n] = accumulate[$"!{n}_sum"] / accumulate[$"!{n}_count"];
                            break;
                        }
                        case AggregateType.Count: {
                            accumulate[n] += 1;
                            break;
                        }
                        case AggregateType.Max: {
                            if (accumulate[n] < v) accumulate[n] = v;
                            break;
                        }
                        case AggregateType.Min: {
                            if (accumulate[n] > v) accumulate[n] = v;
                            break;
                        }
                        case AggregateType.Sum: {
                            accumulate[n] += v;
                            break;
                        }
                    }
                    c++;
                }
                return accumulate;
            };
        }

        public static Func<TObject, IComparable> Eval<TObject>(this OrderExpression order, IDataService<TObject> db)
            where TObject: class =>
            (TObject d) => new OrderComparable<TObject>(order.Statements, db, d);
    }

    public static partial class Type
    {
        // note: wish this could more closely connected with the AggregateFunction
        //       Eval implementation
        public static Dictionary<string, decimal> Init(this AggregateFunction[] aggregates)
        {
            var accumulate = new Dictionary<string, decimal>();
            var c = 0;

            foreach(var aggregate in aggregates)
            {
                var n = aggregate.ImpliedName();

                switch(aggregate.Function)
                {
                    case AggregateType.Avg: {
                        accumulate[$"!{n}_count"] = 0;
                        accumulate[$"!{n}_sum"] = 0;
                        accumulate[n] = 0;
                        break;
                    }
                    case AggregateType.Min: {
                        accumulate[n] = decimal.MaxValue;
                        break;
                    }
                    default:
                        accumulate[n] = 0;
                        break;
                }
                c++;
            }
            return accumulate;
        }
    }
}