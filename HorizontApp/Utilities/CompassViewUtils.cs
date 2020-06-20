using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CompassViewUtils
    {
        //mozna bychom to tady mohli spojit do jedne funkce, nebo presunout GetBearing do CompassProvideru
        public static float GetBearing(GpsLocation myLocation, GpsLocation point)
        {
            var myLoc = GpsUtils.Convert(myLocation);
            var poi = GpsUtils.Convert(point);
            var x = myLoc.BearingTo(poi);
            return x;
        }

        public static float? GetLocationOnScreen(float heading, float bearing, float canvasWidth, float cameraViewAngle)
        {
            float PointCanvasCoords;
            if (Math.Abs(bearing - heading) < cameraViewAngle / 2)
            {
                PointCanvasCoords = ((heading - bearing) / (cameraViewAngle / 2)) * canvasWidth + canvasWidth / 2;
                return PointCanvasCoords;
            }
            else return null;
        }

        
        public static float GetDistance(GpsLocation myLocation, GpsLocation point)
        {
            var myLoc = GpsUtils.Convert(myLocation);
            var poi = GpsUtils.Convert(myLocation);
            var x = myLoc.BearingTo(poi);
            return x;
        }
    } 
}