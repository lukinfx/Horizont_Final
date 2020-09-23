using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.Models;

namespace HorizontApp.Utilities
{
    public class GpsUtils
    {
        private static readonly double MIN_LAT = Dg2Rad(-90d); // -PI/2
        private static readonly double MAX_LAT = Dg2Rad(90d); //  PI/2
        private static readonly double MIN_LON = Dg2Rad(-180d); // -PI
        private static readonly double MAX_LON = Dg2Rad(180d); //  PI

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

        public static GpsLocation ConvertFromXamarin(Xamarin.Essentials.Location loc)
        {
            GpsLocation converted = new GpsLocation();

            converted.Altitude = loc.Altitude.Value;
            converted.Longitude = loc.Longitude;
            converted.Latitude = loc.Latitude;
            return converted;
            ;
        }

        public static bool HasAltitude(GpsLocation loc)
        {
            return (loc.Altitude < -0.01 || loc.Altitude > 0.01);
        }

        public static double Distance(GpsLocation loc1, GpsLocation loc2)
        {
            return Convert(loc1).DistanceTo(Convert(loc2));
        }
        public static double QuickDistance(GpsLocation loc1, GpsLocation loc2)
        {
            double x = 0;
            double x1 = Math.PI * (loc1.Latitude/360) * 12713500;
            double x2 = Math.PI * (loc2.Latitude/360) * 12713500;
            double y1 = Math.Cos(loc1.Latitude * Math.PI / 180) * (loc1.Longitude/360) * 40075000;
            double y2 = Math.Cos(loc2.Latitude * Math.PI / 180) * (loc2.Longitude/360) * 40075000;
            x = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));

            return x;
        }

        public static double Dg2Rad(double degrees)
        {
            return (Math.PI / 180) * degrees;
        }

        public static double Rad2Dg(double radian)
        {
            return radian * (180.0 / Math.PI);
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

        public static double VerticalAngle(GpsLocation c1, GpsLocation c2)
        {
            var dist = Distance(c1, c2);
            return VerticalAngle((c2.Altitude - c1.Altitude), dist);
        }

        public static double VerticalAngle(double altDif, double distance)
        {
            var x = Math.Atan(altDif / distance);
            return Rad2Dg(x);
        }

        public static void BoundingRect(GpsLocation loc, double distance, out GpsLocation min, out GpsLocation max)
        {
            var radLat = Dg2Rad(loc.Latitude);
            var radLon = Dg2Rad(loc.Longitude);

            // angular distance in radians on a great circle
            double radDist = distance / 6371.01;
            double minLat = radLat - radDist;
            double maxLat = radLat + radDist;

            double minLon, maxLon;
            if (minLat > MIN_LAT && maxLat < MAX_LAT)
            {
                double deltaLon = Math.Asin(Math.Sin(radDist) / Math.Cos(radLat));
                minLon = radLon - deltaLon;
                if (minLon < MIN_LON) minLon += 2d * Math.PI;
                maxLon = radLon + deltaLon;
                if (maxLon > MAX_LON) maxLon -= 2d * Math.PI;
            }
            else
            {
                // a pole is within the distance
                minLat = Math.Max(minLat, MIN_LAT);
                maxLat = Math.Min(maxLat, MAX_LAT);
                minLon = MIN_LON;
                maxLon = MAX_LON;
            }

            min = new GpsLocation()
            {
                Latitude = Rad2Dg(minLat),
                Longitude = Rad2Dg(minLon)
            };
            max = new GpsLocation()
            {
                Latitude = Rad2Dg(maxLat),
                Longitude = Rad2Dg(maxLon)
            };
        }
    }
}