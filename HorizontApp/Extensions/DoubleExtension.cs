using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HorizontApp.Extensions
{
    public static class DoubleExtension
    {
        public static bool IsEqual(this double val1, double val2, double precision)
        {
            return Math.Abs(val1-val2) < precision;
        }

    }
}