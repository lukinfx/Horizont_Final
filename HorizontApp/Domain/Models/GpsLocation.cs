using HorizontApp.Utilities;
using PaintSkyLine;
using System;

namespace HorizontApp.Domain.Models
{
    public class GpsLocation
    {
        public double Longitude;
        public double Latitude;
        public double Altitude;

        public double? Distance = null;
        public double? Bearing = null;
        public double? VerticalViewAngle = null;

        public double QuickDistance(GpsLocation myLoc)
        {
            if (Distance == null)
            {
                double x1 = Math.PI * (this.Latitude / 360) * 12713500;
                double x2 = Math.PI * (myLoc.Latitude / 360) * 12713500;
                double y1 = Math.Cos(this.Latitude * Math.PI / 180) * (this.Longitude / 360) * 40075000;
                double y2 = Math.Cos(myLoc.Latitude * Math.PI / 180) * (myLoc.Longitude / 360) * 40075000;
                Distance = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
            }

            return Distance.Value;
        }

        public double QuickBearing(GpsLocation myLoc)
        {
            if (Bearing == null)
            {
                double myY = Math.PI * (myLoc.Latitude / 360) * 12713500;
                double otherY = Math.PI * (this.Latitude / 360) * 12713500;

                double c = Math.Cos(((this.Latitude + myLoc.Latitude) / 2) * Math.PI / 180);
                double myX = c * (myLoc.Longitude / 360) * 40075000;
                double otherX = c * (this.Longitude / 360) * 40075000;

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
                Bearing = Normalize180(result);
            }
            return Bearing.Value;
        }

        public double GetVerticalViewAngle(GpsLocation myLoc)
        {
            this.VerticalViewAngle = GpsUtils.VerticalAngle(this.Altitude - myLoc.Altitude, this.Distance.Value);
            return VerticalViewAngle.Value;
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
    }
}