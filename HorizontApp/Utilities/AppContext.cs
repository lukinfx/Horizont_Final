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
using HorizontApp.Providers;
using HorizontLib.Utilities;

namespace HorizontApp.Utilities
{
    public class AppContext
    {
        private static AppContext _instance;
        
        public GpsLocationProvider GpsLocationProvider { get; private set; }
        public CompassProvider CompassProvider { get; private set; }
        public HeadingStabilizator HeadingStabilizator { get; private set; }
        public ElevationProfileData ElevationProfileData { get; set; }

        public static AppContext Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new AppContext();
                }

                return _instance;
            }
        }

        private AppContext()
        {
            GpsLocationProvider = new GpsLocationProvider();
            CompassProvider = new CompassProvider();
            HeadingStabilizator = new HeadingStabilizator();
        }

    }
}