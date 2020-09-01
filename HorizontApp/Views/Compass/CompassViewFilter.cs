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

        bool IsOverlapping(double item1, double item2, double minDiffLeft, double minDiffRight)
        {
            if (item1 > item2)
            {
                if (Math.Abs(CompassUtils.GetAngleDiff(item1, item2)) < minDiffLeft)
                {
                    return true;
                }
            }
            if (item1 < item2)
            {
                if (Math.Abs(CompassUtils.GetAngleDiff(item1, item2)) < minDiffRight)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Filter(PoiViewItem item, double minDiffLeft, double minDiffRight)
        {
            foreach (var heading in _headings)
            {
                if (IsOverlapping(heading.Bearing, item.Bearing, minDiffLeft, minDiffRight))
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