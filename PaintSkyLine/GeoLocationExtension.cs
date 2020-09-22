using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElevationData;
using static ElevationData.GeoTiffReader;

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

        public GeoPoint(double latitude, double longitude, double alt, GeoPoint myLocation = null)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = (UInt32) alt;

            if (myLocation != null)
            {
                Distance = myLocation.QuickDistance(this);
                Bearing = myLocation.QuickBearing(this);
                VerticalAngle = GetVerticalAngle(Distance, alt - myLocation.Altitude);
            }
        }

        public static bool IsAngleBetween(double angle, double heading, double limit)
        {
            var d = Normalize180(angle - heading);
            return Math.Abs(d) < limit;
        }

        public double DegreeBearing(
            GeoPoint c2)
        {
            var dLon = Dg2Rad(c2.Longitude - this.Longitude);
            var dPhi = Math.Log(
                Math.Tan(Dg2Rad(c2.Latitude) / 2 + Math.PI / 4) / Math.Tan(Dg2Rad(this.Latitude) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            return ToBearing(Math.Atan2(dLon, dPhi));
        }

        public double QuickDistance(GeoPoint loc2)
        {
            double x = 0;
            double x1 = Math.PI * (this.Latitude / 360) * 12713500;
            double x2 = Math.PI * (loc2.Latitude / 360) * 12713500;
            double y1 = Math.Cos(this.Latitude * Math.PI / 180) * (this.Longitude / 360) * 40075000;
            double y2 = Math.Cos(loc2.Latitude * Math.PI / 180) * (loc2.Longitude / 360) * 40075000;
            x = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));

            return x;
        }


        public double QuickBearing(GeoPoint otherLoc)
        {
            double myY = Math.PI * (this.Latitude / 360) * 12713500;
            double otherY = Math.PI * (otherLoc.Latitude / 360) * 12713500;

            double c = Math.Cos(((this.Latitude + otherLoc.Latitude) / 2) * Math.PI / 180);
            double myX = c * (this.Longitude / 360) * 40075000;
            double otherX = c * (otherLoc.Longitude / 360) * 40075000;

            var dX = otherX - myX;
            var dY = otherY - myY;

            //TODO:This can be simplified probably (no if-then-else)
            double result;
            if (dX > 0)
            {
                if (dY > 0)
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
                if (dY > 0)
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

        public void BoundingRect(double distance, out GeoPoint min, out GeoPoint max)
        {
            var radLat = Dg2Rad(this.Latitude);
            var radLon = Dg2Rad(this.Longitude);

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

            min = new GeoPoint(Rad2Dg(minLat), Rad2Dg(minLon), 0);
            max = new GeoPoint(Rad2Dg(maxLat), Rad2Dg(maxLon), 0);
        }
    }
}
