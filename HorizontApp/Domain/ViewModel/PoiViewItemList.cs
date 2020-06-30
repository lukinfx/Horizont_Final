﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.Models;
using HorizontApp.Utilities;

namespace HorizontApp.Domain.ViewModel
{
    public class PoiViewItemList : List<PoiViewItem>
    {
        public PoiViewItemList()
        { 
        }

        /// <summary>
        /// Creates PoiViewItemList with distance calcalted from current location, and filters-out items not matching maxDistance and minAltitude parameters.
        /// </summary>
        /// <param name="poiList">List of Pois</param>
        /// <param name="location">My current location</param>
        /// <param name="maxDistance">Max distance in kilometers</param>
        /// <param name="minAltitude">Min altitude in meters</param>
        public PoiViewItemList(IEnumerable<Poi> poiList, GpsLocation location, double maxDistance = 99999, double minAltitude = 0)
        {
            foreach (var item in poiList)
            {
                var poiViewItem = new PoiViewItem(item);
                poiViewItem.Bearing = CompassViewUtils.GetBearing(location, poiViewItem.GpsLocation);
                poiViewItem.Distance = CompassViewUtils.GetDistance(location, poiViewItem.GpsLocation);

                if ((poiViewItem.Distance < maxDistance * 1000) && (poiViewItem.Altitude > minAltitude * 10))
                {
                    Add(poiViewItem);
                }
            }
        }
    }
}