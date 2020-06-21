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
        System.Timers.Timer _timer = new System.Timers.Timer();

        SKCanvasView canvasView;

        private GpsLocationProvider gpsLocationProvider = new GpsLocationProvider();
        private CompassProvider compassProvider = new CompassProvider();
        PoiList poiList = new PoiList();
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

            _timer.Interval = 100;
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;

            if (bundle == null)
            {
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, CameraFragment.NewInstance()).Commit();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public async void OnClick(Android.Views.View v)
        {
            switch (v.Id)
            {
                case Resource.Id.button1:
                    {
                        compassProvider.ToggleCompass(); 
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
                        var file = GpxFileProvider.GetFile();
                        var listOfPoi = GpxFileParser.Parse(file, PoiCategory.Peaks);

                        foreach (var item in listOfPoi)
                        {
                            await Database.InsertItemAsync(item);
                        }

                        poiList.List = listOfPoi;

                        break;
                    }
                case Resource.Id.button4:
                    {
                        var poiList = await Database.GetItemsAsync();
                        
                        PoiViewItemList poiViewItemList = new PoiViewItemList();
                        poiViewItemList.List = new List<PoiViewItem>();
                        myLocation = await gpsLocationProvider.GetLocationAsync();
                        foreach (var item in poiList /*poiList.List*/)
                        {
                            var poiViewItem = new PoiViewItem
                            {
                                Poi = item
                            };
                            poiViewItem.Heading = CompassViewUtils.GetBearing(myLocation, poiViewItem.GpsLocation);

                            poiViewItemList.List.Add(poiViewItem);
                        }

                        CompassView.SetPoiViewItemList(poiViewItemList);

                        break;
                    }

            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            headingEditText.Text = compassProvider.Heading.ToString();
        }
    }
}