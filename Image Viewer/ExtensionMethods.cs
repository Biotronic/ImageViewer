using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageViewer
{
    internal static class ExtensionMethods
    {
        public static T FirstOrDefault<T>(this IEnumerable<T> a, Func<T, bool> pred, T defaultValue)
        {
            foreach (var v in a)
            {
                if (pred(v))
                {
                    return v;
                }
            }
            return defaultValue;
        }

        public static T LastOrDefault<T>(this IEnumerable<T> a, Func<T, bool> pred, T defaultValue)
        {
            foreach (var v in a.Reverse())
            {
                if (pred(v))
                {
                    return v;
                }
            }
            return defaultValue;
        }
    }
}
