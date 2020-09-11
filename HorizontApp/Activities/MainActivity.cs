using System;
using System.Timers;
using System.Collections.Generic;

using Android;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using HorizontApp.Providers;
using Android.Content.PM;
using Android.Views;
using Android.Content;
using Android.Support.V13.App;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using static Android.Views.View;

using HorizontApp.Utilities;
using HorizontApp.Domain.Models;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Views;
using HorizontApp.Views.Camera;
using HorizontApp.DataAccess;
using HorizontApp.Activities;
using HorizontApp.Domain.Enums;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace HorizontApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    //[Activity(Theme = "@android:style/Theme.DeviceDefault.NoActionBar.Fullscreen", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : AppCompatActivity, IOnClickListener, GestureDetector.IOnGestureListener
    {
        private static readonly int REQUEST_PERMISSIONS = 0;

        private static readonly int ReqCode_SelectCategoryActivity = 1000;

        //UI elements
        private TextView _headingEditText;
        private TextView _GPSEditText;
        private TextView _filterText;
        private ImageButton _selectCategoryButton;
        private ImageButton _pauseButton;
        private ImageButton _recordButton;
        private ImageButton _menuButton;
        private ImageButton _favouriteButton;
        private ImageButton _refreshCorrectorButton;

        private LinearLayout _selectCategoryLayout;
        private CompassView _compassView;
        private SeekBar _distanceSeekBar;
        private SeekBar _heightSeekBar;
        private View _mainLayout;
        private Dictionary<PoiCategory, ImageButton> _imageButtonCategoryFilter = new Dictionary<PoiCategory, ImageButton>();
        private CameraFragment _cameraFragment;

        //Timers
        private Timer _compassTimer = new Timer();
        private Timer _locationTimer = new Timer();
        private Timer _changeFilterTimer = new Timer();

        private bool _favourite = false;
        private bool _compassPaused = false;
        private GestureDetector _gestureDetector;

        private GpsLocationProvider _gpsLocationProvider = new GpsLocationProvider();
        private CompassProvider _compassProvider = new CompassProvider();
        private HeadingStabilizator _headingStabilizator = new HeadingStabilizator();
        private GpsLocation _myLocation = new GpsLocation();

        private PoiDatabase _database;
        public PoiDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    _database = new PoiDatabase();
                }
                return _database;
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Xamarin.Essentials.Platform.Init(this, bundle);
            // Set our view from the "main" mainLayout resource

            SetContentView(Resource.Layout.activity_main);

            _gestureDetector = new GestureDetector(this);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                RequestGPSLocationPermissions();
            }
            else
            {
                InitializeCameraFragment();
            }

            InitializeUIElements();
            InitializeCompassProvider();
            InitializeLocationProvider();
            InitializeChangeFilterTimer();
            InitializeCategoryFilterButtons();

            CompassViewSettings.Instance().SettingsChanged += OnSettingsChanged;
        }

        private void InitializeUIElements()
        {
            _headingEditText = FindViewById<TextView>(Resource.Id.editText1);
            _GPSEditText = FindViewById<TextView>(Resource.Id.editText2);

            _selectCategoryLayout = FindViewById<LinearLayout>(Resource.Id.linearLayoutSelectCategory);
            _selectCategoryLayout.Enabled = false;
            _selectCategoryLayout.Visibility = ViewStates.Invisible;

            _selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            _selectCategoryButton.SetOnClickListener(this);

            _filterText = FindViewById<TextView>(Resource.Id.textView1);

            _distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarDistance);
            _distanceSeekBar.ProgressChanged += OnSeekBarProgressChanged;

            _heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarHeight);
            _heightSeekBar.ProgressChanged += OnSeekBarProgressChanged;

            _menuButton = FindViewById<ImageButton>(Resource.Id.menuButton);
            _menuButton.SetOnClickListener(this);

            _favouriteButton = FindViewById<ImageButton>(Resource.Id.favouriteFilterButton);
            _favouriteButton.SetOnClickListener(this);

            _pauseButton = FindViewById<ImageButton>(Resource.Id.buttonPause);
            _pauseButton.SetOnClickListener(this);

            _recordButton = FindViewById<ImageButton>(Resource.Id.buttonRecord);
            _recordButton.SetOnClickListener(this);

            _refreshCorrectorButton = FindViewById<ImageButton>(Resource.Id.buttonResetCorrector);
            _refreshCorrectorButton.SetOnClickListener(this);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);

            _mainLayout = FindViewById(Resource.Id.sample_main_layout);

        }

        private void InitializeCategoryFilterButton(int resourceId)
        {
            var category = PoiCategoryHelper.GetCategory(resourceId);
            var imageButton = FindViewById<ImageButton>(resourceId);

            imageButton.SetOnClickListener(this);
            bool enabled = CompassViewSettings.Instance().Categories.Contains(category);

            imageButton.SetImageResource(PoiCategoryHelper.GetImage(category, enabled));

            _imageButtonCategoryFilter.Add(category, imageButton);
        }

        private void InitializeCategoryFilterButtons()
        {
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectMountain);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectLake);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectCastle);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectPalace);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectTransmitter);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectRuins);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectViewtower);
            InitializeCategoryFilterButton(Resource.Id.imageButtonSelectChurch);
        }

        private void InitializeCameraFragment()
        {
            _cameraFragment = CameraFragment.NewInstance();
            FragmentManager.BeginTransaction().Replace(Resource.Id.container, _cameraFragment).Commit();
        }

        private void InitializeCompassProvider()
        {
            _compassProvider.Start();

            _compassTimer.Interval = 40;
            _compassTimer.Elapsed += OnCompassTimerElapsed;
            _compassTimer.Enabled = true;
        }

        private void InitializeLocationProvider()
        {
            _gpsLocationProvider.Start();

            _locationTimer.Interval = 3000;
            _locationTimer.Elapsed += OnLocationTimerElapsed;
            _locationTimer.Enabled = true;
        }

        private void InitializeChangeFilterTimer()
        {
            _changeFilterTimer.Interval = 1000;
            _changeFilterTimer.Elapsed += OnChangeFilterTimerElapsed;
            _changeFilterTimer.AutoReset = false;
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

        private async void LoadAndDisplayData()
        {
            try
            {
                var newLocation = await _gpsLocationProvider.GetLocationAsync();

                if (newLocation == null)
                    return;

                if (_myLocation == null || GpsUtils.Distance(_myLocation, newLocation) > 100 || Math.Abs(_myLocation.Altitude - newLocation.Altitude) > 50)
                {
                    _myLocation = newLocation;
                    _GPSEditText.Text = ($"Lat: {_myLocation.Latitude} Lon: {_myLocation.Longitude} Alt: {Math.Round(_myLocation.Altitude, 0)}");


                    var points = GetPointsToDisplay(_myLocation, _distanceSeekBar.Progress, _heightSeekBar.Progress, _favourite);
                    _compassView.SetPoiViewItemList(points);
                }
            }
            catch (Exception ex)
            {
                PopupDialog("Error", ex.Message);
            }

        }

        private void ReloadData(bool favourite)
        {
            try
            {
                if (_myLocation == null)
                    return;

                //TODO: get minAltitude and maxDistance from CompassViewSettings
                var points = GetPointsToDisplay(_myLocation, _distanceSeekBar.Progress, _heightSeekBar.Progress, favourite);
                _compassView.SetPoiViewItemList(points);
                _compassView.Invalidate();
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
                Snackbar.Make(_mainLayout, "Location and camera permissions are needed to show relevant data.", Snackbar.LengthIndefinite)
                    .SetAction("OK", new Action<View>(delegate (View obj) 
                        {
                            ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_PERMISSIONS);
                        })
                    ).Show();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_PERMISSIONS);
            }
        }

        public async void OnClick(Android.Views.View v)
        {
            if (_cameraFragment == null)
                return;

            switch (v.Id)
            {
                case Resource.Id.menuButton:
                {
                    Intent menuActivityIntent = new Intent(this, typeof(MenuActivity));
                    menuActivityIntent.PutExtra("latitude", _myLocation.Latitude);
                    menuActivityIntent.PutExtra("longitude", _myLocation.Longitude);
                    menuActivityIntent.PutExtra("altitude", _myLocation.Altitude);
                    menuActivityIntent.PutExtra("maxDistance", _distanceSeekBar.Progress);
                    menuActivityIntent.PutExtra("minAltitude", _heightSeekBar.Progress);
                    StartActivity(menuActivityIntent);
                    break;
                }
                case Resource.Id.favouriteFilterButton:
                {
                    _favourite = !_favourite;
                    if (_favourite)
                        _favouriteButton.SetImageResource(Resource.Drawable.ic_heart2_on);
                    else
                        _favouriteButton.SetImageResource(Resource.Drawable.ic_heart2);
                    ReloadData(_favourite);
                    _compassView.Invalidate();
                    break;
                }
                case Resource.Id.buttonPause:
                {
                    _compassPaused = !_compassPaused;
                    if (_compassPaused)
                    {
                        _cameraFragment.StopPreview();
                        _recordButton.Enabled = false;
                        _recordButton.Visibility = ViewStates.Invisible;
                        _pauseButton.SetImageResource(Resource.Drawable.ic_pause_on);
                    }
                    else
                    {
                        _cameraFragment.StartPreview();
                        _recordButton.Enabled = true;
                        _recordButton.Visibility = ViewStates.Visible;
                        _pauseButton.SetImageResource(Resource.Drawable.ic_pause);
                    }
                    break;
                }
                case Resource.Id.buttonRecord:
                {
                    _cameraFragment.TakePicture();
                    break;
                }
                case Resource.Id.buttonCategorySelect:
                {
                    if (_selectCategoryLayout.Visibility == ViewStates.Visible)
                    {
                        _selectCategoryLayout.Visibility = ViewStates.Invisible;
                    }
                    else if (_selectCategoryLayout.Visibility == ViewStates.Invisible)
                    {
                        _selectCategoryLayout.Visibility = ViewStates.Visible;
                    }
                        
                    break;
                }
                    
                case Resource.Id.imageButtonSelectMountain:
                case Resource.Id.imageButtonSelectLake:
                case Resource.Id.imageButtonSelectCastle:
                case Resource.Id.imageButtonSelectPalace:
                case Resource.Id.imageButtonSelectTransmitter:
                case Resource.Id.imageButtonSelectRuins:
                case Resource.Id.imageButtonSelectViewtower:
                case Resource.Id.imageButtonSelectChurch:
                    OnCategoryFilterChanged(v.Id);
                    break;
                case Resource.Id.buttonResetCorrector:
                    _compassView.ResetHeadingCorrector();
                    break;
            }
        }

        private void OnCategoryFilterChanged(int resourceId)
        {
            var poiCategory = PoiCategoryHelper.GetCategory(resourceId);
            var imageButton = _imageButtonCategoryFilter[poiCategory];

            if (CompassViewSettings.Instance().Categories.Contains(poiCategory))
            {
                CompassViewSettings.Instance().Categories.Remove(poiCategory);
                imageButton.SetImageResource(PoiCategoryHelper.GetImage(poiCategory, false));
            }
            else
            {
                CompassViewSettings.Instance().Categories.Add(poiCategory);
                imageButton.SetImageResource(PoiCategoryHelper.GetImage(poiCategory, true));
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
            if (!_compassPaused)
            {
                _headingStabilizator.AddValue(_compassProvider.Heading);
            }

            _compassView.Heading = _headingStabilizator.GetHeading() + _compassView.HeadingCorrector;
            _headingEditText.Text = $"{Math.Round(_headingStabilizator.GetHeading(), 0).ToString()}° + {_compassView.HeadingCorrector:F1} | ";
            _compassView.Invalidate();
            
        }

        private async void OnLocationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            LoadAndDisplayData();
        }

        private async void OnChangeFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {

            ReloadData(_favourite);
            _filterText.Visibility = ViewStates.Invisible;
            _changeFilterTimer.Stop();
            //filterText.SetTextAppearance(this, Color.Transparent);
        }
        
        private void OnSeekBarProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            //TODO: Save minAltitude and maxDistance to CompassViewSettings
            _filterText.Text = "vyska nad " + _heightSeekBar.Progress * 16 + "m, do " + _distanceSeekBar.Progress + "km daleko";            
            _filterText.Visibility = ViewStates.Visible;
            
            //reset timer
            _changeFilterTimer.Stop();
            _changeFilterTimer.Start();
            //filterText.SetTextAppearance(this, Color.GreenYellow);
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
                _compassView.OnScroll(distanceX);
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
            ReloadData(_favourite);
        }
    }
}