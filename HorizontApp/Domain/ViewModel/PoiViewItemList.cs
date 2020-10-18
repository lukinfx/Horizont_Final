using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontLib.Domain.Models;
using HorizontApp.Utilities;

namespace HorizontApp.Domain.ViewModel
{
    public class PoiViewItemList : List<PoiViewItem>
    {
        private AppContext Context { get { return AppContext.Instance; } }

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
        public PoiViewItemList(IEnumerable<Poi> poiList, GpsLocation location, double maxDistance, double minAltitude, bool favourite)
        {
            foreach (var item in poiList)
            {
                var poiViewItem = new PoiViewItem(item);
                poiViewItem.Bearing = CompassViewUtils.GetBearing(location, poiViewItem.GpsLocation);
                poiViewItem.AltitudeDifference = CompassViewUtils.GetAltitudeDifference(location, poiViewItem.GpsLocation);
                poiViewItem.Distance = CompassViewUtils.GetDistance(location, poiViewItem.GpsLocation);

                if (favourite && !poiViewItem.Poi.Favorite)
                    continue;

                if (poiViewItem.Distance > maxDistance * 1000)
                    continue;

                if (poiViewItem.Poi.Altitude > 0.1 && poiViewItem.Poi.Altitude < minAltitude)
                    continue;

                if (!AppContext.Instance.Settings.Categories.Contains(poiViewItem.Poi.Category))
                    continue;
                
                Add(poiViewItem);
            }
        }

        public PoiViewItemList(IEnumerable<Poi> poiList, GpsLocation location)
        {
            foreach (var item in poiList)
            {
                var poiViewItem = new PoiViewItem(item);
                poiViewItem.Bearing = CompassViewUtils.GetBearing(location, poiViewItem.GpsLocation);
                poiViewItem.AltitudeDifference = CompassViewUtils.GetAltitudeDifference(location, poiViewItem.GpsLocation);
                poiViewItem.Distance = CompassViewUtils.GetDistance(location, poiViewItem.GpsLocation);
                Add(poiViewItem);
            }
        }
    }
}