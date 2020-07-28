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

namespace HorizontApp.Views.Compass
{
    class CompassViewFilter
    {
        private PoiViewItemList _headings;
        public bool Filter(PoiViewItem item)
        {
            if (_headings.Count ==0)

            foreach (var heading in _headings)
            {
                if (item.Bearing > heading.Bearing - 10 && item.Bearing < heading.Bearing + 10)
                {
                    return false;
                }
                else
                {
                    _headings.Add(item);
                    return true;
                }
            }
            return true;
        }

        public void SetList(PoiViewItemList list)
        {
            _headings = null;
            _headings = list;
        }
    }
}