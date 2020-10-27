using System;
using HorizontLib.Domain.Models;

namespace PaintSkyLine
{
    public class GeoPoint
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double Distance;
        public double Bearing;
        public double VerticalAngle;
    
        private static readonly double MIN_LAT = Dg2Rad(-90d); // -PI/2
        private static readonly double MAX_LAT = Dg2Rad(90d); //  PI/2
        private static readonly double MIN_LON = Dg2Rad(-180d); // -PI
        private static readonly double MAX_LON = Dg2Rad(180d); //  PI

        public GeoPoint(double latitude, double longitude, double alt, GpsLocation myLocation = null)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = (UInt32) alt;
              
            if (myLocation != null)
            {
                Distance = myLocation.QuickDistance(myLocation);
                Bearing = myLocation.QuickBearing(myLocation);
                VerticalAngle = GetVerticalAngle(Distance, alt - myLocation.Altitude);
            }
        }

        public static bool IsAngleBetween(double angle, double heading, double limit)
        {
            var d = Normalize180(angle - heading);
            return Math.Abs(d) < limit;
        }

        public double DegreeBearing(GeoPoint c2)
        {
            var dLon = Dg2Rad(c2.Longitude - this.Longitude);
            var dPhi = Math.Log(Math.Tan(Dg2Rad(c2.Latitude) / 2 + Math.PI / 4) / Math.Tan(Dg2Rad(this.Latitude) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            return ToBearing(Math.Atan2(dLon, dPhi));
        }


        public static double GetVerticalAngle(double distance, double altDif)
        {
            var x = Math.Atan(altDif / distance);
            return Rad2Dg(x);
        }

        public static double Dg2Rad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        public static double Rad2Dg(double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static double Normalize180(double angle)
        {
            var x = Normalize360(angle);
            if (x > 180)
                return x - 360;
            return x;
        }
        public static double Normalize360(double angle)
        {
            var x = angle - Math.Floor(angle / 360) * 360;
            return x;
        }

        public static double ToBearing(double radians)
        {
            // convert radians to degrees (as bearing: 0...360)
            return (Rad2Dg(radians) + 360) % 360;
        }
    }
}
