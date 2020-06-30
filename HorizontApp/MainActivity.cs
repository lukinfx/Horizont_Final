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
        Button getHeadingButton;
        Button getGPSButton;
        Button startCompassButton;
        Button stopCompassButton;
        CompassView compassView;

        Timer compassTimer = new Timer();
        Timer locationTimer = new Timer();

        private GpsLocationProvider gpsLocationProvider = new GpsLocationProvider();
        private CompassProvider compassProvider = new CompassProvider();
        GpsLocation myLocation = new GpsLocation();

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

            getGPSButton = FindViewById<Button>(Resource.Id.button2);
            getGPSButton.SetOnClickListener(this);

            startCompassButton = FindViewById<Button>(Resource.Id.button3);
            startCompassButton.SetOnClickListener(this);

            stopCompassButton = FindViewById<Button>(Resource.Id.button4);
            stopCompassButton.SetOnClickListener(this);

            compassView = FindViewById<CompassView>(Resource.Id.compassView1);

            InitializeCompassTimer();
            InitializeLocationTimer();
        
            if (bundle == null)
            {
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, CameraFragment.NewInstance()).Commit();
            }
            compassProvider.ToggleCompass();
        }

        private void InitializeCompassTimer()
        {
            compassTimer.Interval = 100;
            compassTimer.Elapsed += OnCompassTimerElapsed;
            compassTimer.Enabled = true;
        }

        private void InitializeLocationTimer()
        {
            locationTimer.Interval = 100;
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
                    {
                        var file = GpxFileProvider.GetFile("http://vrcholky.8u.cz/hory.gpx");
                        var listOfPoi = GpxFileParser.Parse(file, PoiCategory.Peaks);

                        int count = 0;
                        foreach (var item in listOfPoi)
                        {
                            await Database.InsertItemAsync(item);
                            count++;
                        }

                        PopupDialog("Information", $"{count} items loaded to database.");
                        break;
                    }
                case Resource.Id.button2:
                    {
                        GpsLocation location = await gpsLocationProvider.GetLocationAsync();
                        GPSEditText.Text = ($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
                        
                        break;
                    }
                case Resource.Id.button3:
                    {
                        /*var file = GpxFileProvider.GetFile("http://vrcholky.8u.cz/hory%20(3).gpx");
                        var listOfPoi = GpxFileParser.Parse(file, PoiCategory.Peaks);

                        foreach (var item in listOfPoi)
                        {
                            await Database.InsertItemAsync(item);
                        }
                        */
                        StartActivity((typeof(ItemListActivity)));
                        break;
                    }
                case Resource.Id.button4:
                    {
                        var poiList = await Database.GetItemsAsync();
                        myLocation = await gpsLocationProvider.GetLocationAsync();

                        PoiViewItemList poiViewItemList = new PoiViewItemList();
                        foreach (var item in poiList)
                        {
                            var poiViewItem = new PoiViewItem(item);
                            poiViewItem.Bearing = CompassViewUtils.GetBearing(myLocation, poiViewItem.GpsLocation);
                            poiViewItem.Distance = CompassViewUtils.GetDistance(myLocation, poiViewItem.GpsLocation);
                            poiViewItemList.Add(poiViewItem);
                        }

                        var poiViewItemListFiltered = new PoiViewItemList();
                        poiViewItemListFiltered.AddRange(poiViewItemList.Where(x => x.Distance < 20000));

                        compassView.SetPoiViewItemList(poiViewItemListFiltered);
                        

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
            //TODO: recalculate PoiViewItemList
            //CompassView.SetPoiViewItemList(poiViewItemList);
        }
    }
}