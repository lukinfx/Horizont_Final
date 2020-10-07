using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
{
    public class GpsUtils
    {
        private static readonly double EARTH_MERIDIAN_LENGTH_M = 40075004;
        private static readonly double EARTH_POLAR_LENGTH_M = 39940638;

        private static readonly double EARTH_POLAR_RADIUS = 12713500;
        private static readonly double EARTH_MERIDIAN_RADIUS = 12756270;

        private static readonly double MIN_LAT = Dg2Rad(-90d); // -PI/2
        private static readonly double MAX_LAT = Dg2Rad(90d); //  PI/2
        private static readonly double MIN_LON = Dg2Rad(-180d); // -PI
        private static readonly double MAX_LON = Dg2Rad(180d); //  PI

        public static bool HasAltitude(GpsLocation loc)
        {
            return (loc.Altitude < -0.01 || loc.Altitude > 0.01);
        }

        public static double QuickDistance(GpsLocation loc1, GpsLocation loc2)
        {
            double x1 = Math.PI * (loc1.Latitude/360) * EARTH_POLAR_RADIUS;
            double x2 = Math.PI * (loc2.Latitude/360) * EARTH_POLAR_RADIUS;
            double y1 = Math.Cos(loc1.Latitude * Math.PI / 180) * (loc1.Longitude/360) * EARTH_MERIDIAN_LENGTH_M;
            double y2 = Math.Cos(loc2.Latitude * Math.PI / 180) * (loc2.Longitude/360) * EARTH_MERIDIAN_LENGTH_M;
            var x = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));

            return x;
        }

        public static double QuickBearing(GpsLocation myLoc, GpsLocation otherLoc)
        {
            double myY = Math.PI * (myLoc.Latitude / 360) * 12713500;
            double otherY = Math.PI * (otherLoc.Latitude / 360) * 12713500;

            double c = Math.Cos(((myLoc.Latitude + otherLoc.Latitude) / 2) * Math.PI / 180);
            double myX = c * (myLoc.Longitude / 360) * 40075000;
            double otherX = c * (otherLoc.Longitude / 360) * 40075000;

            var dX = otherX - myX;
            var dY = otherY - myY;

            //TODO:This can be simplified probably (no if-then-else)
            double result;
            if (dX>0)
            {
                if (dY>0)
                {
                    var alfa = Rad2Dg(Math.Atan(dX / dY));
                    result = 0 + alfa;
                }
                else
                {
                    var alfa = Rad2Dg(Math.Atan(dX / -dY));
                    result = 180 - alfa;
                }
            }
            else
            {
                if (dY>0)
                {
                    var alfa = Rad2Dg(Math.Atan(-dX / dY));
                    result = 0 - alfa;
                }
                else
                {
                    var alfa = Rad2Dg(Math.Atan(-dX / -dY));
                    result = 180 + alfa;
                }
            }

            return Normalize180(result);
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
            var dist = QuickDistance(c1, c2);
            return VerticalAngle((c2.Altitude - c1.Altitude), dist);
        }

        public static double VerticalAngle(double altDif, double distance)
        {
            var x = Math.Atan(altDif / distance);
            return Rad2Dg(x);
        }

        public static bool IsAngleBetween(double angle, double heading, double limit)
        {
            var d = Normalize180(angle - heading);
            return Math.Abs(d) < limit;
        }

        public static GpsLocation QuickGetGeoLocation(GpsLocation loc, double distance, double angle)
        {
            var alfa = Dg2Rad(angle);

            var meridianLength = Math.Cos(loc.Latitude * Math.PI / 180) * EARTH_MERIDIAN_LENGTH_M;

            var x = Math.Sin(alfa) * (distance/meridianLength) * 360;
            var y = Math.Cos(alfa) * (distance/(EARTH_POLAR_LENGTH_M/2)) * 180;

            return new GpsLocation(loc.Longitude + x, loc.Latitude + y, 0);
        }

        public static void BoundingRect(GpsLocation loc, double distanceInKm, out GpsLocation min, out GpsLocation max)
        {
            var radLat = Dg2Rad(loc.Latitude);
            var radLon = Dg2Rad(loc.Longitude);

            // angular distance in radians on a great circle
            double radDist = distanceInKm / 6371.01;
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