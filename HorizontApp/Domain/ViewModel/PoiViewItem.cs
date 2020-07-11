using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.Models;

namespace HorizontApp.Domain.ViewModel
{
    public class PoiViewItem
    {
        public Poi Poi;
        public double Bearing;
        public double Distance;
        public float AltitudeDifference { get; internal set; }

        public GpsLocation GpsLocation
        {
            get
            {
                return new GpsLocation() { Latitude = this.Poi.Latitude, Longitude = this.Poi.Longitude, Altitude = this.Poi.Altitude };
            }
        }

        

        public PoiViewItem(Poi poi) 
        {
            Poi = poi;
        }
    }
}