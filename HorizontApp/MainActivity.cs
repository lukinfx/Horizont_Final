using Android;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using HorizontApp.Providers;
using Android.Content.PM;
using static Android.Views.View;
using System.Timers;
using HorizontApp.Utilities;
using HorizontApp.Domain.Models;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Views;
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
using System.Collections.Generic;
using HorizontApp.Domain.Enums;

namespace HorizontApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    //[Activity(Theme = "@android:style/Theme.DeviceDefault.NoActionBar.Fullscreen", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AppCompatActivity, IOnClickListener, GestureDetector.IOnGestureListener
    {
        private static readonly int REQUEST_LOCATION_PERMISSION = 0;
        private static readonly int REQUEST_CAMERA_PERMISSION = 1;

        private static readonly int ReqCode_SelectCategoryActivity = 1000;

        TextView headingEditText;
        TextView GPSEditText;
        EditText DistanceEditText;
        TextView filterText;
        Button getHeadingButton;
        Button getGPSButton;
        ImageButton selectCategoryButton;
        ImageButton pauseButton;
        ImageButton recordButton;
        LinearLayout selectCategoryLayout;
        CompassView compassView;
        private CameraFragment cameraFragment;
        SeekBar distanceSeekBar;
        SeekBar heightSeekBar;
        private View layout;
        ImageButton menu;
        private bool favourite = false;
        private bool compassPaused = false;
        private GestureDetector _gestureDetector;

        Timer compassTimer = new Timer();
        Timer locationTimer = new Timer();
        Timer changeFilterTimer = new Timer();

        private GpsLocationProvider gpsLocationProvider = new GpsLocationProvider();
        private CompassProvider compassProvider = new CompassProvider();
        private HeadingStabilizator headingStabilizator = new HeadingStabilizator();
        private GpsLocation myLocation = new GpsLocation();

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
        List<PoiCategory> selectedCategories = new List<PoiCategory>();
        ImageButton imageButtonMountains;
        ImageButton imageButtonLakes;
        ImageButton imageButtonCastles;
        ImageButton imageButtonPalaces;
        ImageButton imageButtonRuins;
        ImageButton imageButtonViewTowers;
        ImageButton imageButtonTransmitters;
        ImageButton imageButtonChurches;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Xamarin.Essentials.Platform.Init(this, bundle);
            // Set our view from the "main" layout resource

            SetContentView(Resource.Layout.activity_main);

            headingEditText = FindViewById<TextView>(Resource.Id.editText1);
            GPSEditText = FindViewById<TextView>(Resource.Id.editText2);

            selectCategoryLayout = FindViewById<LinearLayout>(Resource.Id.linearLayoutSelectCategory);
            selectCategoryLayout.Enabled = false;
            selectCategoryLayout.Visibility = ViewStates.Invisible;

            selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            selectCategoryButton.SetOnClickListener(this);

            filterText = FindViewById<TextView>(Resource.Id.textView1);

            distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarDistance);
            distanceSeekBar.ProgressChanged += SeekBarProgressChanged;

            heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarHeight);
            heightSeekBar.ProgressChanged += SeekBarProgressChanged;

            var menuButton = FindViewById<ImageButton>(Resource.Id.menuButton);
            menuButton.SetOnClickListener(this);

            menu = FindViewById<ImageButton>(Resource.Id.imageButton1);
            menu.SetOnClickListener(this);

            pauseButton = FindViewById<ImageButton>(Resource.Id.buttonPause);
            pauseButton.SetOnClickListener(this);
            
            recordButton = FindViewById<ImageButton>(Resource.Id.buttonRecord);
            recordButton.SetOnClickListener(this);

            compassView = FindViewById<CompassView>(Resource.Id.compassView1);

            layout = FindViewById(Resource.Id.sample_main_layout);

            _gestureDetector = new GestureDetector(this);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                RequestGPSLocationPermissions();
            }
            else
            {
                if (bundle == null)
                {
                    InitializeCameraFragment();
                }
            }

            compassProvider.Start();
            gpsLocationProvider.Start();


            InitializeCompassTimer();
            InitializeLocationTimer();
            InitializeChangeFilterTimer();

            CompassViewSettings.Instance().SettingsChanged += OnSettingsChanged;

            filterButtonsInitialization();
        }

        private void filterButtonsInitialization()
        {
            imageButtonMountains = FindViewById<ImageButton>(Resource.Id.imageButtonSelectMountain);
            imageButtonMountains.SetOnClickListener(this);
            if (CompassViewSettings.Instance().Categories.Contains(PoiCategory.Mountains))
                imageButtonMountains.SetImageResource(Resource.Drawable.c_mountain);
            else
                imageButtonMountains.SetImageResource(Resource.Drawable.c_mountain_grey);
            imageButtonLakes = FindViewById<ImageButton>(Resource.Id.imageButtonSelectLake);
            imageButtonLakes.SetOnClickListener(this);
            if (CompassViewSettings.Instance().Categories.Contains(PoiCategory.Lakes))
                imageButtonLakes.SetImageResource(Resource.Drawable.c_lake);
            else
                imageButtonLakes.SetImageResource(Resource.Drawable.c_lake_grey);
            imageButtonCastles = FindViewById<ImageButton>(Resource.Id.imageButtonSelectCastle);
            imageButtonCastles.SetOnClickListener(this);
            if (CompassViewSettings.Instance().Categories.Contains(PoiCategory.Castles))
                imageButtonCastles.SetImageResource(Resource.Drawable.c_castle);
            else
                imageButtonCastles.SetImageResource(Resource.Drawable.c_castle_grey);
            imageButtonPalaces = FindViewById<ImageButton>(Resource.Id.imageButtonSelectPalace);
            imageButtonPalaces.SetOnClickListener(this);
            if (CompassViewSettings.Instance().Categories.Contains(PoiCategory.Palaces))
                imageButtonPalaces.SetImageResource(Resource.Drawable.c_palace);
            else
                imageButtonPalaces.SetImageResource(Resource.Drawable.c_palace_grey);
            imageButtonTransmitters = FindViewById<ImageButton>(Resource.Id.imageButtonSelectTransmitter);
            imageButtonTransmitters.SetOnClickListener(this);
            if (CompassViewSettings.Instance().Categories.Contains(PoiCategory.Transmitters))
                imageButtonTransmitters.SetImageResource(Resource.Drawable.c_transmitter);
            else
                imageButtonTransmitters.SetImageResource(Resource.Drawable.c_transmitter_grey);
            imageButtonRuins = FindViewById<ImageButton>(Resource.Id.imageButtonSelectRuins);
            imageButtonRuins.SetOnClickListener(this);
            if (CompassViewSettings.Instance().Categories.Contains(PoiCategory.Ruins))
                imageButtonRuins.SetImageResource(Resource.Drawable.c_ruins);
            else
                imageButtonRuins.SetImageResource(Resource.Drawable.c_ruins_grey);
            imageButtonViewTowers = FindViewById<ImageButton>(Resource.Id.imageButtonSelectViewtower);
            imageButtonViewTowers.SetOnClickListener(this);
            if (CompassViewSettings.Instance().Categories.Contains(PoiCategory.ViewTowers))
                imageButtonViewTowers.SetImageResource(Resource.Drawable.c_viewtower);
            else
                imageButtonViewTowers.SetImageResource(Resource.Drawable.c_viewtower_grey);
            imageButtonChurches = FindViewById<ImageButton>(Resource.Id.imageButtonSelectChurch);
            imageButtonChurches.SetOnClickListener(this);
            if (CompassViewSettings.Instance().Categories.Contains(PoiCategory.Churches))
                imageButtonChurches.SetImageResource(Resource.Drawable.c_church);
            else
                imageButtonChurches.SetImageResource(Resource.Drawable.c_church_grey);
        }

        private void InitializeCameraFragment()
        {
            cameraFragment = CameraFragment.NewInstance();
            FragmentManager.BeginTransaction().Replace(Resource.Id.container, cameraFragment).Commit();
        }

        private void InitializeCompassTimer()
        {
            compassTimer.Interval = 40;
            compassTimer.Elapsed += OnCompassTimerElapsed;
            compassTimer.Enabled = true;
        }

        private void InitializeLocationTimer()
        {
            locationTimer.Interval = 3000;
            locationTimer.Elapsed += OnLocationTimerElapsed;
            locationTimer.Enabled = true;
        }

        private void InitializeChangeFilterTimer()
        {
            changeFilterTimer.Interval = 1000;
            changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            changeFilterTimer.AutoReset = false;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            InitializeCameraFragment();
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
            if (cameraFragment == null)
                return;

            switch (v.Id)
            {
                case Resource.Id.menuButton:
                {
                    Intent menuActivityIntent = new Intent(this, typeof(MenuActivity));
                    menuActivityIntent.PutExtra("latitude", myLocation.Latitude);
                    menuActivityIntent.PutExtra("longitude", myLocation.Longitude);
                    menuActivityIntent.PutExtra("altitude", myLocation.Altitude);
                    menuActivityIntent.PutExtra("maxDistance", distanceSeekBar.Progress);
                    menuActivityIntent.PutExtra("minAltitude", heightSeekBar.Progress);
                    StartActivity(menuActivityIntent);
                    break;
                }
                case Resource.Id.imageButton1:
                {
                    favourite = !favourite;
                    if (favourite)
                        menu.SetImageResource(Resource.Drawable.ic_heart2_on);
                    else
                        menu.SetImageResource(Resource.Drawable.ic_heart2);
                    ReloadData(favourite);
                    compassView.Invalidate();
                    break;
                }
                case Resource.Id.buttonPause:
                {
                    compassPaused = !compassPaused;
                    if (compassPaused)
                    {
                        cameraFragment.StopPreview();
                        recordButton.Enabled = false;
                        recordButton.Visibility = ViewStates.Invisible;
                        pauseButton.SetImageResource(Resource.Drawable.ic_pause_on);
                    }
                    else
                    {
                        cameraFragment.StartPreview();
                        recordButton.Enabled = true;
                        recordButton.Visibility = ViewStates.Visible;
                        pauseButton.SetImageResource(Resource.Drawable.ic_pause);
                    }
                    break;
                }
                case Resource.Id.buttonRecord:
                {
                    cameraFragment.TakePicture();
                    break;
                }
                case Resource.Id.buttonCategorySelect:
                {
                    if (selectCategoryLayout.Visibility == ViewStates.Visible)
                    {
                        selectCategoryLayout.Visibility = ViewStates.Invisible;
                    }
                    else if (selectCategoryLayout.Visibility == ViewStates.Invisible)
                    {
                        selectCategoryLayout.Visibility = ViewStates.Visible;
                        selectedCategories = new List<PoiCategory>();
                    }
                        
                    break;
                }
                    
                case Resource.Id.imageButtonSelectMountain:
                    _handleCategoryFilterChanged(PoiCategory.Mountains);
                    imageButtonMountains.SetImageResource(PoiCategoryHelper.GetImage(PoiCategory.Mountains, CompassViewSettings.Instance().Categories.Contains(PoiCategory.Mountains)));
                    break;

                case Resource.Id.imageButtonSelectLake:
                    _handleCategoryFilterChanged(PoiCategory.Lakes);
                    imageButtonLakes.SetImageResource(PoiCategoryHelper.GetImage(PoiCategory.Lakes, CompassViewSettings.Instance().Categories.Contains(PoiCategory.Lakes)));

                    break;

                case Resource.Id.imageButtonSelectCastle:
                    _handleCategoryFilterChanged(PoiCategory.Castles);
                    imageButtonCastles.SetImageResource(PoiCategoryHelper.GetImage(PoiCategory.Castles, CompassViewSettings.Instance().Categories.Contains(PoiCategory.Castles)));

                    break;

                case Resource.Id.imageButtonSelectPalace:
                    _handleCategoryFilterChanged(PoiCategory.Palaces);
                    imageButtonPalaces.SetImageResource(PoiCategoryHelper.GetImage(PoiCategory.Palaces, CompassViewSettings.Instance().Categories.Contains(PoiCategory.Palaces)));

                    break;

                case Resource.Id.imageButtonSelectTransmitter:
                    _handleCategoryFilterChanged(PoiCategory.Transmitters);
                    imageButtonTransmitters.SetImageResource(PoiCategoryHelper.GetImage(PoiCategory.Transmitters, CompassViewSettings.Instance().Categories.Contains(PoiCategory.Transmitters)));

                    break;

                case Resource.Id.imageButtonSelectRuins:
                    _handleCategoryFilterChanged(PoiCategory.Ruins);
                    imageButtonRuins.SetImageResource(PoiCategoryHelper.GetImage(PoiCategory.Ruins, CompassViewSettings.Instance().Categories.Contains(PoiCategory.Ruins)));

                    break;

                case Resource.Id.imageButtonSelectViewtower:
                    _handleCategoryFilterChanged(PoiCategory.ViewTowers);
                    imageButtonViewTowers.SetImageResource(PoiCategoryHelper.GetImage(PoiCategory.ViewTowers, CompassViewSettings.Instance().Categories.Contains(PoiCategory.ViewTowers)));

                    break;

                case Resource.Id.imageButtonSelectChurch:
                    _handleCategoryFilterChanged(PoiCategory.Churches);
                    imageButtonChurches.SetImageResource(PoiCategoryHelper.GetImage(PoiCategory.Churches, CompassViewSettings.Instance().Categories.Contains(PoiCategory.Churches)));

                    break;
            }
        }

        private void _handleCategoryFilterChanged(PoiCategory poiCategory)
        {
            if (CompassViewSettings.Instance().Categories.Contains(poiCategory))
            {
                CompassViewSettings.Instance().Categories.Remove(poiCategory);
            }
            else
            {
                CompassViewSettings.Instance().Categories.Add(poiCategory);
            }

            CompassViewSettings.Instance().HandleSettingsChanged();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == ReqCode_SelectCategoryActivity)
            {
                //ReloadData(favourite);
            }
        }

        private void OnCompassTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!compassPaused)
            {
                headingStabilizator.AddValue(compassProvider.Heading);
            }

            compassView.Heading = headingStabilizator.GetHeading() + compassView.HeadingCorrector;
            headingEditText.Text = $"{Math.Round(headingStabilizator.GetHeading(), 0).ToString()}° + {compassView.HeadingCorrector:F1} | ";
            compassView.Invalidate();
            
        }

        private async void OnLocationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            LoadAndDisplayData();
        }

        private async void OnChangeFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {

            ReloadData(favourite);
            filterText.Visibility = ViewStates.Invisible;
            changeFilterTimer.Stop();
            //filterText.SetTextAppearance(this, Color.Transparent);
        }

        private async void LoadAndDisplayData()
        {
            try
            {
                var newLocation = await gpsLocationProvider.GetLocationAsync();

                if (newLocation == null)
                    return;

                if (myLocation == null || GpsUtils.Distance(myLocation, newLocation) > 100 || Math.Abs(myLocation.Altitude - newLocation.Altitude) > 50)
                {
                    myLocation = newLocation;
                    GPSEditText.Text = ($"Lat: {myLocation.Latitude} Lon: {myLocation.Longitude} Alt: {Math.Round(myLocation.Altitude, 0)}");


                    var points = GetPointsToDisplay(myLocation, distanceSeekBar.Progress, heightSeekBar.Progress, favourite);
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
            filterText.Text = "vyska nad " + heightSeekBar.Progress * 16 + "m, do " + distanceSeekBar.Progress + "km daleko";            
            filterText.Visibility = ViewStates.Visible;
            
            //reset timer
            changeFilterTimer.Stop();
            changeFilterTimer.Start();
            //filterText.SetTextAppearance(this, Color.GreenYellow);
        }

        private void ReloadData(bool favourite)
        {
            try
            {
                if (myLocation == null)
                    return;

                var points = GetPointsToDisplay(myLocation, distanceSeekBar.Progress, heightSeekBar.Progress, favourite);
                compassView.SetPoiViewItemList(points);
                compassView.Invalidate();
            }
            catch (Exception ex)
            {
                PopupDialog("Error", $"Error when loading data. {ex.Message}");
            }
        }

        private PoiViewItemList GetPointsToDisplay(GpsLocation location, double maxDistance, double minAltitude, bool favourite)
        {
            try
            {
                var poiList = Database.GetItems(location, maxDistance);

                PoiViewItemList poiViewItemList = new PoiViewItemList(poiList, location, maxDistance, minAltitude, favourite);
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

        public bool OnDown(MotionEvent e)
        {
            return false;
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            return false;
        }

        public void OnLongPress(MotionEvent e)
        {
            
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            if (e1.RawY < Resources.DisplayMetrics.HeightPixels / 2)
                compassView.OnScroll(distanceX);
            return false;
        }

        public void OnShowPress(MotionEvent e)
        {
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            return false;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            _gestureDetector.OnTouchEvent(e);
            return false;
        }

        public void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            ReloadData(favourite);
        }
    }
}