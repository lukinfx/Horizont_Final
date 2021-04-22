﻿using System.Collections.Generic;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;

namespace Peaks360Lib.Domain.ViewModel
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
        /// <param name="minAltitude">Min altitude (progress 100 = 1600m)</param>
        public PoiViewItemList(IEnumerable<Poi> poiList, GpsLocation myLocation, double maxDistance, bool favourite, List<PoiCategory> categories)
        {
            foreach (var item in poiList)
            {
                var poiViewItem = new PoiViewItem(item);
                poiViewItem.GpsLocation.Bearing = GpsUtils.QuickBearing(myLocation, poiViewItem.GpsLocation);
                poiViewItem.AltitudeDifference = CompassViewUtils.GetAltitudeDifference(myLocation, poiViewItem.GpsLocation);
                poiViewItem.GpsLocation.Distance = GpsUtils.QuickDistance(myLocation, poiViewItem.GpsLocation);
                poiViewItem.GpsLocation.GetVerticalViewAngle(myLocation);

                if (favourite && !poiViewItem.Poi.Favorite)
                    continue;

                if (poiViewItem.GpsLocation.Distance > maxDistance * 1000)
                    continue;

                if (!categories.Contains(poiViewItem.Poi.Category))
                    continue;
                
                Add(poiViewItem);
            }
        }

        public PoiViewItemList(IEnumerable<Poi> poiList, GpsLocation myLocation)
        {
            if (poiList != null)
            {
                foreach (var item in poiList)
                {
                    var poiViewItem = new PoiViewItem(item);
                    poiViewItem.GpsLocation.Bearing = GpsUtils.QuickBearing(myLocation, poiViewItem.GpsLocation);
                    poiViewItem.AltitudeDifference = CompassViewUtils.GetAltitudeDifference(myLocation, poiViewItem.GpsLocation);
                    poiViewItem.GpsLocation.Distance = GpsUtils.QuickDistance(myLocation, poiViewItem.GpsLocation);
                    poiViewItem.GpsLocation.GetVerticalViewAngle(myLocation);
                    Add(poiViewItem);
                }
            }
        }
    }
}