using System;
using FunctionalLink;

namespace RollsEngine
{
    static class Util
    {
        public static int Lesser(int a, int b) =>
            a < b ? a : b;
    }

    class OrderComparable<TObject> : IComparable
            where TObject: class
    {
        public OrderComparable(OrderStatement[] s, IDataService<TObject> db, TObject v) =>
            (Statements, Db, Value) = (s, db, v);

        public readonly TObject Value;
        public readonly IDataService<TObject> Db;
        public readonly OrderStatement[] Statements;
        public int CompareTo(object obj)
        {
            var other = (obj as OrderComparable<TObject>)?.Value;

            if (other == null)
                return 1;

            foreach (var statement in Statements)
            {
                var a = statement.Value.Eval(Db, Value);
                var b = statement.Value.Eval(Db, other);

                var result = a?.CompareTo(b) ?? 1;
                var op = statement.Op.ValueOrDefault();

                if (result != 0)
                    return op == OrderOperator.Asc
                        ? result
                        : result * -1;
            }

            return 1;
        }
    }
}