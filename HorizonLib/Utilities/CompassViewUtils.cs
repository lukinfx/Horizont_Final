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

        public static float? GetXLocationOnScreen(float heading, float bearing, float canvasWidth, float cameraViewAngle, float offset)
        {
            if (bearing < 0) bearing = 360 + bearing;
            double diff = GetAngleDiff(bearing, heading);
            var xCoord = ((float)diff / (cameraViewAngle / 2)) * canvasWidth / 2 + canvasWidth / 2 + offset;
            
            // +-50 due to the edge of the screen and rounding bearing
            if (xCoord >= 0 - 50 && xCoord <= canvasWidth + 50)
            {
                
                return xCoord;
            }
            else return null;
        }

        public static float GetYLocationOnScreen(double itemViewAngle, double canvasHeight, double cameraViewAngle)
        {
            var YCoord = (canvasHeight / 2) - ((itemViewAngle / (cameraViewAngle / 2)) * canvasHeight / 2);

            return (float)YCoord;
        }

        public static double GetPoiViewAngle(double distance, double altitudeDifference)
        {
            return GpsUtils.Rad2Dg(Math.Atan(altitudeDifference / distance));
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

        public static double GetTiltCorrection(double bearing, double heading, double viewAngleHorizontal, double leftTiltCorrector, double rightTiltCorrector)
        {
            double diff = CompassViewUtils.GetAngleDiff(bearing, heading);
            double percentX = (diff + viewAngleHorizontal / 2) / viewAngleHorizontal; //0.00 - 1.00
            double verticalAngleCorrection = leftTiltCorrector + (rightTiltCorrector - leftTiltCorrector) * percentX;
            return verticalAngleCorrection;
        }

        public static (float, float) AdjustViewAngles(float viewAngleHorizontal, float viewAngleVertical, Size canvasSize, Size imageSize, bool allowRotation)
        {
            var adjustedViewAngleVertical = viewAngleVertical;
            var adjustedViewAngleHorizontal = viewAngleHorizontal;
            if (canvasSize.Height > 0 && canvasSize.Width > 0 && imageSize.Width > 0 && imageSize.Height > 0)
            {
                if (!allowRotation || (allowRotation && canvasSize.Width > canvasSize.Height))
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