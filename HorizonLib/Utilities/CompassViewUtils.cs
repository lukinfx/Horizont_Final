using System;
using System.Drawing;
using System.Linq;
using HorizonLib.Domain.Enums;
using HorizontLib.Domain.Models;
using HorizontLib.Domain.ViewModel;

namespace HorizontLib.Utilities
{
    public class CompassViewUtils
    {
        /*public static float GetBearing(GpsLocation myLocation, GpsLocation point)
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
        }*/

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
            double diff = GetAngleDiff(bearing, heading);
            // +2 due to the edge of the screen and rounding bearing
            if (Math.Abs(diff) < (cameraViewAngle + 2) / 2)
            {
                XCoord = ((float)diff/ (cameraViewAngle / 2)) * canvasWidth/2 + canvasWidth / 2;
                return XCoord;
            }
            else return null;
        }

        public static float GetYLocationOnScreen(double verticalAngle, double canvasHeight, double cameraViewAngle)
        {
            var YCoord = (canvasHeight / 2) - ((verticalAngle / (cameraViewAngle / 2)) * canvasHeight / 2);
            var YCoordFloat = (float)YCoord;
            return YCoordFloat;
        }

        public static double GetPoiViewAngle(double distance, double altitudeDifference)
        {
            return GpsUtils.Rad2Dg(Math.Atan(altitudeDifference / distance));
        }

        public static float GetYLocationOnScreen(double distance, double altitudeDifference, double canvasHeight, double cameraViewAngle)
        {
            return GetYLocationOnScreen(GetPoiViewAngle(distance, altitudeDifference), canvasHeight, cameraViewAngle);
        }

        public static Visibility IsPoiVisible (PoiViewItem item, ElevationProfileData elevationProfileData)
        {
            if (elevationProfileData == null)
                return Visibility.Visible;

            var leftPoints = elevationProfileData.GetData((int) item.Bearing);
            var rightPoints = elevationProfileData.GetData((int)GpsUtils.Normalize360(item.Bearing + 1));

            var itemViewAngle = GetPoiViewAngle(item.Distance, item.AltitudeDifference);

            var maxLeft = leftPoints.GetPoints().Where(p => p.Distance < item.Distance).Max(p => p.VerticalViewAngle) ?? -100;
            var maxRight = rightPoints.GetPoints().Where(p => p.Distance < item.Distance).Max(p => p.VerticalViewAngle) ?? -100;;
            var maxViewAngle = Math.Max(maxLeft, maxRight);

            var viewAngleDiff = itemViewAngle - maxViewAngle;

            if (viewAngleDiff > -0.5)
                return Visibility.Visible;
            if (viewAngleDiff > -2.0)
                return Visibility.PartialyVisible;

            return Visibility.Invisible;
        }

        public static float GetYLocationOnScreen(double distance, double altitudeDifference, double canvasHeight, double cameraViewAngle, float XLocation, double leftTiltCorrector, double rightTiltCorrector, double canvasWidth)
        {
            var verticalAngle = GpsUtils.Rad2Dg(Math.Atan(altitudeDifference / distance));
            var YCoord = (canvasHeight / 2) - ((verticalAngle / (cameraViewAngle / 2)) * canvasHeight / 2);
            double leftPixelsDifference = (leftTiltCorrector / cameraViewAngle) * canvasHeight;
            double rightPixelsDifference = (rightTiltCorrector / cameraViewAngle) * canvasHeight;
            double YDifference = leftPixelsDifference + XLocation * (rightPixelsDifference - leftPixelsDifference) / canvasWidth;
            var YCoordFloat = (float)(YCoord + YDifference);
            return YCoordFloat;
        }

        public static float GetYLocationOnScreen(float YLocation, float XLocation, double leftTiltCorrector, double rightTiltCorrector, double canvasWidth, double canvasHeight, double cameraViewAngle)
        {
            double leftPixelsDifference = (leftTiltCorrector / cameraViewAngle) * canvasHeight;
            double rightPixelsDifference = (rightTiltCorrector / cameraViewAngle) * canvasHeight;
            double YDifference = leftPixelsDifference + XLocation * (rightPixelsDifference - leftPixelsDifference) / canvasWidth;
            var YCoordFloat = (float)(YLocation + YDifference);
            return YCoordFloat;
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

        public static double GetAngleDiff(double alfa, double beta)
        {
            if (alfa > beta && Math.Abs(alfa - beta) > 180)
            {
                return alfa - beta - 360;
            }
            else if (alfa < beta && Math.Abs(alfa - beta) > 180)
            {
                return 360 - beta + alfa;
            }
            else return alfa - beta;
        }

        public static (float, float) AdjustViewAngles(float viewAngleHorizontal, float viewAngleVertical, Size canvasSize, Size imageSize)
        {
            var adjustedViewAngleVertical = viewAngleVertical;
            var adjustedViewAngleHorizontal = viewAngleHorizontal;
            if (canvasSize.Height > 0 && canvasSize.Width > 0 && imageSize.Width > 0 && imageSize.Height > 0)
            {
                if (canvasSize.Width > canvasSize.Height)
                {
                    var ratio = canvasSize.Height / (float)canvasSize.Width;
                    var displayedPictureHeight = imageSize.Width * ratio;
                    var r2 = displayedPictureHeight / (float)imageSize.Height;

                    adjustedViewAngleVertical = viewAngleVertical * r2;
                }
                else
                {
                    var ratio = canvasSize.Width / (float)canvasSize.Height;
                    var displayedPictureWidth = imageSize.Width * ratio;
                    var r2 = displayedPictureWidth / (float)imageSize.Height;

                    adjustedViewAngleHorizontal = viewAngleHorizontal * r2;
                }
            }

            return (adjustedViewAngleHorizontal, adjustedViewAngleVertical);
        }
    } 
}