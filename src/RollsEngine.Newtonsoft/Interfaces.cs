using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RollsEngine.Newtonsoft
{
    public interface INewtonsoftSource
    {
        IEnumerable<JObject> Read(string from);

        IEnumerable<JObject> Read(string from, string[] keys);
    }

    public interface INewtonsoftFunction
    {
        IComparable Execute(JObject document, string name, object[] parameters);
    }
}