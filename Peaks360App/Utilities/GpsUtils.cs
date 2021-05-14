using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;

namespace Peaks360App.Utilities
{
    public class GpsUtilities : IGpsUtilities
    {
        public double Distance(GpsLocation loc1, GpsLocation loc2)
        {
            return GpsUtils.Distance(loc1, loc2);
        }
        public double Bearing(GpsLocation loc1, GpsLocation loc2)
        {
            return GpsUtils.Bearing(loc1, loc2);
        }
    }


    public class GpsUtils : Peaks360Lib.Utilities.GpsUtils
    {
        public static Android.Locations.Location Convert(GpsLocation loc)
        {
            Android.Locations.Location converted = new Android.Locations.Location("");

            converted.Altitude = loc.Altitude;
            converted.Longitude = loc.Longitude;
            converted.Latitude = loc.Latitude;
            return converted;
        }

        public static GpsLocation Convert(Android.Locations.Location loc)
        {
            GpsLocation converted = new GpsLocation();

            converted.Altitude = loc.Altitude;
            converted.Longitude = loc.Longitude;
            converted.Latitude = loc.Latitude;
            return converted;
        }

        public static GpsLocation Convert(Xamarin.Essentials.Location loc)
        {
            GpsLocation converted = new GpsLocation();

            converted.Altitude = loc.Altitude ?? 0;
            converted.Longitude = loc.Longitude;
            converted.Latitude = loc.Latitude;
            return converted;
        }

        public static GpsLocation ConvertFromXamarin(Xamarin.Essentials.Location loc)
        {
            GpsLocation converted = new GpsLocation();

            converted.Altitude = loc.Altitude.Value;
            converted.Longitude = loc.Longitude;
            converted.Latitude = loc.Latitude;
            return converted;
            ;
        }

        public static double Distance(GpsLocation loc1, GpsLocation loc2)
        {
            return Convert(loc1).DistanceTo(Convert(loc2));
        }

        public static double Bearing(GpsLocation loc1, GpsLocation loc2)
        {
            return Convert(loc1).BearingTo(Convert(loc2));
        }

        public static double VerticalAngle(GpsLocation c1, GpsLocation c2)
        {
            var dist = Distance(c1, c2);
            return VerticalAngle((c2.Altitude - c1.Altitude), dist);
        }
    }
}