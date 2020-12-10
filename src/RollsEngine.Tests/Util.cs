using System.Diagnostics;

namespace RollsEngine.Tests
{
    public static class Util
    {
        public static void Dump(this object o) {
            Debug.WriteLine(o);
        }
    }
}