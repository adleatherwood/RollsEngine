using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RollsEngine.Newtonsoft
{
    public class NewtonsoftService : IDataService<JObject>
    {
        private readonly INewtonsoftSource _source;
        private readonly INewtonsoftFunction _funcs;

        public NewtonsoftService(INewtonsoftSource source) =>
            (_source, _funcs) = (source, new NewtonsoftFunction());
        public NewtonsoftService(INewtonsoftSource source, INewtonsoftFunction funcs) =>
            (_source, _funcs) = (source, funcs);

        public JObject Add(JObject document, string name, object value)
        {
            if (value != null)
            {
                var unboxed = (value as ComparableToken)?.Token;

                if (unboxed != null)
                    document[name] = unboxed;
                else
                    document[name] = JToken.FromObject(value);
            }

            return document;
        }

        public IComparable Execute(JObject document, string name, object[] parameters)
        {
            IEnumerable<T> toEnum<T>(int index) => Unbox<IEnumerable<object>>(index, parameters).Cast<T>();
            string toString(int index)          => Unbox<string>(index, parameters);
            int toInt(int index)                => (int) As<decimal>(index, parameters);
            bool toBool(int index)              => As<bool>(index, parameters);

            switch(name.ToLower())
            {
                case "sub":    return parameters.Length == 3
                    ? toString(0)?.Substring(toInt(1), toInt(2))
                    : toString(0)?.Substring(toInt(1));
                case "rtrim":  return toString(0)?.TrimEnd();
                case "ltrim":  return toString(0)?.TrimStart();
                case "trim":   return toString(0)?.Trim();
                case "upper":  return toString(0)?.ToUpper();
                case "lower":  return toString(0)?.ToLower();
                case "starts": return parameters.Length == 3
                    ? toString(0)?.StartsWith(toString(1), toBool(2), null) ?? false
                    : toString(0)?.StartsWith(toString(1)) ?? false;
                case "split":  return new NonComparableToken(new JArray(toString(0)?.Split(toString(1)) ?? new string[0]));
                case "join":   return String.Join(toString(0), toEnum<string>(1));

                case "any":    return toEnum<object>(0)?.Any() ?? false;
                case "first":  return toEnum<IComparable>(0)?.FirstOrDefault();
                case "last":   return toEnum<IComparable>(0)?.LastOrDefault();
                case "at":     return toEnum<IComparable>(0)?.ElementAt(toInt(1));
                case "length": return toEnum<object>(0)?.Count();

                case "now":    return DateTime.Now.ToString("o");
                case "utc":    return DateTime.UtcNow.ToString("o");
                case "year":   return DateTime.TryParse(toString(0), out var year)   ? year.Year     : -1;
                case "month":  return DateTime.TryParse(toString(0), out var month)  ? month.Month   : -1;
                case "day":    return DateTime.TryParse(toString(0), out var day)    ? day.Day       : -1;
                case "hour":   return DateTime.TryParse(toString(0), out var hour)   ? hour.Hour     : -1;
                case "minute": return DateTime.TryParse(toString(0), out var minute) ? minute.Minute : -1;
                case "second": return DateTime.TryParse(toString(0), out var second) ? second.Second : -1;
                default:
                    return _funcs.Execute(document, name, parameters);
            }
        }

        public JObject New() =>
            new JObject();

        public decimal Number(IComparable comparable)
        {
            var unboxed = Unbox<object>(comparable).ToString();
            return  Decimal.TryParse(unboxed, out var parsed)
                ? parsed
                : 0;
        }

        public IComparable Path(JObject document, string path)  {
            var isArray = path.Last() == ']';
            var token = isArray
                ? new NonComparableToken(new JArray(document.SelectTokens(path)))
                : new ComparableToken(document.SelectToken(path));

            return token;
        }

        public IEnumerable<JObject> Read(string from) =>
            _source.Read(from);

        public IEnumerable<JObject> Read(string from, string[] keys) =>
            _source.Read(from, keys);

        private static T Unbox<T>(int index, object[] parameters)
            where T: class
        {
            var target = index >= parameters.Length ? null : parameters[index];
            var result = Unbox<T>(target);

            return result;
        }

        private static T Unbox<T>(object target)
            where T: class
        {
            var result =
                   ((target as ComparableToken)?.Primitive?.Value)
                ?? ((target as NonComparableToken)?.Primitive?.Value)
                ?? target;

            return result as T;
        }

        private static T As<T>(int index, object[] parameters)
            where T: struct
        {
            var target = index >= parameters.Length ? null : parameters[index];
            var result = As<T>(target);

            return result;
        }

        private static T As<T>(object target)
            where T: struct
        {
            var result = ((target as ComparableToken)?.Primitive?.Value) ?? (target);

            return (T) result;
        }
    }
}
