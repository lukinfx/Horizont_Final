using System.Collections.Generic;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;
using HorizontLib.Utilities;

namespace HorizontLib.Domain.ViewModel
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
        public PoiViewItemList(IEnumerable<Poi> poiList, GpsLocation myLocation, double maxDistance, double minAltitude, bool favourite, List<PoiCategory> categories)
        {
            foreach (var item in poiList)
            {
                var poiViewItem = new PoiViewItem(item);
                poiViewItem.Bearing = GpsUtils.QuickBearing(myLocation, poiViewItem.GpsLocation);
                poiViewItem.AltitudeDifference = CompassViewUtils.GetAltitudeDifference(myLocation, poiViewItem.GpsLocation);
                poiViewItem.Distance = GpsUtils.QuickDistance(myLocation, poiViewItem.GpsLocation);

                if (favourite && !poiViewItem.Poi.Favorite)
                    continue;

                if (poiViewItem.Distance > maxDistance * 1000)
                    continue;

                if (poiViewItem.Poi.Altitude > 0.1 && poiViewItem.Poi.Altitude < minAltitude)
                    continue;

                if (!categories.Contains(poiViewItem.Poi.Category))
                    continue;
                
                Add(poiViewItem);
            }
        }

        public PoiViewItemList(IEnumerable<Poi> poiList, GpsLocation myLocation)
        {
            foreach (var item in poiList)
            {
                var poiViewItem = new PoiViewItem(item);
                poiViewItem.Bearing = GpsUtils.QuickBearing(myLocation, poiViewItem.GpsLocation);
                poiViewItem.AltitudeDifference = CompassViewUtils.GetAltitudeDifference(myLocation, poiViewItem.GpsLocation);
                poiViewItem.Distance = GpsUtils.QuickDistance(myLocation, poiViewItem.GpsLocation);
                Add(poiViewItem);
            }
        }
    }
}