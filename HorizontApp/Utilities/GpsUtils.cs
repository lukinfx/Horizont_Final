using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontLib.Domain.Models;

namespace HorizontApp.Utilities
{
    public class GpsUtils : HorizontLib.Utilities.GpsUtils
    {
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

        public static double Distance(GpsLocation loc1, GpsLocation loc2)
        {
            return Convert(loc1).DistanceTo(Convert(loc2));
        }

        public static double VerticalAngle(GpsLocation c1, GpsLocation c2)
        {
            var dist = Distance(c1, c2);
            return VerticalAngle((c2.Altitude - c1.Altitude), dist);
        }

    }
}