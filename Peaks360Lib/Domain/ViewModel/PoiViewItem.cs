using System;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;

namespace Peaks360Lib.Domain.ViewModel
{
    public class PoiViewItem
    {
        public Poi Poi;
        public Visibility Visibility;
        public bool Overlapped;
        public bool Selected;
        public float AltitudeDifference { get; set; }

        public GpsLocation GpsLocation { get; private set; }

        public PoiViewItem(Poi poi)
        {
            Poi = poi;
            GpsLocation = new GpsLocation() { Latitude = this.Poi.Latitude, Longitude = this.Poi.Longitude, Altitude = this.Poi.Altitude };
        }

        public int Priority
        {
            get { return (int)Visibility; }
        }

        public double VerticalViewAngle
        {
            get { return GpsUtils.Rad2Dg(Math.Atan(AltitudeDifference / GpsLocation.Distance.Value)); }
        }

        public bool IsImportant()
        {
            return !string.IsNullOrEmpty(Poi.Wikidata) || !string.IsNullOrEmpty(Poi.Wikipedia);
        }

        public bool IsFullyVisible()
        {
            return Visibility == Visibility.Visible;
        }
    }
}