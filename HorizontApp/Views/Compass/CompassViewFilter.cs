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
        private PoiViewItemList _headings = new PoiViewItemList();

        bool IsOverlapping(double item1, double item2, double minDiff)
        {
            return Math.Abs(CompassUtils.GetAngleDiff(item1, item2)) < minDiff;
        }

        public bool Filter(PoiViewItem item, double minDiff)
        {
            foreach (var heading in _headings)
            {
                if (IsOverlapping(heading.Bearing, item.Bearing, minDiff))
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