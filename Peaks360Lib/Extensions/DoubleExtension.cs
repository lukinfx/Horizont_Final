using System;

namespace Peaks360Lib.Extensions
{
    public static class DoubleExtension
    {
        public static bool IsEqual(this double val1, double val2, double precision)
        {
            return Math.Abs(val1-val2) < precision;
        }

    }
}