using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using HorizontApp.Providers;
using Android.Content.PM;
using static Android.Views.View;
using SkiaSharp.Views.Android;
using Android.Locations;
using System.Timers;
using System.Linq;
using HorizontApp.Utilities;
using HorizontApp.Domain.Enums;
using System.Collections.Generic;
using HorizontApp.Domain.Models;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Views;
using Javax.Xml.Transform.Dom;
using HorizontApp.Views.Camera;
using Android.Views;
using HorizontApp.DataAccess;
using Xamarin.Essentials;
using AlertDialog = Android.App.AlertDialog;
using System;

namespace HorizontApp
{
    class x : Application
    {

    }

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    //[Activity(Theme = "@android:style/Theme.DeviceDefault.NoActionBar.Fullscreen", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AppCompatActivity, IOnClickListener
    {
        static PoiDatabase database;

        EditText headingEditText;
        EditText GPSEditText;
        EditText DistanceEditText;
        Button getHeadingButton;
        Button getGPSButton;
        Button startCompassButton;
        Button stopCompassButton;
        CompassView compassView;
        SeekBar distanceSeekBar;
        SeekBar heightSeekBar;

        Timer compassTimer = new Timer();
        Timer locationTimer = new Timer();

        private GpsLocationProvider gpsLocationProvider = new GpsLocationProvider();
        private CompassProvider compassProvider = new CompassProvider();
        GpsLocation myLocation = null;

        public static PoiDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new PoiDatabase();
                }
                return database;
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            //Window.RequestFeature(WindowFeatures.NoTitle);

            Xamarin.Essentials.Platform.Init(this, bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            headingEditText = FindViewById<EditText>(Resource.Id.editText1);
            GPSEditText = FindViewById<EditText>(Resource.Id.editText2);

            getHeadingButton = FindViewById<Button>(Resource.Id.button1);
            getHeadingButton.SetOnClickListener(this);
            
            DistanceEditText = FindViewById<EditText>(Resource.Id.editText3);
            DistanceEditText.SetOnClickListener(this);
            
            distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBar2);
            distanceSeekBar.ProgressChanged += SeekBarProgressChanged;
            //System.EventHandler

            heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBar1);
            heightSeekBar.ProgressChanged += SeekBarProgressChanged;

            getGPSButton = FindViewById<Button>(Resource.Id.button2);
            getGPSButton.SetOnClickListener(this);

            startCompassButton = FindViewById<Button>(Resource.Id.button3);
            startCompassButton.SetOnClickListener(this);

            stopCompassButton = FindViewById<Button>(Resource.Id.button4);
            stopCompassButton.SetOnClickListener(this);

            compassView = FindViewById<CompassView>(Resource.Id.compassView1);

            if (bundle == null)
            {
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, CameraFragment.NewInstance()).Commit();
            }

            compassProvider.Start();
            gpsLocationProvider.Start();

            //LoadAndDisplayData();

            InitializeCompassTimer();
            InitializeLocationTimer();
        }


        private void InitializeCompassTimer()
        {
            compassTimer.Interval = 100;
            compassTimer.Elapsed += OnCompassTimerElapsed;
            compassTimer.Enabled = true;
        }

        private void InitializeLocationTimer()
        {
            locationTimer.Interval = 3000;
            locationTimer.Elapsed += OnLocationTimerElapsed;
            locationTimer.Enabled = true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public void PopupDialog(string title, string message)
        {
            using (var dialog = new AlertDialog.Builder(this))
            {
                dialog.SetTitle(title);
                dialog.SetMessage(message);
                dialog.Show();
            }
        }

        public async void OnClick(Android.Views.View v)
        {
            switch (v.Id)
            {
                case Resource.Id.button1:
                    LoadDataFromInternet("http://vrcholky.8u.cz/hory.gpx");
                    break;
                case Resource.Id.button2:
                    {
                        break;
                    }
                case Resource.Id.button3:
                    {
                        LoadDataFromInternet("http://vrcholky.8u.cz/hory%20(3).gpx");
                        break;
                    }
                case Resource.Id.button4:
                    {
                        ReloadData();
                        break;
                    }

            }
        }

        private void OnCompassTimerElapsed(object sender, ElapsedEventArgs e)
        {
            headingEditText.Text = compassProvider.Heading.ToString();
            compassView.Heading = compassProvider.Heading;
            compassView.Invalidate();
        }

        private void OnLocationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            LoadAndDisplayData();
        }

        private void LoadAndDisplayData()
        {
            try
            {
                var newLocation = gpsLocationProvider.GetLocation();

                if (newLocation == null)
                    return;

                if (myLocation == null || GpsUtils.Distance(myLocation, newLocation) > 100)
                {
                    myLocation = newLocation;
                    GPSEditText.Text = ($"Latitude: {myLocation.Latitude}, Longitude: {myLocation.Longitude}, Altitude: {myLocation.Altitude}");


                    var points = GetPointsToDisplay(myLocation, distanceSeekBar.Progress, heightSeekBar.Progress);
                    compassView.SetPoiViewItemList(points);
                }
            }
            catch (Exception ex)
            {
                PopupDialog("Error", ex.Message);
            }

        }

        private void SeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            DistanceEditText.Text = "vyska nad " + heightSeekBar.Progress * 10 + ", do " + distanceSeekBar.Progress + " daleko";
        }

        private async void LoadDataFromInternet(string filePath)
        {
            try
            {
                var file = GpxFileProvider.GetFile(filePath);
                var listOfPoi = GpxFileParser.Parse(file, PoiCategory.Peaks);

                int count = 0;
                foreach (var item in listOfPoi)
                {
                    await Database.InsertItemAsync(item);
                    count++;
                }

                PopupDialog("Information", $"{count} items loaded to database.");
            }
            catch(Exception ex)
            {
                PopupDialog("Error", $"Error when loading data. {ex.Message}");
            }
        }

        private void ReloadData()
        {
            try
            {
                if (myLocation == null)
                    return;

                var points = GetPointsToDisplay(myLocation, distanceSeekBar.Progress, heightSeekBar.Progress);
                compassView.SetPoiViewItemList(points);
            }
            catch (Exception ex)
            {
                PopupDialog("Error", $"Error when loading data. {ex.Message}");
            }
        }

        private PoiViewItemList GetPointsToDisplay(GpsLocation location, double maxDistance, double minAltitude)
        {
            try
            {
                var poiList = Database.GetItems();

                PoiViewItemList poiViewItemList = new PoiViewItemList();
                foreach (var item in poiList)
                {
                    var poiViewItem = new PoiViewItem(item);
                    poiViewItem.Bearing = CompassViewUtils.GetBearing(location, poiViewItem.GpsLocation);
                    poiViewItem.Distance = CompassViewUtils.GetDistance(location, poiViewItem.GpsLocation);

                    if ((poiViewItem.Distance < maxDistance * 1000) && (poiViewItem.Altitude > minAltitude * 10))
                    {
                        poiViewItemList.Add(poiViewItem);
                    }
                }

                return poiViewItemList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when fetching data. {ex.Message}");
            }
        }
    }
}