using HorizonLib.Domain.Enums;
using HorizontLib.Domain.Models;

namespace HorizontLib.Domain.ViewModel
{
    public class PoiViewItem
    {
        public Poi Poi;
        public Visibility Visibility;
        public bool Overlapped;
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

    }
}