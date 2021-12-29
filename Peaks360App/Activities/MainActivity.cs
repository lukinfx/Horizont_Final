using System;
using System.Timers;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Content;
using Android.Gms.Ads;
using Android.Views;
using Xamarin.Essentials;
using Peaks360Lib.Domain.ViewModel;
using Peaks360App.Utilities;
using Peaks360App.Views.Camera;
using Peaks360App.Activities;
using Peaks360App.AppContext;
using Peaks360App.Providers;
using Peaks360App.Views.Advertisment;
using Exception = System.Exception;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using ImageButton = Android.Widget.ImageButton;
using View = Android.Views.View;
using AndroidX.DrawerLayout.Widget;
using Peaks360App.Views;

namespace Peaks360App
{
    [Activity(Label = "@string/app_name")]
    public class MainActivity : HorizonBaseActivity, /*DrawerLayout.IDrawerListener,*/ View.IOnTouchListener
    {
        //private static readonly int ReqCode_SelectCategoryActivity = 1000;

        //UI elements
        private ImageButton _pauseButton;
        private ImageButton _recordButton;
        private ImageButton _resetCorrectionButton;
        private SlidingMenuControl _slidingMenu;
        //private TextView _textViewStatusLine;

        private View _mainLayout;
        
        private CameraFragment _cameraFragment;

        private AdView _advertismentView;
        private AdvertismentLoader _advertismentLoader;

        private static bool _firstStart = true;

        protected override IAppContext Context { get { return AppContextLiveData.Instance; } }

        protected override bool MoveingAndZoomingEnabled => false;
        protected override bool TwoPointTiltCorrectionEnabled => false;
        protected override bool OnePointTiltCorrectionEnabled => true; 
        protected override bool HeadingCorrectionEnabled => true;
        protected override bool ViewAngleCorrectionEnabled => false;
        protected override bool ImageCroppingEnabled => false;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            AppContextLiveData.Instance.SetLocale(BaseContext);
            
            Xamarin.Essentials.Platform.Init(this, bundle);
            MobileAds.Initialize(this);

            if (AppContextLiveData.Instance.IsPortrait)
            {
                SetContentView(Resource.Layout.MainActivityPortrait);
            }
            else
            {
                SetContentView(Resource.Layout.MainActivityLandscape);
            }

            InitializeMenu();
            InitializeBaseActivityUI();
            InitializeUIElements();
            InitializeCameraFragment();

            Context.Start();
            Start();

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
            _advertismentView?.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();

            Context.Resume();
            Context.ReloadData();
            ElevationProfileProvider.Instance().CheckAndReloadElevationProfile(this, MaxDistance, Context);
            
            _advertismentView?.Resume();
            
            if (_advertismentLoader != null && !_advertismentLoader.IsLoaded())
            {
                _advertismentLoader.RequestNew();
            }
        }

        protected override void OnDestroy()
        {
            _advertismentView?.Destroy();
            base.OnDestroy();
        }

        protected override void OnSaveInstanceState(Bundle bundle)
        {
            base.OnSaveInstanceState(bundle);

            // Save UI state changes to the savedInstanceState.
            // This bundle will be passed to onCreate if the process is
            // killed and restarted.
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
            {
                var uiOptions =
                    SystemUiFlags.HideNavigation |
                    SystemUiFlags.LayoutHideNavigation |
                    SystemUiFlags.LayoutFullscreen |
                    SystemUiFlags.Fullscreen |
                    SystemUiFlags.LayoutStable |
                    SystemUiFlags.ImmersiveSticky;

                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
            }
        }

        private void InitializeUIElements()
        {
            //_advertismentView = FindViewById<AdView>(Resource.Id.adView);
            //AdRequest adRequest = new AdRequest.Builder().Build();
            //_advertismentView.LoadAd(adRequest);

            //_advertismentLoader = new AdvertismentLoader(this, Resources.GetText(Resource.String.GoogleAdsId_MainActivity_Interstitial));

            _activityControlArea = FindViewById<LinearLayout>(Resource.Id.mainActivityControlArea);
            //_textViewStatusLine = FindViewById<TextView>(Resource.Id.textViewStatusLine);
            //_textViewStatusLine.Selected = true;

            _slidingMenu = FindViewById<SlidingMenuControl>(Resource.Id.drawer_layout);
            _slidingMenu.SetOnTouchListener(this);

            _pauseButton = FindViewById<ImageButton>(Resource.Id.buttonPause);
            _pauseButton.SetOnClickListener(this);
            UpdatePauseButton();

            _recordButton = FindViewById<ImageButton>(Resource.Id.buttonRecord);
            _recordButton.SetOnClickListener(this);

            _resetCorrectionButton = FindViewById<ImageButton>(Resource.Id.buttonResetCorrector);
            _resetCorrectionButton.SetOnClickListener(this);

            _compassView.Initialize(Context, true, Context.Settings.CameraPictureSize);

            _mainLayout = FindViewById(Resource.Id.sample_main_layout);
        }

