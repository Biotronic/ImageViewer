using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
