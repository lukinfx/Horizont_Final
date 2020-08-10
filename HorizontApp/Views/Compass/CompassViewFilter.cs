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
using HorizontApp.Domain.ViewModel;
using HorizontApp.Utilities;

namespace HorizontApp.Views.Compass
{
    class CompassViewFilter
    {
        private static int itemAngle = 7;
        private PoiViewItemList _headings = new PoiViewItemList();

        bool IsOverlapping(double item1, double item2)
        {
            return Math.Abs(CompassUtils.GetAngleDiff(item1, item2)) < itemAngle;
        }

        public bool Filter(PoiViewItem item)
        {
            foreach (var heading in _headings)
            {
                if (IsOverlapping(heading.Bearing, item.Bearing))
                if (item.Bearing > heading.Bearing - 10 && item.Bearing < heading.Bearing + 10)
                {
                    return false;
                }
            }

            _headings.Add(item);
            return true;
        }

        public void Reset()
        {
            _headings.Clear();
        }
    }
}