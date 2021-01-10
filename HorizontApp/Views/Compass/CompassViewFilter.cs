using System;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Views.Compass
{
    class CompassViewFilter
    {
        private PoiViewItemList _headings = new PoiViewItemList();

        bool IsOverlapping(double item1, double item2, double minDiff)
        {
            return Math.Abs(CompassViewUtils.GetAngleDiff(item1, item2)) < minDiff;
        }

        /// <summary>
        /// Filter items
        /// </summary>
        /// <param name="item"></param>
        /// <param name="minDiff"></param>
        /// <returns>returns true if items are overlapping</returns>
        public bool IsOverlapping(PoiViewItem item, double minDiff)
        {
            foreach (var heading in _headings)
            {
                if (IsOverlapping(heading.GpsLocation.Bearing.Value, item.GpsLocation.Bearing.Value, minDiff))
                {
                    return true;
                }
            }

            _headings.Add(item);
            return false;
        }

        public void Reset()
        {
            _headings.Clear();
        }
    }
}