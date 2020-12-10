using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RollsEngine.Newtonsoft
{
    internal class ComparableToken : IComparable
    {
        public ComparableToken(JToken t) =>
            (Token, Comparable, Primitive) = (t
                , new Lazy<IComparable>(() => ToComparable(t))
                , new Lazy<object>(() => ToPrimitive(t)));

        public readonly JToken Token;
        public readonly Lazy<IComparable> Comparable;
        public readonly Lazy<object> Primitive;

        public virtual int CompareTo(object obj)
        {
            var a = Comparable?.Value;
            var b = (obj as ComparableToken)?.Comparable?.Value ?? obj;
            var result = a?.CompareTo(b) ?? 1;

            return result;
        }

        public static IComparable Create(JToken token) =>
            new ComparableToken(token);

        protected static IComparable ToComparable(JToken token) =>
            token == null                      ? null
            : token.Type == JTokenType.Boolean ? token.Value<bool>()
            : token.Type == JTokenType.Date    ? token.Value<DateTime>()
            : token.Type == JTokenType.Float   ? token.Value<decimal>()
            : token.Type == JTokenType.Guid    ? token.Value<Guid>()
            : token.Type == JTokenType.Integer ? token.Value<decimal>()
            : token.Type == JTokenType.String  ? token.Value<string>()
            : (IComparable) null;

        protected static object ToPrimitive(JToken token) =>
            token == null                      ? null
            : token.Type == JTokenType.Boolean ? token.Value<bool>()
            : token.Type == JTokenType.Array   ? (token as JArray)?.Values()?.Select(ToPrimitive)?.ToArray()
            : token.Type == JTokenType.Date    ? token.Value<DateTime>()
            : token.Type == JTokenType.Float   ? token.Value<decimal>()
            : token.Type == JTokenType.Guid    ? token.Value<Guid>()
            : token.Type == JTokenType.Integer ? token.Value<decimal>()
            : token.Type == JTokenType.String  ? token.Value<string>()
            : (object) null;
    }

    internal class NonComparableToken : ComparableToken
    {
        public NonComparableToken(JToken t) : base(t) {}

        public override int CompareTo(object obj) =>
            1;
    }
}