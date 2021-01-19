using Android.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using HorizontApp.AppContext;
using HorizontApp.DataAccess;
using HorizontApp.Tasks;
using HorizontApp.Utilities;
using HorizontApp.Views;
using HorizontLib.Domain.ViewModel;
using System;
using System.Threading;
using Xamarin.Essentials;
using static Android.Views.View;
using GpsUtils = HorizontApp.Utilities.GpsUtils;
using ImageButton = Android.Widget.ImageButton;
using View = Android.Views.View;

namespace HorizontApp.Activities
{
    public abstract class HorizonBaseActivity : Activity, IOnClickListener, GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener
    {
        private static string TAG = "Horizon-BaseActivity";

        protected abstract IAppContext Context { get; }
        private TextView _headingTextView;

        protected CompassView _compassView;
        private TextView _textViewNotification;

        private ImageButton _favouriteButton;
        private ImageButton _displayTerrainButton;

        private LinearLayout _seekBars;
        private LinearLayout _poiInfo;

        private bool _elevationProfileBeingGenerated = false;

        private GestureDetector _gestureDetector;

        private SeekBar _distanceSeekBar;
        private SeekBar _heightSeekBar;

        //for gesture detection
        private int m_PreviousMoveX;
        private int m_PreviousMoveY;
        private int m_FirstMoveX;
        private int m_FirstMoveY;
        private float m_PreviousDistance;
        private float m_PreviousDistanceX;
        private float m_PreviousDistanceY;
        private bool m_IsScaling;
        private int m_startTime;
        private int m_tapCount = 0;

        protected abstract bool MoveingAndZoomingEnabled { get; }
        protected abstract bool TiltCorrectionEnabled { get; }
        protected abstract bool HeadingCorrectionEnabled { get; }
        protected abstract bool ViewAngleCorrectionEnabled { get; }


        private PoiDatabase _database;
        protected PoiDatabase Database
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

        protected int MaxDistance { get { return _distanceSeekBar.Progress; } }
        protected int MinHeight { get { return _heightSeekBar.Progress; } }
        protected void InitializeBaseActivityUI()
        {
            AppContextLiveData.Instance.SetLocale(this);

            _gestureDetector = new GestureDetector(this);


            _textViewNotification = FindViewById<TextView>(Resource.Id.textViewNotification);

            _headingTextView = FindViewById<TextView>(Resource.Id.editText1);

            _distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarDistance);
            _distanceSeekBar.Progress = Context.Settings.MaxDistance;
            _distanceSeekBar.ProgressChanged += OnMaxDistanceChanged;
            _heightSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarHeight);
            _heightSeekBar.Progress = Context.Settings.MinAltitute;
            _heightSeekBar.ProgressChanged += OnMinAltitudeChanged;

            _seekBars = FindViewById<LinearLayout>(Resource.Id.mainActivitySeekBars);
            _poiInfo = FindViewById<LinearLayout>(Resource.Id.mainActivityPoiInfo);
            _seekBars.Visibility = ViewStates.Visible;
            _poiInfo.Visibility = ViewStates.Gone;

            _displayTerrainButton = FindViewById<ImageButton>(Resource.Id.buttonDisplayTerrain);
            _displayTerrainButton.SetOnClickListener(this);
            _displayTerrainButton.SetImageResource(Context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);

            _favouriteButton = FindViewById<ImageButton>(Resource.Id.favouriteFilterButton);
            _favouriteButton.SetOnClickListener(this);
            _favouriteButton.SetImageResource(Context.ShowFavoritesOnly ? Resource.Drawable.ic_heart2_on : Resource.Drawable.ic_heart2);

            var _selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            _selectCategoryButton.SetOnClickListener(this);