        protected override void OnStart()
        {
            base.OnStart();

            Android.Content.Context ctx = this;

            if (_firstStart)
            {
                CheckAndRequestAppReview();

                TutorialDialog.ShowTutorial(this, TutorialPart.MainActivity,
                    new TutorialPage[]
                    {
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_compass_calibration, textResourceId = Resource.String.Tutorial_Main_CompassCalibration},
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_heading_correction, textResourceId = Resource.String.Tutorial_Main_HeadingCorrection},
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_horizont_correction_simple, textResourceId = Resource.String.Tutorial_Main_HorizontCorrection},
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_show_poi_data, textResourceId = Resource.String.Tutorial_Main_ShowPoiData},
                        new TutorialPage() {imageResourceId = Resource.Drawable.tutorial_ar_warning, textResourceId = Resource.String.Tutorial_Main_ARWarning},
                    },
                    () =>
                    {
                        _firstStart = false;
                        if (!Context.Database.IsAnyDownloadedPois())
                        {
                            AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
                            alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) =>
                            {
                                Intent downloadActivityIntent = new Intent(ctx, typeof(DownloadActivity));
                                StartActivity(downloadActivityIntent);
                                //_adapter.NotifyDataSetChanged();
                            });
                            alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) => { });
                            alert.SetMessage(Resources.GetText(Resource.String.Main_DownloadDataQuestion));
                            var answer = alert.Show();
                        }

                        ElevationProfileProvider.Instance().CheckAndReloadElevationProfile(this, MaxDistance, Context);
                    });
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
                    case Resource.Id.buttonPause:
                        {
                            HandleButtonPauseClicked();
                            break;
                        }
                    case Resource.Id.buttonRecord:
                        {
                            _recordButton.SetImageResource(Resource.Drawable.ic_photo2);
                            _recordButton.Enabled = false;
                            _cameraFragment.TakePicture(Context);
                            Timer timer = new Timer(500);
                            timer.Elapsed += OnTakePictureTimerElapsed;
                            timer.Enabled = true;
                            break;
                        }
                    case Resource.Id.buttonResetCorrector:
                    {
                        Context.HeadingCorrector = 0;
                        Context.LeftTiltCorrector = 0;
                        Context.RightTiltCorrector = 0;
                            Context.Settings.SetAutoLocation();
                        break;
                    }


                    case Resource.Id.downloadDataLinearLayout:
                        _slidingMenu.CloseMenu();
                        Intent downloadActivityIntent = new Intent(this, typeof(DownloadActivity));
                        StartActivity(downloadActivityIntent);
                        break;
                    case Resource.Id.listOfPoisLinearLayout:
                        _slidingMenu.CloseMenu();
                        StartPoisListActivity();
                        break;
                    case Resource.Id.settingsLinearLayout:
                        _slidingMenu.CloseMenu();
                        StartSettingsActivity();
                        break;
                    case Resource.Id.photoGalleryLinearLayout:
                        _slidingMenu.CloseMenu();
                        Intent photosActivityIntent = new Intent(this, typeof(PhotosActivity));
                        StartActivity(photosActivityIntent);
                        break;
                    case Resource.Id.aboutLinearLayout:
                        _slidingMenu.CloseMenu();
                        Intent aboutActivityIntent = new Intent(this, typeof(AboutActivity));
                        StartActivity(aboutActivityIntent);
                        break;

                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, ex.Message);
            }
        }

        private void HandleButtonPauseClicked()
        {
            Context.ToggleCompassPaused();
            UpdatePauseButton();
            
            _advertismentLoader?.Show();
        }

        private void UpdatePauseButton()
        {
            if (Context.CompassPaused)
            {
                _pauseButton.SetImageResource(Resource.Drawable.ic_pause_on);
            }
            else
            {
                _pauseButton.SetImageResource(Resource.Drawable.ic_pause);
            }
        }

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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _recordButton.SetImageResource(Resource.Drawable.ic_photo1);
                _recordButton.Enabled = true;
            });
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

        public override void OnHeadingChanged(object sender, HeadingChangedEventArgs e)
        {
            base.OnHeadingChanged(sender, e);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateResetButtonVisibility();
                RefreshHeading();
            });
        }

        public override void OnTiltChanged()
        {
            base.OnTiltChanged();
            UpdateResetButtonVisibility();
        }

        private void UpdateResetButtonVisibility()
        {
            _resetCorrectionButton.Visibility = (Context.Settings.IsManualLocation || Math.Abs(Context.HeadingCorrector) > 0.1 || Math.Abs(Context.LeftTiltCorrector) > 0.01 || Math.Abs(Context.RightTiltCorrector) > 0.01)
                ? ViewStates.Visible : ViewStates.Gone;
        }

        public override void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            try
            {
                base.OnDataChanged(sender, e);

                _compassView.SetPoiViewItemList(e.PoiData);

                if (!TutorialDialog.IsInProgress)
                {
                    //TODO: check is the following call is really needed
                    ElevationProfileProvider.Instance().CheckAndReloadElevationProfile(this, MaxDistance, Context);
                }
            }
            catch (Exception)
            {
                //TODO: Possibly log the failure
            }
        }

        protected override void UpdateStatusBar()
        {
            if (GpsUtils.HasLocation(Context.MyLocation))
            {
                var gpsLocation = $"GPS:{Context.MyLocation.LocationAsString()} Alt:{Context.MyLocation.Altitude:F0}m";
                SetStatusLineText($"{gpsLocation} ({Context.MyLocationPlaceInfo.PlaceName})");
            }
            else
            {
                SetStatusLineText(Resources.GetText(Resource.String.Main_WaitingForGps), true);
            }
        }

        private void CheckAndRequestAppReview()
        {
            try
            {
                if (Context.Settings.IsReviewRequired())
                {
                    AlertDialog.Builder alert = new AlertDialog.Builder(this).SetCancelable(false);
                    alert.SetPositiveButton(Resources.GetText(Resource.String.Common_Yes), (senderAlert, args) =>
                    {
                        Context.Settings.SetApplicationRatingCompleted();
                        RequestAppReview();
                    });
                    alert.SetNegativeButton(Resources.GetText(Resource.String.Common_No), (senderAlert, args) =>
                    {
                    });
                    alert.SetMessage(Resources.GetText(Resource.String.Main_ReviewAppQuestion));
                    var answer = alert.Show();
                }
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, ex.Message);
            } 
        }

        private Intent GetRateIntent(string url)
        {
            var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(url));

            intent.AddFlags(ActivityFlags.NoHistory);
            intent.AddFlags(ActivityFlags.MultipleTask);
            if ((int)Build.VERSION.SdkInt >= 21)
            {
                intent.AddFlags(ActivityFlags.NewDocument);
            }
            else
            {
                intent.AddFlags(ActivityFlags.ClearWhenTaskReset);
            }
            intent.SetFlags(ActivityFlags.ClearTop);
            intent.SetFlags(ActivityFlags.NewTask);
            return intent;
        }

        private void RequestAppReview()
        {
            var appId = Android.App.Application.Context.ApplicationInfo.ProcessName;
            var url = $"market://details?id={appId}";
            try
            {
                var intent = GetRateIntent(url);
                StartActivity(intent);
            }
            catch (Exception ex)
            {
                PopupHelper.ErrorDialog(this, ex.Message);
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

        protected void SetStatusLineText(string text, bool alert = false)
        {
            // _textViewStatusLine.Text = text;
            // _textViewStatusLine.SetTextColor(alert ? Android.Graphics.Color.Black : Android.Graphics.Color.DarkGray);
        }

        /*public void OnDrawerClosed(View drawerView)
        {
        }

        public void OnDrawerOpened(View drawerView)
        {
        }

        public void OnDrawerSlide(View drawerView, float slideOffset)
        {
        }

        public void OnDrawerStateChanged(int newState)
        {
        }*/

        public bool OnTouch(View v, MotionEvent e)
        {
            if (_slidingMenu.IsMenuOpened())
                return _slidingMenu.OnTouchEvent(e);

            if (OnTouchEvent(e))
                return true;

            return _slidingMenu.OnTouchEvent(e);
        }


        /*class AdListener : Android.Gms.Ads.AdListener
        {
            MainActivity that;

            public AdListener(MainActivity t)
            {
                that = t;
            }

            public override void OnAdClosed()
            {
                that.RequestNewInterstitial();
            }
        }*/
    }
}