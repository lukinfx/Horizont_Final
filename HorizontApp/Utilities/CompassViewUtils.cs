﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Locations;
using Android.Media;
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
        public static float GetYLocationOnScreen(double distance, float altitudeDifference, float canvasHeight, float cameraViewAngle)
        {
            var YCoord = (canvasHeight / 2) - ((GpsUtils.Rad2Dg(Math.Atan(altitudeDifference / distance)) / (cameraViewAngle / 2)) * canvasHeight / 2);
            var YCoordFloat = (float)YCoord;
            return YCoordFloat;
        }

        public static float GetHeadingDifference(float cameraViewAngle, float canvasWidth, float moveX)
        {
            float headingDiff = (moveX / canvasWidth) * cameraViewAngle;

            return headingDiff;
        }

    } 
}