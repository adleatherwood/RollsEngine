using System;
using System.Collections.Generic;

namespace RollsEngine
{
    public interface IDataService<TObject>
    {
        TObject New();

        decimal Number(IComparable comparable);

        IComparable Path(TObject document, string path);

        TObject Add(TObject document, string name, object value);

        IEnumerable<TObject> Read(string from);

        IEnumerable<TObject> Read(string from, string[] keys);

        IComparable Execute(TObject document, string name, object[] parameters);
    }
}