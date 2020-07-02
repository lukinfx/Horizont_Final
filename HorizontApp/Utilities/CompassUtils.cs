using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace HorizontApp.Utilities
{
    public class CompassUtils
    {
        public static double GetDiff(double alfa, double beta)
        {
            if (alfa > beta && Math.Abs(alfa - beta) > 180)
            {
                return alfa - beta - 360;
            }
            else if (alfa < beta && Math.Abs(alfa - beta) > 180)
            {
                return 360 - beta + alfa;
            }
            else return alfa - beta;
        }
    }
}