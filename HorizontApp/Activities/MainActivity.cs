using System;
using System.Timers;
using System.Collections.Generic;
using Android;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Content.PM;
using Android.Views;
using Android.Content;
using Android.Support.V13.App;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using static Android.Views.View;
using HorizontApp.Utilities;
using HorizontLib.Domain.Models;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Views;
using HorizontApp.Views.Camera;
using HorizontApp.DataAccess;
using HorizontApp.Activities;
using HorizontLib.Domain.Enums;
using HorizontApp.Tasks;
using Java.Lang;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Xamarin.Essentials;
using AppContext = HorizontApp.Utilities.AppContext;
using Exception = System.Exception;
using Math = System.Math;
using String = System.String;

namespace HorizontApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
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
        private ImageButton _displayTerrainButton;
        private ImageButton _refreshCorrectorButton;

        private LinearLayout _mainActivitySeekBars;
        private LinearLayout _mainActivityPoiFilter;
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
        private DisplayOrientation appOrientation;

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

        private AppContext Context { get { return AppContext.Instance; } }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Xamarin.Essentials.Platform.Init(this, bundle);

            appOrientation = DeviceDisplay.MainDisplayInfo.Orientation;
            DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;

            // Set our view from the "main" mainLayout resource
            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            var orientation = mainDisplayInfo.Orientation;
            if (orientation == DisplayOrientation.Portrait)
            {
                SetContentView(Resource.Layout.MainActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.MainActivityLandscape);
            }

            _gestureDetector = new GestureDetector(this);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != Permission.Granted )
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

            if (bundle != null)
            {
                _myLocation.Latitude = bundle.GetDouble("MyLatitude");
                _myLocation.Longitude = bundle.GetDouble("MyLongitude");
                _myLocation.Altitude = bundle.GetDouble("MyAltitude");

                var delayedAction = new System.Threading.Timer(o =>
                    {
                        RefreshLocation();
                        RefreshHeading();
                        RefreshElevationProfile();
                    },
                    null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
            }

        }

        protected override void OnSaveInstanceState(Bundle bundle)
        {
            base.OnSaveInstanceState(bundle);

            // Save UI state changes to the savedInstanceState.
            // This bundle will be passed to onCreate if the process is
            // killed and restarted.
            bundle.PutDouble("MyLatitude", _myLocation.Latitude);
            bundle.PutDouble("MyLongitude", _myLocation.Longitude);
            bundle.PutDouble("MyAltitude", _myLocation.Altitude);

            _compassTimer.Elapsed -= OnCompassTimerElapsed;
            _locationTimer.Elapsed -= OnLocationTimerElapsed;
        }

        private void InitializeUIElements()
        {
            _headingEditText = FindViewById<TextView>(Resource.Id.editText1);
            _GPSEditText = FindViewById<TextView>(Resource.Id.editText2);

            _mainActivityPoiFilter = FindViewById<LinearLayout>(Resource.Id.mainActivityPoiFilter);
            _mainActivityPoiFilter.Enabled = false;
            _mainActivityPoiFilter.Visibility = ViewStates.Invisible;

            _mainActivitySeekBars = FindViewById<LinearLayout>(Resource.Id.mainActivitySeekBars);
            _mainActivitySeekBars.Enabled = true;
            _mainActivitySeekBars.Visibility = ViewStates.Visible;

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

            _displayTerrainButton = FindViewById<ImageButton>(Resource.Id.buttonDisplayTerrain);
            _displayTerrainButton.SetOnClickListener(this);

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


            var buttonSave = FindViewById<Button>(Resource.Id.buttonSavePoiFilter);
            buttonSave.SetOnClickListener(this);
            var buttonSelectAll = FindViewById<Button>(Resource.Id.buttonSelectAll);
            buttonSelectAll.SetOnClickListener(this);
            var buttonSelectNone = FindViewById<Button>(Resource.Id.buttonSelectNone);
            buttonSelectNone.SetOnClickListener(this);
        }

        private void InitializeCameraFragment()
        {
            _cameraFragment = CameraFragment.NewInstance();
            FragmentManager.BeginTransaction().Replace(Resource.Id.container, _cameraFragment).Commit();
        }

        private void InitializeCompassProvider()
        {
            Context.CompassProvider.Start();

            _compassTimer.Interval = 100;
            _compassTimer.Elapsed += OnCompassTimerElapsed;
            _compassTimer.Enabled = true;
        }

        private void InitializeLocationProvider()
        {
            Context.GpsLocationProvider.Start();

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

        private async void LoadAndDisplayData()
        {
            try
            {
                var newLocation = await Context.GpsLocationProvider.GetLocationAsync();

                if (newLocation == null)
                    return;

                var distance = GpsUtils.Distance(_myLocation, newLocation);
                if (distance > 100 || Math.Abs(_myLocation.Altitude - newLocation.Altitude) > 50)
                {
                    bool needRefresh = false;
                    if (distance > 100)
                    {
                        _myLocation.Latitude = newLocation.Latitude;
                        _myLocation.Longitude = newLocation.Longitude;
                        needRefresh = true;
                    }

                    //keep old location if new location has no altitude
                    if (!GpsUtils.HasAltitude(_myLocation) || GpsUtils.HasAltitude(newLocation))
                    {
                        _myLocation.Altitude = newLocation.Altitude;
                        needRefresh = true;
                    }

                    if (needRefresh)
                    {
                        RefreshLocation();
                    }
                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", ex.Message);
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
                PopupHelper.ErrorDialog(this, "Error", $"Error when loading data. {ex.Message}");
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

            var requiredPermissions = new String[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.Camera, Manifest.Permission.ReadExternalStorage };

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.Camera) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadExternalStorage) )
            {
                Snackbar.Make(_mainLayout, "Internal storage, location and camera permissions are needed to show relevant data.", Snackbar.LengthIndefinite)
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
            try {
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
                            if (_cameraFragment != null)
                            {
                                _cameraFragment.StopPreview();
                            }

                            _recordButton.Enabled = false;
                            _recordButton.Visibility = ViewStates.Invisible;
                            _pauseButton.SetImageResource(Resource.Drawable.ic_pause_on);
                        }
                        else
                        {
                            if (_cameraFragment != null)
                            {
                                _cameraFragment.StartPreview();
                            }

                            _recordButton.Enabled = true;
                            _recordButton.Visibility = ViewStates.Visible;
                            _pauseButton.SetImageResource(Resource.Drawable.ic_pause);
                        }
                        break;
                    }
                    case Resource.Id.buttonDisplayTerrain:
                    {
                        GenerateElevationProfile();
                        break;
                    }
                    case Resource.Id.buttonRecord:
                    {
                        _cameraFragment.TakePicture(_myLocation, Context.CompassProvider.Heading);
                        break;
                    }
                    case Resource.Id.buttonCategorySelect:
                    {
                        if (_mainActivityPoiFilter.Visibility == ViewStates.Visible)
                        {
                            _mainActivityPoiFilter.Visibility = ViewStates.Invisible;
                            _mainActivitySeekBars.Visibility = ViewStates.Visible;
                        }
                        else if (_mainActivityPoiFilter.Visibility == ViewStates.Invisible)
                        {
                            _mainActivityPoiFilter.Visibility = ViewStates.Visible;
                            _mainActivitySeekBars.Visibility = ViewStates.Invisible;
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
                    case Resource.Id.buttonSavePoiFilter:
                        _mainActivityPoiFilter.Visibility = ViewStates.Invisible;
                        break;
                    case Resource.Id.buttonSelectAll:
                        OnCategoryFilterSelectAll();
                        break;
                    case Resource.Id.buttonSelectNone:
                        OnCategoryFilterSelectNone();
                        break;


                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", ex.Message);
            }
        }

        private void GenerateElevationProfile()
        {
            try {
                if (!GpsUtils.HasAltitude(_myLocation))
                {
                    PopupHelper.ErrorDialog(this, "Error", "It's not possible to generate elevation profile without known altitude");
                    return;
                }

                var ec = new ElevationCalculation(_myLocation, _distanceSeekBar.Progress);

                var size = ec.GetSizeToDownload();
                if (size == 0)
                {
                    StartDownloadAndCalculate(ec);
                    return;
                }

                using (var builder = new AlertDialog.Builder(this))
                {
                    builder.SetTitle("Question");
                    builder.SetMessage($"This action requires to download additional {size} MBytes. Possibly set lower visibility to reduce amount downloaded data. \r\n\r\nDo you really want to continue?");
                    builder.SetIcon(Android.Resource.Drawable.IcMenuHelp);
                    builder.SetPositiveButton("OK", (senderAlert, args) => { StartDownloadAndCalculateAsync(ec); });
                    builder.SetNegativeButton("Cancel", (senderAlert, args) => { });

                    var myCustomDialog = builder.Create();

                    myCustomDialog.Show();
                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when generating elevation profile. {ex.Message}");
            }
        }

        private void StartDownloadAndCalculateAsync(ElevationCalculation ec)
        {
            try
            {
                StartDownloadAndCalculate(ec);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when generating elevation profile. {ex.Message}");
            }
        }

        private void StartDownloadAndCalculate(ElevationCalculation ec)
        {
            var lastProgressUpdate = System.Environment.TickCount;

            var pd = new ProgressDialog(this);
            pd.SetMessage("Loading elevation data. Please Wait.");
            pd.SetCancelable(false);
            pd.SetProgressStyle(ProgressDialogStyle.Horizontal);
            pd.Show();

            ec.OnFinishedAction = (result) =>
            {
                pd.Hide();
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    PopupHelper.ErrorDialog(this, "Error", result.ErrorMessage);
                }

                Context.ElevationProfileData = result;
                RefreshElevationProfile();
            };
            ec.OnStageChange = (text, max) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    pd.SetMessage(text);
                    pd.Max = max;
                });
            };
            ec.OnProgressChange = (progress) =>
            {
                var tickCount = System.Environment.TickCount;
                if (tickCount - lastProgressUpdate > 100)
                {
                    MainThread.BeginInvokeOnMainThread(() => { pd.Progress = progress; });
                    Thread.Sleep(50);
                    lastProgressUpdate = tickCount;
                }
            };

            ec.Execute(_myLocation);
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

        private void OnCategoryFilterSelectAll()
        {
            IEnumerable<PoiCategory> a = (IEnumerable<PoiCategory>)System.Enum.GetValues(typeof(PoiCategory));
            foreach (var category in a)
            {
                if (CompassViewSettings.Instance().Categories.Contains(category))
                {
                    continue;
                }
                else
                {
                    var imageButton = _imageButtonCategoryFilter[category];
                    CompassViewSettings.Instance().Categories.Add(category);
                    imageButton.SetImageResource(PoiCategoryHelper.GetImage(category, true));
                }
            }
            CompassViewSettings.Instance().HandleSettingsChanged();
        } 

        private void OnCategoryFilterSelectNone()
        {
            foreach (var category in CompassViewSettings.Instance().Categories)
            {
                var imageButton = _imageButtonCategoryFilter[category];
                imageButton.SetImageResource(PoiCategoryHelper.GetImage(category, false));
            }

            CompassViewSettings.Instance().Categories.Clear();
            
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
                //TODO:Move _headingStabilizator to _compassProvider class
                Context.HeadingStabilizator.AddValue(Context.CompassProvider.Heading);
            }

            RefreshHeading();
        }

        private void RefreshHeading()
        {
            _compassView.Heading = Context.HeadingStabilizator.GetHeading() + _compassView.HeadingCorrector;
            if (appOrientation == DisplayOrientation.Portrait)
            {
                _headingEditText.Text = $"{Math.Round(Context.HeadingStabilizator.GetHeading(), 0):F0}°+{_compassView.HeadingCorrector + 90:F0} | ";
            }
            else
            {
                _headingEditText.Text = $"{Math.Round(Context.HeadingStabilizator.GetHeading(), 0):F0}°+{_compassView.HeadingCorrector:F0} | ";
            }

            _compassView.Invalidate();
        }

        private void RefreshLocation()
        {
            try
            {
                _GPSEditText.Text = ($"Lat:{_myLocation.Latitude:F7} Lon:{_myLocation.Longitude:F7} Alt:{_myLocation.Altitude:F0}");

                var points = GetPointsToDisplay(_myLocation, _distanceSeekBar.Progress, _heightSeekBar.Progress, _favourite);
                _compassView.SetPoiViewItemList(points);
            }
            catch (Exception)
            {
                //TODO: handle this
            }
        }

        private void RefreshElevationProfile()
        {
            if (Context.ElevationProfileData != null)
            {
                _compassView.SetElevationProfile(Context.ElevationProfileData);
            }
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

        private void OnMainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
        {
            // Process changes
            
        }
    }
}