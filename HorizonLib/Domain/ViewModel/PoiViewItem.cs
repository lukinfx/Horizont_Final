using HorizonLib.Domain.Enums;
using HorizontLib.Domain.Models;

namespace HorizontLib.Domain.ViewModel
{
    public class PoiViewItem
    {
        public Poi Poi;
        public double Bearing;
        public double Distance;
        public Visibility Visibility;
        public float AltitudeDifference { get; set; }

        public GpsLocation GpsLocation
        {
            get
            {
                return new GpsLocation() { Latitude = this.Poi.Latitude, Longitude = this.Poi.Longitude, Altitude = this.Poi.Altitude };
            }
        }

        

        public PoiViewItem(Poi poi) 
        {
            Poi = poi;
        }
    }
}