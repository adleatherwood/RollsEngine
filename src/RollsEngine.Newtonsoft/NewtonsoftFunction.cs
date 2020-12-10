using System;
using Newtonsoft.Json.Linq;

namespace RollsEngine.Newtonsoft
{
    internal class NewtonsoftFunction : INewtonsoftFunction
    {
        public IComparable Execute(JObject document, string name, object[] parameters)
        {
            throw new NotImplementedException($"Function '{name}' is not implemented");
        }
    }
}