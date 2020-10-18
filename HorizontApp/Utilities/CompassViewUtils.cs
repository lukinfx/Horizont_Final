using System;
using HorizontLib.Domain.Models;

namespace HorizontApp.Utilities
{
    public class CompassViewUtils
    {
        public static float GetBearing(GpsLocation myLocation, GpsLocation point)
        {
            var myLoc = GpsUtils.Convert(myLocation);
            var poi = GpsUtils.Convert(point);
            var x = myLoc.BearingTo(poi);
            return x;
        }

        public static float GetDistance(GpsLocation myLocation, GpsLocation point)
        {
            var myLoc = GpsUtils.Convert(myLocation);
            var poi = GpsUtils.Convert(point);
            var x = myLoc.DistanceTo(poi);
            return x;
        }

        public static float GetAltitudeDifference(GpsLocation myLocation, GpsLocation point)
        {
            var diff = point.Altitude - myLocation.Altitude;
            float a = (float)diff;
            return a;
        }

        public static float? GetXLocationOnScreen(float heading, float bearing, float canvasWidth, float cameraViewAngle)
        {
            float XCoord;
            if (bearing < 0) bearing = 360 + bearing;
            double diff = CompassUtils.GetAngleDiff(bearing, heading);
            if (Math.Abs(diff) < cameraViewAngle / 2)
            {
                XCoord = ((float)diff/ (cameraViewAngle / 2)) * canvasWidth/2 + canvasWidth / 2;
                return XCoord;
            }
            else return null;
        }

        public static float GetYLocationOnScreen(double verticalAngle, float canvasHeight, float cameraViewAngle)
        {
            var YCoord = (canvasHeight / 2) - ((verticalAngle / (cameraViewAngle / 2)) * canvasHeight / 2);
            var YCoordFloat = (float)YCoord;
            return YCoordFloat;
        }

        public static float GetYLocationOnScreen(double distance, float altitudeDifference, float canvasHeight, float cameraViewAngle)
        {
            return GetYLocationOnScreen(GpsUtils.Rad2Dg(Math.Atan(altitudeDifference / distance)), canvasHeight, cameraViewAngle);
        }

        /// <summary>
        /// Returns angular difference defined by moveX translation
        /// </summary>
        /// <param name="cameraViewAngle"></param>
        /// <param name="canvasWidth"></param>
        /// <param name="moveX"></param>
        /// <returns></returns>
        public static float GetHeadingDifference(float cameraViewAngle, float canvasWidth, float moveX)
        {
            float headingDiff = (moveX / canvasWidth) * cameraViewAngle;

            return headingDiff;
        }

    } 
}