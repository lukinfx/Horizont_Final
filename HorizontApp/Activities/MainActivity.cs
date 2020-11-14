using System;
using System.Timers;
using System.Collections.Generic;
using Java.Lang;
using Exception = System.Exception;
using Math = System.Math;
using String = System.String;
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
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Xamarin.Essentials;
using HorizontApp.Utilities;
using HorizontApp.Views;
using HorizontApp.Views.Camera;
using HorizontApp.Activities;
using HorizontLib.Domain.Enums;
using HorizontApp.Tasks;
using HorizontApp.AppContext;
using SQLitePCL;

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
        private CompassView _compassView;
        private SeekBar _distanceSeekBar;
        private SeekBar _heightSeekBar;
        private View _mainLayout;
        
        private CameraFragment _cameraFragment;

        private static bool _firstStart = true;

        private bool _elevationProfileBeingGenerated = false;

        private GestureDetector _gestureDetector;

        private IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Xamarin.Essentials.Platform.Init(this, bundle);

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
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                RequestGPSLocationPermissions();
            }
            else
            {
                InitializeCameraFragment();
                Context.Start();
            }

            Context.DataChanged += OnDataChanged;
            Context.HeadingChanged += OnHeadingChanged;

            InitializeUIElements();

            if (bundle != null)
            {
                var delayedAction = new System.Threading.Timer(o =>
                    {
                        RefreshElevationProfile();
                    },
                    null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
            }

        }

        protected override void OnPause()
        {
            base.OnPause();
            Context.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            Context.Resume();
        }

        protected override void OnSaveInstanceState(Bundle bundle)
        {
            base.OnSaveInstanceState(bundle);

            // Save UI state changes to the savedInstanceState.
            // This bundle will be passed to onCreate if the process is
            // killed and restarted.
        }

        private void InitializeUIElements()
        {
            _headingEditText = FindViewById<TextView>(Resource.Id.editText1);
            _GPSEditText = FindViewById<TextView>(Resource.Id.editText2);

            _mainActivitySeekBars = FindViewById<LinearLayout>(Resource.Id.mainActivitySeekBars);
            _mainActivitySeekBars.Enabled = true;
            _mainActivitySeekBars.Visibility = ViewStates.Visible;

            _selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            _selectCategoryButton.SetOnClickListener(this);

            _filterText = FindViewById<TextView>(Resource.Id.textView1);

            _distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarDistance);
            _distanceSeekBar.Progress = Context.Settings.MaxDistance;
            _distanceSeekBar.ProgressChanged += OnMaxDistanceChanged;

            _heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarHeight);
            _heightSeekBar.Progress = Context.Settings.MinAltitute;
            _heightSeekBar.ProgressChanged += OnMinAltitudeChanged;

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
            _displayTerrainButton.SetImageResource(Context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);

            _refreshCorrectorButton = FindViewById<ImageButton>(Resource.Id.buttonResetCorrector);
            _refreshCorrectorButton.SetOnClickListener(this);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);

            _mainLayout = FindViewById(Resource.Id.sample_main_layout);

        }

        protected override void OnStart()
        {
            base.OnStart();
            _compassView.Initialize(Context);

            Android.Content.Context ctx = this;
            if (_firstStart && !Context.Database.IsAnyDownloadedPois())
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton("Yes", (senderAlert, args) =>
                {
                    Intent downloadActivityIntent = new Intent(ctx, typeof(DownloadActivity));
                    StartActivity(downloadActivityIntent);
                    //_adapter.NotifyDataSetChanged();
                });
                alert.SetNegativeButton("No", (senderAlert, args) => { });
                alert.SetMessage("No points of interest have been downloaded yet. Do you want do download them now?");
                var answer = alert.Show();
            }

            _firstStart = false;
        }

        private void InitializeCameraFragment()
        {
            _cameraFragment = CameraFragment.NewInstance();
            FragmentManager.BeginTransaction().Replace(Resource.Id.container, _cameraFragment).Commit();
        }

        public async void OnClick(Android.Views.View v)
        {
            try
            {
                switch (v.Id)
                {
                    case Resource.Id.menuButton:
                        {
                            Intent menuActivityIntent = new Intent(this, typeof(MenuActivity));
                            menuActivityIntent.PutExtra("latitude", Context.MyLocation.Latitude);
                            menuActivityIntent.PutExtra("longitude", Context.MyLocation.Longitude);
                            menuActivityIntent.PutExtra("altitude", Context.MyLocation.Altitude);
                            menuActivityIntent.PutExtra("maxDistance", _distanceSeekBar.Progress);
                            menuActivityIntent.PutExtra("minAltitude", _heightSeekBar.Progress);
                            StartActivity(menuActivityIntent);
                            break;
                        }
                    case Resource.Id.favouriteFilterButton:
                        {
                            Context.Settings.ToggleFavourite(); ;
                            if (Context.Settings.ShowFavoritesOnly)
                                _favouriteButton.SetImageResource(Resource.Drawable.ic_heart2_on);
                            else
                                _favouriteButton.SetImageResource(Resource.Drawable.ic_heart2);
                            break;
                        }
                    case Resource.Id.buttonPause:
                        {
                            Context.ToggleCompassPaused();
                            if (Context.CompassPaused)
                            {
                                /*if (_cameraFragment != null)
                                {
                                    _cameraFragment.StopPreview();
                                }*/

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
                            HandleDisplayTarrainButtonClicked();
                            break;
                        }
                    case Resource.Id.buttonRecord:
                        {
                            _recordButton.SetImageResource(Resource.Drawable.ic_photo2);
                            _cameraFragment.TakePicture(Context);
                            Timer timer = new Timer(500);
                            timer.Elapsed += OnTakePictureTimerElapsed;
                            timer.Enabled = true;
                            break;
                        }
                    case Resource.Id.buttonCategorySelect:
                        {
                            var dialog = new PoiFilterDialog(this, Context);
                            dialog.Show();
                            break;
                        }
                    case Resource.Id.buttonResetCorrector:
                    {
                        _compassView.HeadingCorrector = 0;
                        Context.Settings.IsManualLocation = false;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", ex.Message);
            }
        }

        #region Request Permissions

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            InitializeCameraFragment();
            Context.Start();
        }

        private void RequestGPSLocationPermissions()
        {
            //From: https://docs.microsoft.com/cs-cz/xamarin/android/app-fundamentals/permissions
            //Sample app: https://github.com/xamarin/monodroid-samples/tree/master/android-m/RuntimePermissions

            var requiredPermissions = new String[]
            {
                Manifest.Permission.AccessFineLocation, 
                Manifest.Permission.Camera, 
                Manifest.Permission.ReadExternalStorage,
                Manifest.Permission.WriteExternalStorage
            };

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.Camera) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadExternalStorage) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.WriteExternalStorage))
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

        #endregion Request Permissions

        #region Elevation Profile Calculation



        private void HandleDisplayTarrainButtonClicked()
        {
            Context.Settings.ShowElevationProfile = !Context.Settings.ShowElevationProfile;
            if (Context.Settings.ShowElevationProfile && (Context.ElevationProfileData == null || Context.ElevationProfileDataDistance < Context.Settings.MaxDistance) && _elevationProfileBeingGenerated == false)
            {
                GenerateElevationProfile();
            }
            _displayTerrainButton.SetImageResource(Context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);
        }

        private void GenerateElevationProfile()
        {
            try {
                if (!GpsUtils.HasAltitude(Context.MyLocation))
                {
                    PopupHelper.ErrorDialog(this, "Error", "It's not possible to generate elevation profile without known altitude");
                    return;
                }

                var ec = new ElevationCalculation(Context.MyLocation, _distanceSeekBar.Progress);

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
                Context.ElevationProfileDataDistance = ec.MaxDistance;
                StartDownloadAndCalculate(ec);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, "Error", $"Error when generating elevation profile. {ex.Message}");
            }
        }

        private void StartDownloadAndCalculate(ElevationCalculation ec)
        {
            _elevationProfileBeingGenerated = true;
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
                _elevationProfileBeingGenerated = false;
                Context.ElevationProfileDataDistance = Context.Settings.MaxDistance;

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

            ec.Execute(Context.MyLocation);
        }

        #endregion Elevation Profile Calculation

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == ReqCode_SelectCategoryActivity)
            {
                //ReloadData(favourite);
            }
        }

        private void OnTakePictureTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _recordButton.SetImageResource(Resource.Drawable.ic_photo1);
        }

        private void RefreshHeading()
        {
            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                _headingEditText.Text = $"{Math.Round(Context.Heading, 0):F0}°+{_compassView.HeadingCorrector + 90:F0} | ";
            }
            else
            {
                _headingEditText.Text = $"{Math.Round(Context.Heading, 0):F0}°+{_compassView.HeadingCorrector:F0} | ";
            }

            _compassView.Invalidate();
        }

        private void RefreshElevationProfile()
        {
            if (Context.ElevationProfileData != null)
            {
                _compassView.SetElevationProfile(Context.ElevationProfileData);
            }
        }

        private void OnMinAltitudeChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            _filterText.Text = "vyska nad " + _heightSeekBar.Progress + "m, do " + _distanceSeekBar.Progress + "km daleko";
            _filterText.Visibility = ViewStates.Visible;

            Context.Settings.MinAltitute = _heightSeekBar.Progress;
        }

        private void OnMaxDistanceChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            //TODO: Save minAltitude and maxDistance to CompassViewSettings
            _filterText.Text = "vyska nad " + _heightSeekBar.Progress + "m, do " + _distanceSeekBar.Progress + "km daleko";
            _filterText.Visibility = ViewStates.Visible;

            Context.Settings.MaxDistance = _distanceSeekBar.Progress;
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            if (e1.RawY < Resources.DisplayMetrics.HeightPixels / 2)
                _compassView.OnScroll(distanceX);
            return false;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            _gestureDetector.OnTouchEvent(e);
            return false;
        }

        public void OnHeadingChanged(object sender, HeadingChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RefreshHeading();
            });
        }

        public void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _filterText.Visibility = ViewStates.Invisible;

                _GPSEditText.Text = GpsUtils.HasLocation(Context.MyLocation) ?
                    $"Lat:{Context.MyLocation.Latitude:F7} Lon:{Context.MyLocation.Longitude:F7} Alt:{Context.MyLocation.Altitude:F0}":"No GPS location";

                _compassView.SetPoiViewItemList(e.PoiData);

                if (Context.Settings.ShowElevationProfile)
                {
                    if (GpsUtils.HasAltitude(Context.MyLocation) && (Context.ElevationProfileData == null || Context.ElevationProfileDataDistance < Context.Settings.MaxDistance) && _elevationProfileBeingGenerated == false)
                    {
                        GenerateElevationProfile();
                    }
                }
            });
        }

        #region Required abstract methods
        public bool OnDown(MotionEvent e) { return false; }
        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY) { return false; }
        public void OnLongPress(MotionEvent e) { }
        public void OnShowPress(MotionEvent e) { }
        public bool OnSingleTapUp(MotionEvent e) { return false; }
        #endregion Required abstract methods
    }
}