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
    public class PoiViewItem : Poi
    {
        public double Bearing;
        public double Distance;
        
        public GpsLocation GpsLocation
        {
            get
            {
                return new GpsLocation() { Latitude = this.Latitude, Longitude = this.Longitude, Altitude = this.Altitude };
            }
        }

        public PoiViewItem(Poi poi) : base(poi)
        {
        }
    }
}