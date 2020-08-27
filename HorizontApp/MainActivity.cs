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

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Xamarin.Essentials.Platform.Init(this, bundle);
            // Set our view from the "main" layout resource

            SetContentView(Resource.Layout.activity_main);

            headingEditText = FindViewById<TextView>(Resource.Id.editText1);
            GPSEditText = FindViewById<TextView>(Resource.Id.editText2);

            selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            selectCategoryButton.SetOnClickListener(this);

            filterText = FindViewById<TextView>(Resource.Id.textView1);

            distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBar2);
            distanceSeekBar.ProgressChanged += SeekBarProgressChanged;

            heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBar1);
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
        }

        private void InitializeCameraFragment()
        {
            cameraFragment = CameraFragment.NewInstance();
            FragmentManager.BeginTransaction().Replace(Resource.Id.container, cameraFragment).Commit();
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
                    Intent selectCategoryIntent = new Intent(this, typeof(SelectCategoryActivity));
                    StartActivityForResult(selectCategoryIntent, ReqCode_SelectCategoryActivity);
                    
                    break;
            }
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

                compassView.Heading = headingStabilizator.GetHeading();
                headingEditText.Text = Math.Round(headingStabilizator.GetHeading(), 0).ToString() + "° | ";

                compassView.Invalidate();
            }
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

                if (myLocation == null || GpsUtils.Distance(myLocation, newLocation) > 100)
                {
                    myLocation = newLocation;
                    GPSEditText.Text = ($"Latitude: {myLocation.Latitude}, Longitude: {myLocation.Longitude}, Altitude: {Math.Round(myLocation.Altitude, 0)}");


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