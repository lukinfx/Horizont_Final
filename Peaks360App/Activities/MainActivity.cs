﻿using System;
using System.Timers;
using Android;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Content.PM;
using Android.Content;
using Android.Support.V13.App;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Peaks360Lib.Domain.ViewModel;
using Xamarin.Essentials;
using Peaks360App.Utilities;
using Peaks360App.Views.Camera;
using Peaks360App.Activities;
using Peaks360App.AppContext;
using Peaks360App.Services;
using Xamarin.Forms;
using Exception = System.Exception;
using Math = System.Math;
using String = System.String;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using ImageButton = Android.Widget.ImageButton;
using View = Android.Views.View;

namespace Peaks360App
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : HorizonBaseActivity
    {
        private static readonly int REQUEST_PERMISSIONS = 0;

        //private static readonly int ReqCode_SelectCategoryActivity = 1000;

        //UI elements
        private ImageButton _pauseButton;
        private ImageButton _recordButton;
        private ImageButton _menuButton;
        private ImageButton _refreshCorrectorButton;

        private View _mainLayout;
        
        private CameraFragment _cameraFragment;

        private static bool _firstStart = true;

        protected override IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override bool MoveingAndZoomingEnabled => false;
        protected override bool TiltCorrectionEnabled => false;
        protected override bool HeadingCorrectionEnabled => true;
        protected override bool ViewAngleCorrectionEnabled => false;
        protected override bool ImageCroppingEnabled => false;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            AppContextLiveData.Instance.SetLocale(this);
            Xamarin.Forms.Forms.Init(this, bundle);
            Xamarin.Essentials.Platform.Init(this, bundle);

            if (AppContextLiveData.Instance.IsPortrait)
            {
                SetContentView(Resource.Layout.MainActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.MainActivityLandscape);
            }

            InitializeBaseActivityUI();
            InitializeUIElements();

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadUserDictionary) != Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteUserDictionary) != Permission.Granted)
            {
                RequestGPSLocationPermissions();
            }
            else
            {
                InitializeCameraFragment();
                Context.Start();
            }


            Start();
            Context.HeadingChanged += OnHeadingChanged;

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

            CheckAndReloadElevationProfile();
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
            _activityControlBar = FindViewById<LinearLayout>(Resource.Id.mainActivityControlBar);

            _menuButton = FindViewById<ImageButton>(Resource.Id.menuButton);
            _menuButton.SetOnClickListener(this);

            _pauseButton = FindViewById<ImageButton>(Resource.Id.buttonPause);
            _pauseButton.SetOnClickListener(this);

            _recordButton = FindViewById<ImageButton>(Resource.Id.buttonRecord);
            _recordButton.SetOnClickListener(this);

            _refreshCorrectorButton = FindViewById<ImageButton>(Resource.Id.buttonResetCorrector);
            _refreshCorrectorButton.SetOnClickListener(this);

            _compassView.Initialize(Context, true, Context.Settings.CameraPictureSize);

            _mainLayout = FindViewById(Resource.Id.sample_main_layout);
        }

        protected override void OnStart()
        {
            base.OnStart();

            Android.Content.Context ctx = this;

            bool isFirstStart = _firstStart;
            _firstStart = false;

            //For checking the GPS Status
            bool gpsAvailable = DependencyService.Get<IGpsService>().isGpsAvailable();
            if (isFirstStart && !gpsAvailable)
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Yes), (senderAlert, args) =>
                {
                    DependencyService.Get<IGpsService>().OpenSettings();
                });
                alert.SetNegativeButton(Resources.GetText(Resource.String.No), (senderAlert, args) => { });
                alert.SetMessage(Resources.GetText(Resource.String.Main_EnableGpsQuestion));
                var answer = alert.Show();

            }

            if (isFirstStart && !Context.Database.IsAnyDownloadedPois())
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetPositiveButton(Resources.GetText(Resource.String.Yes), (senderAlert, args) =>
                {
                    Intent downloadActivityIntent = new Intent(ctx, typeof(DownloadActivity));
                    StartActivity(downloadActivityIntent);
                    //_adapter.NotifyDataSetChanged();
                });
                alert.SetNegativeButton(Resources.GetText(Resource.String.No), (senderAlert, args) => { });
                alert.SetMessage(Resources.GetText(Resource.String.Main_DownloadDataQuestion));
                var answer = alert.Show();
            }
        }

        private void InitializeCameraFragment()
        {
            _cameraFragment = CameraFragment.NewInstance();
            FragmentManager.BeginTransaction().Replace(Resource.Id.container, _cameraFragment).Commit();
        }

        public override void OnClick(View v)
        {
            base.OnClick(v);

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
                            menuActivityIntent.PutExtra("maxDistance", MaxDistance);
                            menuActivityIntent.PutExtra("minAltitude", MinHeight);
                            StartActivity(menuActivityIntent);
                            break;
                        }
                    case Resource.Id.buttonPause:
                        {
                            HandleButtonPauseClicked();
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
                    case Resource.Id.buttonResetCorrector:
                    {
                        Context.HeadingCorrector = 0;
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

        private void HandleButtonPauseClicked()
        {
            Context.ToggleCompassPaused();
            if (Context.CompassPaused)
            {
                _pauseButton.SetImageResource(Resource.Drawable.ic_pause_on);
            }
            else
            {
                _pauseButton.SetImageResource(Resource.Drawable.ic_pause);
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
                Manifest.Permission.WriteExternalStorage,
                Manifest.Permission.ReadUserDictionary,
                Manifest.Permission.WriteUserDictionary
            };

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.Camera) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadExternalStorage) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.WriteExternalStorage) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadUserDictionary) ||
                ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.WriteUserDictionary))
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

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            /*if (requestCode == ReqCode_SelectCategoryActivity)
            {
                ReloadData(favourite);
            }*/
        }

        private void OnTakePictureTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _recordButton.SetImageResource(Resource.Drawable.ic_photo1);
        }

        private void RefreshHeading()
        {
            _compassView.Invalidate();
        }

        private void RefreshElevationProfile()
        {
            if (Context.ElevationProfileData != null)
            {
                _compassView.SetElevationProfile(Context.ElevationProfileData);
            }
        }

        public void OnHeadingChanged(object sender, HeadingChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RefreshHeading();
            });
        }

        public override void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            base.OnDataChanged(sender, e);

            _compassView.SetPoiViewItemList(e.PoiData);

            CheckAndReloadElevationProfile();
        }

        protected override void UpdateStatusBar()
        {
            if (GpsUtils.HasLocation(Context.MyLocation))
            {
                var gpsLocation = $"GPS:{Context.MyLocation.LocationAsString()} Alt:{Context.MyLocation.Altitude:F0}m";
                SetStatusLineText($"{gpsLocation} ({Context.MyLocationName})");
            }
            else
            {
                SetStatusLineText(Resources.GetText(Resource.String.Main_WaitingForGps), true);
            }
        }

        protected override void OnMove(int distanceX, int distanceY)
        {
            //move functionality which is not supported here
        }

        protected override void OnZoom(float scale, int x, int y)
        {
            //zoom functionality which is not supported here
        }

        protected override void OnCropAdjustment(CroppingHandle handle, float distanceX, float distanceY)
        {
            //cropping functionality which is not supported here
        }

        protected override int GetScreenWidth()
        {
            //it's just for zoom functionality which is not supported here
            return 0;
        }

        protected override int GetScreenHeight()
        {
            //it's just for zoom functionality which is not supported here
            return 0;
        }

        protected override int GetPictureWidth()
        {
            //it's just for move/zoom functionality which is not supported here, but anyway...
            return Context.Settings.CameraPictureSize.Width;
        }

        protected override int GetPictureHeight()
        {
            //it's just for move/zoom functionality which is not supported here, but anyway...
            return Context.Settings.CameraPictureSize.Height;
        }
    }
}