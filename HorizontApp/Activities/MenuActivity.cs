using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using HorizontLib.Domain.Models;
using Android.Views;
using HorizontApp.Views.ListOfPoiView;
using Android.Content;
using static Android.Views.View;
using HorizontApp.Activities;
using Xamarin.Essentials;
using System;

namespace HorizontApp.Activities
{
    [Activity(Label = "MenuActivity")]
    public class MenuActivity : Activity, IOnClickListener
    {
        private GpsLocation _location;
        private int _maxDistance;
        private int _minAltitude;
        private DisplayOrientation appOrientation;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _location = new GpsLocation()
            {
                Latitude = Intent.GetDoubleExtra("latitude", 0),
                Longitude = Intent.GetDoubleExtra("longitude", 0),
                Altitude = Intent.GetDoubleExtra("altitude", 0),
            };

            _maxDistance = Intent.GetIntExtra("maxDistance", 0);
            _minAltitude = Intent.GetIntExtra("minAltitude", 0);

            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            var orientation = mainDisplayInfo.Orientation;

            if (orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.MenuActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.MenuActivity);
            }

            var buttonHome = FindViewById<ImageButton>(Resource.Id.buttonHome);
            buttonHome.SetOnClickListener(this);

            var buttonList = FindViewById<ImageButton>(Resource.Id.buttonList);
            buttonList.SetOnClickListener(this);

            var buttonDownload = FindViewById<ImageButton>(Resource.Id.buttonDownload);
            buttonDownload.SetOnClickListener(this);

            var buttonSettings = FindViewById<ImageButton>(Resource.Id.buttonSettings);
            buttonSettings.SetOnClickListener(this);

            var buttonAbout = FindViewById<ImageButton>(Resource.Id.buttonAbout);
            buttonAbout.SetOnClickListener(this);
        }

        //private void OnMainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
        //{
        //    var displayInfo = e.DisplayInfo;
        //    if (displayInfo.Orientation != appOrientation)
        //    {
        //        appOrientation = displayInfo.Orientation;
        //    }
        //}

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonHome:
                    Finish();
                    break;

                case Resource.Id.buttonDownload:
                    Intent downloadActivityIntent = new Intent(this, typeof(DownloadActivity));
                    StartActivity(downloadActivityIntent);
                    break;

                case Resource.Id.buttonList:
                    Intent listActivityIntent = new Intent(this, typeof(PoiListActivity));
                    listActivityIntent.PutExtra("latitude", _location.Latitude);
                    listActivityIntent.PutExtra("longitude", _location.Longitude);
                    listActivityIntent.PutExtra("altitude", _location.Altitude);
                    listActivityIntent.PutExtra("maxDistance", _maxDistance);
                    listActivityIntent.PutExtra("minAltitude", _minAltitude);
                    StartActivity(listActivityIntent);
                    break;
                case Resource.Id.buttonSettings:
                    Intent settingsActivityIntent = new Intent(this, typeof(SettingsActivity));
                    StartActivity(settingsActivityIntent);
                    break;
            }
        }
    }
}