            FindViewById<ImageButton>(Resource.Id.buttonWiki).SetOnClickListener(this);
            FindViewById<ImageButton>(Resource.Id.buttonMap).SetOnClickListener(this);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);
            _compassView.LayoutChange += OnLayoutChanged;
        }

        protected void Start()
        {
            //Finnaly setup OnDataChanged listener and Road all data
            Context.DataChanged += DataChanged;
        }

        public void OnLayoutChanged(object sender, LayoutChangeEventArgs e)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                Context.ReloadData();
            });
        }

        public virtual void DataChanged(object sender, DataChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _textViewNotification.Visibility = ViewStates.Invisible;

                OnDataChanged(sender, e);

                UpdateStatusBar();
            });
        }

        public virtual void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            _textViewNotification.Visibility = ViewStates.Invisible;
            UpdateStatusBar();
        }

        private void OnMinAltitudeChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            _textViewNotification.Text = "vyska nad " + _heightSeekBar.Progress + "m, do " + _distanceSeekBar.Progress + "km daleko";
            _textViewNotification.Visibility = ViewStates.Visible;

            Context.Settings.MinAltitute = _heightSeekBar.Progress;
        }

        private void OnMaxDistanceChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            //TODO: Save minAltitude and maxDistance to CompassViewSettings
            _textViewNotification.Text = "vyska nad " + _heightSeekBar.Progress + "m, do " + _distanceSeekBar.Progress + "km daleko";
            _textViewNotification.Visibility = ViewStates.Visible;

            Context.Settings.MaxDistance = _distanceSeekBar.Progress;
        }

        private float Distance(float x0, float x1, float y0, float y1)
        {
            var x = x0 - x1;
            var y = y0 - y1;
            return FloatMath.Sqrt(x * x + y * y);
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            return false;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            base.OnTouchEvent(e);
            _gestureDetector.OnTouchEvent(e);

            var touchCount = e.PointerCount;
            switch (e.Action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Pointer1Down:
                case MotionEventActions.Pointer2Down:
                    {

                        m_FirstMoveX = m_PreviousMoveX = (int)e.GetX();
                        m_FirstMoveY = m_PreviousMoveY = (int)e.GetY();

                        if (touchCount == 1)
                        {

                            if (System.Environment.TickCount - m_startTime > 500)
                            {
                                m_tapCount = 0;
                            }

                            if (m_tapCount == 0)
                            {
                                m_startTime = System.Environment.TickCount;
                            }

                            m_tapCount++;
                        }

                        if (touchCount >= 2)
                        {
                            m_PreviousDistance = Distance(e.GetX(0), e.GetX(1), e.GetY(0), e.GetY(1));
                            m_PreviousDistanceX = Math.Abs(e.GetX(0) - e.GetX(1));
                            m_PreviousDistanceY = Math.Abs(e.GetY(0) - e.GetY(1));

                            m_IsScaling = true;
                        }
                    }
                    break;
                case MotionEventActions.Move:
                    {
                        //heading and tilt correction
                        if (touchCount == 1)
                        {
                            var distanceX = m_PreviousMoveX - (int)e.GetX();
                            var distanceY = m_PreviousMoveY - (int)e.GetY();
                            m_PreviousMoveX = (int)e.GetX();
                            m_PreviousMoveY = (int)e.GetY();

                            //moving
                            if (MoveingAndZoomingEnabled && !m_IsScaling)
                            {
                                OnMove(-distanceX, -distanceY);
                            }
                            else if (HeadingCorrectionEnabled && Math.Abs(m_FirstMoveX - e.GetX()) > Math.Abs(m_FirstMoveY - e.GetY()))
                            {
                                _compassView.OnScroll(distanceX);
                                Log.WriteLine(LogPriority.Debug, TAG, $"Heading correction: {distanceX}");
                            }
                            else if (TiltCorrectionEnabled)
                            {
                                if (e.RawX < Resources.DisplayMetrics.WidthPixels / 2)
                                {
                                    _compassView.OnScroll(distanceY, true);
                                    Log.WriteLine(LogPriority.Debug, TAG, $"Left tilt correction: {distanceY}");
                                }
                                else
                                {
                                    _compassView.OnScroll(distanceY, false);
                                    Log.WriteLine(LogPriority.Debug, TAG, $"Right tilt correction: {distanceY}");
                                }
                            }
                        }
                        else if (touchCount >= 2)
                        {
                            //zooming
                            if (MoveingAndZoomingEnabled && touchCount >= 2 && m_IsScaling)
                            {
                                var distance = Distance(e.GetX(0), e.GetX(1), e.GetY(0), e.GetY(1));
                                var scale = (distance - m_PreviousDistance) / DispDistance();
                                m_PreviousDistance = distance;

                                scale += 1;
                                scale = scale * scale;

                                OnZoom(scale, GetScreenWidth() / 2, GetScreenHeight() / 2);
                            }
                            else if (ViewAngleCorrectionEnabled)
                            {
                                var distX = Math.Abs(e.GetX(0) - e.GetX(1));
                                var distY = Math.Abs(e.GetY(0) - e.GetY(1));
                                if (distX > distY)
                                {
                                    var scale = (distX - m_PreviousDistanceX) / GetScreenWidth();
                                    m_PreviousDistanceX = distX;
                                    scale += 1;
                                    _compassView.ScaleHorizontalViewAngle(scale);
                                    //OnVerticalViewAngleChange();
                                    Log.WriteLine(LogPriority.Debug, TAG, $"Horizontal VA correction: {scale}");
                                }
                                else
                                {
                                    var scale = (distY - m_PreviousDistanceY) / GetScreenHeight();
                                    m_PreviousDistanceY = distY;
                                    scale += 1;
                                    _compassView.ScaleVerticalViewAngle(scale);
                                    //OnHorizontalViewAngleChange();
                                    Log.WriteLine(LogPriority.Debug, TAG, $"Vertical VA correction: {scale}");
                                }
                            }
                        }
                        break;
                    }
                case MotionEventActions.Up:
                case MotionEventActions.Pointer1Up:
                case MotionEventActions.Pointer2Up:
                    {
                        if (touchCount <= 1)
                        {
                            if (m_IsScaling)
                            {
                                m_IsScaling = false;
                            }
                        }
                        break;
                    }
            }

            UpdateStatusBar();
            return true;
        }

        protected abstract void UpdateStatusBar();

        protected abstract void OnMove(int distanceX, int distanceY);
        protected abstract void OnZoom(float scale, int x, int y);

        protected abstract int GetScreenWidth();
        protected abstract int GetScreenHeight();

        protected abstract int GetPictureWidth();
        protected abstract int GetPictureHeight();

        private float DispDistance()
        {
            return FloatMath.Sqrt(GetScreenWidth() * GetScreenWidth() + GetScreenHeight() * GetScreenHeight());
        }

        public virtual void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonDisplayTerrain:
                    HandleDisplayTarrainButtonClicked();
                    break;

                case Resource.Id.favouriteFilterButton:
                    {
                        Context.ToggleFavourite();
                        _favouriteButton.SetImageResource(Context.ShowFavoritesOnly ? Resource.Drawable.ic_heart2_on : Resource.Drawable.ic_heart2);
                        Context.ReloadData();
                        break;
                    }
                case Resource.Id.buttonCategorySelect:
                    {
                        var dialog = new PoiFilterDialog(this, Context);
                        dialog.Show();

                        break;
                    }
                case Resource.Id.buttonMap:
                    MapUtilities.OpenMap(Context.SelectedPoi.Poi);
                    break;
                case Resource.Id.buttonWiki:
                    WikiUtilities.OpenWiki(Context.SelectedPoi.Poi);
                    break;
            }
        }


        #region ElevationProfile
        private void HandleDisplayTarrainButtonClicked()
        {
            Context.Settings.ShowElevationProfile = !Context.Settings.ShowElevationProfile;
            _displayTerrainButton.SetImageResource(Context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);

            CheckAndReloadElevationProfile();
        }

        protected void CheckAndReloadElevationProfile()
        {
            if (Context.Settings.ShowElevationProfile)
            {
                if (GpsUtils.HasAltitude(Context.MyLocation))
                {
                    if (_elevationProfileBeingGenerated == false)
                    {
                        if (Context.ElevationProfileData == null || !Context.ElevationProfileData.IsValid(Context.MyLocation, Context.Settings.MaxDistance))
                        {
                            GenerateElevationProfile();
                        }
                        else
                        {
                            RefreshElevationProfile();
                        }
                    }
                }
            }

            _compassView.Invalidate();
        }

        protected void GenerateElevationProfile()
        {
            try
            {
                if (!GpsUtils.HasAltitude(Context.MyLocation))
                {
                    PopupHelper.ErrorDialog(this, "Error", "It's not possible to generate elevation profile without known altitude");
                    return;
                }

                _elevationProfileBeingGenerated = true;

                var ec = new ElevationCalculation(Context.MyLocation, MaxDistance);

                var size = ec.GetSizeToDownload();
                if (size == 0)
                {
                    StartDownloadAndCalculate(ec);
                    return;
                }

                using (var builder = new AlertDialog.Builder(this))
                {
                    builder.SetTitle("Question");
                    builder.SetMessage($"This action requires to download additional {size} MBytes. Possibly set lower visibility to reduce amount of downloaded data. \r\n\r\nDo you really want to continue?");
                    builder.SetIcon(Android.Resource.Drawable.IcMenuHelp);
                    builder.SetPositiveButton("OK", (senderAlert, args) => { StartDownloadAndCalculateAsync(ec); });
                    builder.SetNegativeButton("Cancel", (senderAlert, args) => { _elevationProfileBeingGenerated = false; });

                    var myCustomDialog = builder.Create();

                    myCustomDialog.Show();
                }
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

                RefreshElevationProfile();
                _elevationProfileBeingGenerated = false;
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

        protected void RefreshElevationProfile()
        {
            if (Context.ElevationProfileData != null)
            {
                _compassView.SetElevationProfile(Context.ElevationProfileData);
            }
        }
        #endregion ElevationProfile

        #region Required abstract methods
        public virtual bool OnDown(MotionEvent e) { return false; }
        public virtual bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (velocityX > 4000)
            {
                //Previous image
                return true;
            }

            if (velocityX < -4000)
            {
                //Next image
                return true;
            }

            return false;
        }
        public virtual void OnLongPress(MotionEvent e) { }
        public virtual void OnShowPress(MotionEvent e) { }
        public virtual bool OnSingleTapUp(MotionEvent e) { return false; }
        public virtual bool OnDoubleTap(MotionEvent e)
        {
            return false;
        }
        public virtual bool OnDoubleTapEvent(MotionEvent e)
        {
            return false;
        }
        public virtual bool OnSingleTapConfirmed(MotionEvent e)
        {
            var newSelectedPoi = _compassView.GetPoiByScreenLocation(e.GetX(0), e.GetY(0));

            if (Context.SelectedPoi != null)
            {
                Context.SelectedPoi.Selected = false;
            }

            if (newSelectedPoi != null)
            {
                Context.SelectedPoi = newSelectedPoi;
                Context.SelectedPoi.Selected = true;

                _seekBars.Visibility = ViewStates.Gone;
                _poiInfo.Visibility = ViewStates.Visible;
                FindViewById<TextView>(Resource.Id.textViewPoiName).Text = Context.SelectedPoi.Poi.Name;
                FindViewById<TextView>(Resource.Id.textViewPoiDescription).Text = "No description";
                FindViewById<TextView>(Resource.Id.textViewPoiGpsLocation).Text = $"{Context.SelectedPoi.Poi.Altitude} m / {(Context.SelectedPoi.GpsLocation.Distance / 1000):F2} km";
                FindViewById<TextView>(Resource.Id.textViewPoiData).Text = $"{Context.SelectedPoi.Poi.Latitude:F7} N, {Context.SelectedPoi.Poi.Longitude:F7} E";
                FindViewById<ImageButton>(Resource.Id.buttonWiki).Visibility = WikiUtilities.HasWiki(Context.SelectedPoi.Poi) ? ViewStates.Visible : ViewStates.Gone;
            }
            else
            {
                Context.SelectedPoi = null;

                _seekBars.Visibility = ViewStates.Visible;
                _poiInfo.Visibility = ViewStates.Gone;
            }
            _compassView.Invalidate();

            return false;
        }

        #endregion Required abstract methods
    }
}