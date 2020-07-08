using Android;
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
using HorizontApp.Views.ListOfPoiView;
using Android.Content;
using Android.Support.V13.App;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using HorizontApp.Activities;

namespace HorizontApp
{
    class x : Application
    {

    }

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    //[Activity(Theme = "@android:style/Theme.DeviceDefault.NoActionBar.Fullscreen", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AppCompatActivity, IOnClickListener
    {
        private static readonly int REQUEST_LOCATION_PERMISSION = 0;
        private static readonly int REQUEST_CAMERA_PERMISSION = 1;

        EditText headingEditText;
        EditText GPSEditText;
        EditText DistanceEditText;
        Button getHeadingButton;
        Button getGPSButton;
        Button stopCompassButton;
        CompassView compassView;
        private CameraFragment cameraFragment;
        SeekBar distanceSeekBar;
        SeekBar heightSeekBar;
        private View layout;
        ImageButton menu;

        Timer compassTimer = new Timer();
        Timer locationTimer = new Timer();

        private GpsLocationProvider gpsLocationProvider = new GpsLocationProvider();
        private CompassProvider compassProvider = new CompassProvider();
        GpsLocation myLocation = null;

        private PoiDatabase database;
        public PoiDatabase Database
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

            menu = FindViewById<ImageButton>(Resource.Id.imageButton1);
            menu.SetOnClickListener(this);

            stopCompassButton = FindViewById<Button>(Resource.Id.button4);
            stopCompassButton.SetOnClickListener(this);

            compassView = FindViewById<CompassView>(Resource.Id.compassView1);

            layout = FindViewById(Resource.Id.sample_main_layout);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                RequestGPSLocationPermissions();
            }


            if (bundle == null)
            {
                cameraFragment = CameraFragment.NewInstance();
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, cameraFragment).Commit();
            }

            compassProvider.Start();
            gpsLocationProvider.Start();


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
                    Intent downloadActivityIntent = new Intent(this, typeof(DownloadActivity));
                    StartActivity(downloadActivityIntent);
                    break;
                case Resource.Id.button2:
                    {
                        Intent i = new Intent(this, typeof(PoiListActivity));
                        i.PutExtra("latitude", myLocation.Latitude);
                        i.PutExtra("longitude", myLocation.Longitude);
                        i.PutExtra("altitude", myLocation.Altitude);
                        i.PutExtra("maxDistance", distanceSeekBar.Progress);
                        i.PutExtra("minAltitude", heightSeekBar.Progress);
                        
                        StartActivity(i);
                        break;
                    }
                case Resource.Id.button4:
                    {
                        ReloadData();
                        break;
                    }
                case Resource.Id.imageButton1:
                    {
                        break;
                    }

            }
        }

        private void OnCompassTimerElapsed(object sender, ElapsedEventArgs e)
        {
            headingEditText.Text = compassProvider.Heading.ToString();
            compassView.headings.Enqueue(compassProvider.Heading);
            compassView.Invalidate();
        }

        private async void OnLocationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            LoadAndDisplayData();
        }

        private async void LoadAndDisplayData()
        {
            try
            {
                var newLocation = await gpsLocationProvider.GetLocationAsync();

                if (newLocation == null)
                    return;

                if (myLocation == null || GpsUtils.Distance(myLocation, newLocation) > 100)
                {
                    myLocation = newLocation;
                    GPSEditText.Text = ($"Latitude: {myLocation.Latitude}, Longitude: {myLocation.Longitude}, Altitude: {myLocation.Altitude}");


                    var points = GetPointsToDisplay(myLocation, distanceSeekBar.Progress, heightSeekBar.Progress);
                    compassView.SetPoiViewItemList(points);

                    compassView.ViewAngleHorizontal = cameraFragment.ViewAngleHorizontal;
                    compassView.ViewAngleVertical = cameraFragment.ViewAngleVertical;
                }
            }
            catch (Exception ex)
            {
                PopupDialog("Error", ex.Message);
            }

        }

        private void SeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            DistanceEditText.Text = "vyska nad " + heightSeekBar.Progress * 16 + "m, do " + distanceSeekBar.Progress + "km daleko";
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
                var poiList = Database.GetItems(location, maxDistance);

                PoiViewItemList poiViewItemList = new PoiViewItemList(poiList, location, maxDistance, minAltitude);
                return poiViewItemList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when fetching data. {ex.Message}");
            }
        }

        private void RequestGPSLocationPermissions()
        {
            //From: https://docs.microsoft.com/cs-cz/xamarin/android/app-fundamentals/permissions
            //Sample app: https://github.com/xamarin/monodroid-samples/tree/master/android-m/RuntimePermissions

            var requiredPermissions = new String[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.Camera };

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.Camera))
            {
                Snackbar.Make(layout, "Location and camera permissions are needed to show relevant data.", Snackbar.LengthIndefinite)
                    .SetAction("OK", new Action<View>(delegate (View obj) 
                        {
                            ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_LOCATION_PERMISSION);
                        })
                    ).Show();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_LOCATION_PERMISSION);
            }
        }
    }
}