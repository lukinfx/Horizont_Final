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
using HorizontApp.Utilities;
using HorizontApp.Views;
using Object = Java.Lang.Object;

namespace HorizontApp.Tasks
{
    public class ElevationCalculation : AsyncTask<GpsLocation, string, ElevationProfileData>
    {
        private GpsLocation _myLocation;
        private int _visibility;
        private CompassView _compassView;

        public ElevationCalculation(GpsLocation myLocation, int visibility, CompassView compassView)
        {
            _myLocation = myLocation;
            _visibility = visibility;
            _compassView = compassView;
        }

        protected override void OnProgressUpdate(params string[] values)
        {
            base.OnProgressUpdate(values);
            System.Console.Write(".");
        }

        protected override void OnPreExecute()
        {
            base.OnPreExecute();
            System.Console.WriteLine("Staring");
        }

        protected override void OnPostExecute(ElevationProfileData result)
        {
            base.OnPostExecute(result);
            System.Console.WriteLine("Finished");
            _compassView.SetElevationProfile(result);
        }

        protected override ElevationProfileData RunInBackground(params GpsLocation[] @params)
        {
            ElevationProfile elevationProfile = new ElevationProfile();
            elevationProfile.Load(_myLocation, _visibility);
            return elevationProfile.GetProfile();
        }
    }
}