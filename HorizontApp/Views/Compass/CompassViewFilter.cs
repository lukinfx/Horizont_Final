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