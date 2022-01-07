using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using Peaks360Lib.Domain.ViewModel;
using Peaks360App.AppContext;
using Peaks360App.DataAccess;
using Peaks360App.Utilities;
using Peaks360App.Views;
using Peaks360App.Providers;
using static Android.Views.View;
using View = Android.Views.View;
using ImageButton = Android.Widget.ImageButton;
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Activities
{
    public abstract class HorizonBaseActivity : Activity, IOnClickListener, GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener, IProgressReceiver
    {
        private static string TAG = "Horizon-BaseActivity";
        private static float MENU_SLIDE_LIMIT = 0.1f;

        protected abstract IAppContext Context { get; }

        protected CompassView _compassView;
        private TextView _textViewNotification;

        protected ImageButton _favouriteButton;
        protected ImageButton _displayTerrainButton;
        protected ImageButton _showPoiListButton;

        protected LinearLayout _seekBars;
        protected LinearLayout _poiInfo;

        private GestureDetector _gestureDetector;

        private DistanceSeekBar _distanceSeekBar;
        protected LinearLayout _activityControlArea;
        protected ProgressBar _progressBar;
        protected LinearLayout _progressBarLayout;

        //for gesture detection
        private int m_PreviousMoveX;
        private int m_PreviousMoveY;
        private int m_FirstMoveX;
        private int m_FirstMoveY;
        private float m_PreviousDistance;
        private float m_PreviousDistanceX;
        private float m_PreviousDistanceY;
        private bool m_IsScaling;
        private CroppingHandle? m_croppingHandle;

        protected abstract bool MoveingAndZoomingEnabled { get; }
        protected abstract bool TwoPointTiltCorrectionEnabled { get; }
        protected abstract bool OnePointTiltCorrectionEnabled { get; }
        protected abstract bool HeadingCorrectionEnabled { get; }
        protected abstract bool ViewAngleCorrectionEnabled { get; }
        protected abstract bool ImageCroppingEnabled { get; }
        protected abstract PoiListActivity.ContextType ContextType { get; }

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
        
        protected void InitializeMenu()
        {
            var buttonList = FindViewById<LinearLayout>(Resource.Id.listOfPoisLinearLayout);
            buttonList.SetOnClickListener(this);

            var buttonDownload = FindViewById<LinearLayout>(Resource.Id.downloadDataLinearLayout);
            buttonDownload.SetOnClickListener(this);

            var buttonSettings = FindViewById<LinearLayout>(Resource.Id.settingsLinearLayout);
            buttonSettings.SetOnClickListener(this);

            var buttonPhotos = FindViewById<LinearLayout>(Resource.Id.photoGalleryLinearLayout);
            buttonPhotos.SetOnClickListener(this);

            var buttonAbout = FindViewById<LinearLayout>(Resource.Id.aboutLinearLayout);
            buttonAbout.SetOnClickListener(this);
        }

        protected void InitializeBaseActivityUI()
        {
            AppContextLiveData.Instance.SetLocale(this);

            _gestureDetector = new GestureDetector(this);

            _textViewNotification = FindViewById<TextView>(Resource.Id.textViewNotification);
            
            _distanceSeekBar = FindViewById<DistanceSeekBar>(Resource.Id.seekBarDistance);
            _distanceSeekBar.Progress = Context.Settings.MaxDistance;
            _distanceSeekBar.ProgressChanged += OnMaxDistanceChanged;

            _poiInfo = FindViewById<LinearLayout>(Resource.Id.mainActivityPoiInfo);
            _poiInfo.SetOnClickListener(this);
            _poiInfo.Visibility = ViewStates.Gone;

            _displayTerrainButton = FindViewById<ImageButton>(Resource.Id.buttonDisplayTerrain);
            _displayTerrainButton.SetOnClickListener(this);
            _displayTerrainButton.SetImageResource(Context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);

            _showPoiListButton = FindViewById<ImageButton>(Resource.Id.buttonPoiList);
            _showPoiListButton.SetOnClickListener(this);

            var _selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            _selectCategoryButton.SetOnClickListener(this);

            FindViewById<Button>(Resource.Id.buttonWiki).SetOnClickListener(this);
            FindViewById<Button>(Resource.Id.buttonMap).SetOnClickListener(this);
            FindViewById<ImageView>(Resource.Id.buttonFavourite).SetOnClickListener(this); 
            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);
            _compassView.LayoutChange += OnLayoutChanged;

            _progressBar = FindViewById<ProgressBar>(Resource.Id.MainActivityProgressBar);
            _progressBarLayout = FindViewById<LinearLayout>(Resource.Id.MainActivityProgressBarLayout);
            _progressBarLayout.Visibility = ViewStates.Invisible;
        }

        private string _maxDistanceMinAltitudeTemplate;

        protected void Start()
        {
            _maxDistanceMinAltitudeTemplate = Resources.GetText(Resource.String.Main_MaxDistanceMinAltitudeTemplate);

            UpdateStatusBar();
        }

        protected override void OnPause()
        {
            base.OnPause();
            Context.DataChanged -= DataChanged;
            Context.HeadingChanged -= HeadingChanged;
            Context.GpsFixAcquired -= GpsFixAcquired;
            Context.GpsFixLost -= GpsFixLost;
            ElevationProfileProvider.Instance().ElevationProfileChanged -= OnElevationProfileChanged;
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            Context.DataChanged += DataChanged;
            Context.HeadingChanged += HeadingChanged;
            Context.GpsFixAcquired += GpsFixAcquired;
            Context.GpsFixLost += GpsFixLost;
            ElevationProfileProvider.Instance().ElevationProfileChanged += OnElevationProfileChanged;
        }

        protected override void OnDestroy()
        {
            Context.DataChanged -= DataChanged;
            Context.HeadingChanged -= HeadingChanged;
            ElevationProfileProvider.Instance().ElevationProfileChanged -= OnElevationProfileChanged;

            base.OnDestroy();
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

        public void HeadingChanged(object sender, HeadingChangedEventArgs e)
        {
            OnHeadingChanged(sender, e);
        }

        public void GpsFixAcquired(object sender, EventArgs e)
        {
            OnGpsFixAcquired(sender);
        }

        public void GpsFixLost(object sender, EventArgs e)
        {
            OnGpsFixLost(sender);
        }
        public virtual void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            _textViewNotification.Visibility = ViewStates.Invisible;
            UpdateStatusBar();
        }

        public virtual void OnHeadingChanged(object sender, HeadingChangedEventArgs e)
        {
        }

        public virtual void OnGpsFixAcquired(object sender)
        {
        }

        public virtual void OnGpsFixLost(object sender)
        {
        }

        public virtual void OnTiltChanged()
        {
        }

        private void OnMaxDistanceChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            ShowMaxDistanceMinAltitudeText();
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
            bool handled = false;
            base.OnTouchEvent(e);
            _gestureDetector.OnTouchEvent(e);

            var touchCount = e.PointerCount;
            int curX = (int)e.GetX();
            int curY = (int)e.GetY();

            /*int curX1 = (int)e.GetX(0);
            int curY1 = (int)e.GetY(0);
            int curX2 = touchCount > 1 ? (int)e.GetX(1) : -1;
            int curY2 = touchCount > 1 ? (int)e.GetY(1) : -1;
            Log.WriteLine(LogPriority.Debug, TAG, $"Event: {e.Action} (X={curX}/{curX1}/{curX2}, Y={curY}/{curY1}/{curY2}, PrevX={m_PreviousMoveX}, PrevY={m_PreviousMoveY}, FirstX={m_FirstMoveX}, FirstY={m_FirstMoveY}, TC={touchCount})");*/

            switch (e.Action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Pointer1Down:
                case MotionEventActions.Pointer2Down:
                    {
                        m_FirstMoveX = m_PreviousMoveX = curX;
                        m_FirstMoveY = m_PreviousMoveY = curY;

                        if (touchCount >= 2)
                        {
                            m_PreviousDistance = Distance(e.GetX(0), e.GetX(1), e.GetY(0), e.GetY(1));
                            m_PreviousDistanceX = Math.Abs(e.GetX(0) - e.GetX(1));
                            m_PreviousDistanceY = Math.Abs(e.GetY(0) - e.GetY(1));

                            m_IsScaling = true;
                        }

                        if (ImageCroppingEnabled)
                        {
                            m_croppingHandle = GetCroppingHandle(e.GetX(), e.GetY());
                        }
                    }
                    break;
                case MotionEventActions.Move:
                    {
                        if (touchCount == 1)
                        {
                            //image moving + heading and tilt correction 
                            var distanceX = m_PreviousMoveX - curX;
                            var distanceY = m_PreviousMoveY - curY;
                            m_PreviousMoveX = curX;
                            m_PreviousMoveY = curY;

                            if (ImageCroppingEnabled && m_croppingHandle != null)
                            {
                                OnCropAdjustment(m_croppingHandle.Value, -distanceX, -distanceY);
                                handled = true;
                            }
                            else if (MoveingAndZoomingEnabled && !m_IsScaling)
                            {
                                if (m_FirstMoveX > 100)
                                {
                                    OnMove(-distanceX, -distanceY);
                                    handled = true;
                                }
                            }
                            else if (HeadingCorrectionEnabled && Math.Abs(m_FirstMoveX - curX) > Math.Abs(m_FirstMoveY - curY))
                            {
                                if (m_FirstMoveX > _compassView.Width * MENU_SLIDE_LIMIT)
                                {
                                    _compassView.OnHeadingCorrection(distanceX);
                                    handled = true;
                                }
                            }
                            else if (OnePointTiltCorrectionEnabled && Math.Abs(m_FirstMoveY - curY) > Math.Abs(m_FirstMoveX - curX))
                            {
                                _compassView.OnTiltCorrection(distanceY, TiltCorrectionType.Both);
                                OnTiltChanged();
                                handled = true;
                            }
                            else if (TwoPointTiltCorrectionEnabled)
                            {
                                _compassView.OnTiltCorrection(distanceY, (e.RawX < Resources.DisplayMetrics.WidthPixels / 2) ? TiltCorrectionType.Left : TiltCorrectionType.Right);
                                OnTiltChanged();
                                handled = true;
                            }
                        }
                        else if (touchCount >= 2)
                        {
                            //image zooming + view angle correction
                            if (MoveingAndZoomingEnabled && touchCount >= 2 && m_IsScaling)
                            {
                                var distance = Distance(e.GetX(0), e.GetX(1), e.GetY(0), e.GetY(1));
                                var scale = (distance - m_PreviousDistance) / DispDistance();
                                m_PreviousDistance = distance;

                                scale += 1;
                                scale = scale * scale;

                                OnZoom(scale, GetScreenWidth() / 2, GetScreenHeight() / 2);
                                handled = true;
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
                                    handled = true;
                                }
                                else
                                {
                                    var scale = (distY - m_PreviousDistanceY) / GetScreenHeight();
                                    m_PreviousDistanceY = distY;
                                    scale += 1;
                                    _compassView.ScaleVerticalViewAngle(scale);
                                    //OnHorizontalViewAngleChange();
                                    Log.WriteLine(LogPriority.Debug, TAG, $"Vertical VA correction: {scale}");
                                    handled = true;
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
                        if (touchCount >= 2)
                        {
                            var remainingTouchIdx = (e.Action == MotionEventActions.Pointer1Up ? 1 : 0);
                            m_FirstMoveX = m_PreviousMoveX = (int)e.GetX(remainingTouchIdx);
                            m_FirstMoveY = m_PreviousMoveY = (int)e.GetY(remainingTouchIdx);
                        }
                        break;
                    }
            }

            UpdateStatusBar();
            return handled;
        }

        protected abstract void UpdateStatusBar();

        protected abstract void OnMove(int distanceX, int distanceY);
        protected abstract void OnZoom(float scale, int x, int y);

        protected virtual void OnPreviousImage() { }
        protected virtual void OnNextImage() { }

        protected abstract void OnCropAdjustment(CroppingHandle handle, float distanceX, float distanceY);

        protected abstract int GetScreenWidth();
        protected abstract int GetScreenHeight();

        protected abstract int GetPictureWidth();
        protected abstract int GetPictureHeight();

        protected virtual CroppingHandle? GetCroppingHandle(float x, float y)
        {
            return null;
        }

        protected void ShowMaxDistanceMinAltitudeText()
        {
            _textViewNotification.Text = String.Format(_maxDistanceMinAltitudeTemplate, _distanceSeekBar.Progress.ToString());
            _textViewNotification.Visibility = ViewStates.Visible;
        }

        private float DispDistance()
        {
            return FloatMath.Sqrt(GetScreenWidth() * GetScreenWidth() + GetScreenHeight() * GetScreenHeight());
        }

        public virtual void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.buttonDisplayTerrain:
                    OnDisplayTarrainButtonClicked();
                    break;
                case Resource.Id.buttonCategorySelect:
                    OnCategoryButtonClicked();
                    break;
                case Resource.Id.buttonPoiList:
                    StartPoisListActivity(ContextType);
                    break;
                case Resource.Id.mainActivityPoiInfo:
                {
                    if (Context.SelectedPoi != null)
                    {
                        Intent editActivityIntent = new Intent(this, typeof(EditActivity));
                        editActivityIntent.PutExtra("Id", Context.SelectedPoi.Poi.Id);
                        StartActivityForResult(editActivityIntent, EditActivity.REQUEST_EDIT_POI);
                    }
                }
                    break;
                case Resource.Id.buttonMap:
                    MapUtilities.OpenMap(Context.SelectedPoi.Poi);
                    break;
                case Resource.Id.buttonWiki:
                    WikiUtilities.OpenWiki(Context.SelectedPoi.Poi);
                    break;
                case Resource.Id.buttonFavourite:
                    OnPoiFavouriteButtonClicked(Context.SelectedPoi.Poi);
                    break;
            }
        }

        protected void StartPoisListActivity(PoiListActivity.ContextType contextType)
        {
            Intent listActivityIntent = new Intent(this, typeof(PoiListActivity));
            listActivityIntent.PutExtra("contextType", (short)contextType);
            listActivityIntent.PutExtra("latitude", Context.MyLocation.Latitude);
            listActivityIntent.PutExtra("longitude", Context.MyLocation.Longitude);
            listActivityIntent.PutExtra("altitude", Context.MyLocation.Altitude);
            listActivityIntent.PutExtra("maxDistance", MaxDistance);
            listActivityIntent.PutExtra("minAltitude", 0);
            StartActivityForResult(listActivityIntent, PoiListActivity.REQUEST_SHOW_POI_LIST);
        }

        protected void StartSettingsActivity()
        {
            Intent settingsActivityIntent = new Intent(this, typeof(SettingsActivity));
            StartActivityForResult(settingsActivityIntent, SettingsActivity.REQUEST_SHOW_SETTINGS);
        }

        private void OnPoiFavouriteButtonClicked(Poi poi)
        {
            poi.Favorite = !poi.Favorite;
            ImageView favouriteButton = FindViewById<ImageView>(Resource.Id.buttonFavourite);
            favouriteButton.SetImageResource(poi.Favorite ? Android.Resource.Drawable.ButtonStarBigOn : Android.Resource.Drawable.ButtonStarBigOff);
            Context.Database.UpdateItemAsync(poi);
            _compassView.Update(poi);
            _compassView.RefreshPoisDoBeDisplayed();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == SettingsActivity.REQUEST_SHOW_SETTINGS && resultCode == EditActivity.RESULT_OK_AND_CLOSE_PARENT)
            {
                Recreate();
            }

            if (requestCode == EditActivity.REQUEST_EDIT_POI)
            {
                if (resultCode == EditActivity.RESULT_OK || resultCode == EditActivity.RESULT_OK_AND_CLOSE_PARENT)
                {
                    //TODO: use Poi model
                    Context.ReloadData();
                    _compassView.RefreshPoisDoBeDisplayed();

                    if (Context.SelectedPoi != null)
                    {
                        _compassView.Update(Context.SelectedPoi.Poi);
                        OnPointSelected(Context.SelectedPoi);
                    }
                    else
                    {
                        HideControls();
                    }
                }
            }
        }

        protected virtual void OnCategoryButtonClicked()
        {
                var dialog = new PoiFilterDialog(this, Context);
                dialog.Show();
        }

        protected virtual void OnDisplayTarrainButtonClicked()
        {
            Context.Settings.ShowElevationProfile = !Context.Settings.ShowElevationProfile;
            _compassView.ShowElevationProfile = Context.Settings.ShowElevationProfile;

            _displayTerrainButton.SetImageResource(_compassView.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);

            ElevationProfileProvider.Instance().CheckAndReloadElevationProfile(this, MaxDistance, Context, this);
        }

        protected bool IsControlsVisible()
        {
            return _activityControlArea.Visibility == ViewStates.Visible;
        }

        protected void HideControls()
        {
            //_seekBars.Visibility = ViewStates.Gone;
            _activityControlArea.Visibility = ViewStates.Gone;
            ActivityHelper.ChangeSystemUiVisibility(this);
        }

        protected void ShowControls()
        {
            //_seekBars.Visibility = ViewStates.Visible;
            _activityControlArea.Visibility = ViewStates.Visible;
            ActivityHelper.ChangeSystemUiVisibility(this);
        }

        public void OnElevationProfileChanged(object sender, ElevationProfileChangedEventArgs e)
        {
            Context.ElevationProfileData = e.ElevationProfileData;
            _compassView.SetElevationProfile(e.ElevationProfileData);
        }

        private void OnPointSelected(PoiViewItem item)
        {
            if (item == null)
            {
                ShowControls();
                _poiInfo.Visibility = ViewStates.Gone;
            }
            else
            {
                HideControls();
                _poiInfo.Visibility = ViewStates.Visible;

                var altitudeText = $"{Resources.GetText(Resource.String.Common_Altitude)}: {item.Poi.Altitude} m";
                var distanceText = $"{Resources.GetText(Resource.String.Common_Distance)}: {(item.GpsLocation.Distance / 1000):F2} km";
                var verticalAngleText = $"{Resources.GetText(Resource.String.Common_VerticalViewAngle)}: {(item.VerticalViewAngle > 0 ? "+" : "")}{item.VerticalViewAngle:F3}°";
                var bearingText = $"{Resources.GetText(Resource.String.Common_Bearing)}: {(item.GpsLocation.Bearing > 0 ? "+" : "")}{item.GpsLocation.Bearing:F0}°";

                FindViewById<TextView>(Resource.Id.textViewPoiName).Text = item.Poi.Name;
                FindViewById<ImageView>(Resource.Id.imageViewInfoAvailable).Visibility = item.IsImportant() ? ViewStates.Visible : ViewStates.Gone;
                FindViewById<TextView>(Resource.Id.textViewPoiPartiallyVisible).Visibility = item.IsFullyVisible() ? ViewStates.Invisible : ViewStates.Visible;
                FindViewById<TextView>(Resource.Id.textViewPoiDescription).Text = bearingText + " / " + verticalAngleText;
                FindViewById<TextView>(Resource.Id.textViewPoiGpsLocation).Text = altitudeText + " / " + distanceText;
                //FindViewById<TextView>(Resource.Id.textViewPoiData).Text = $"{Resources.GetText(Resource.String.Common_GPSLocation)}: {item.GpsLocation.LocationAsString()}";
                FindViewById<Button>(Resource.Id.buttonWiki).Visibility = WikiUtilities.HasWiki(item.Poi) ? ViewStates.Visible : ViewStates.Gone;
                //FindViewById<Button>(Resource.Id.buttonWiki).Text = Resources.GetText(Resource.String.Common_Details);
                //FindViewById<Button>(Resource.Id.buttonMap).Text = Resources.GetText(Resource.String.Common_Map);

                var favouriteResId = item.Poi.Favorite ? Android.Resource.Drawable.ButtonStarBigOn : Android.Resource.Drawable.ButtonStarBigOff;
                FindViewById<ImageView>(Resource.Id.buttonFavourite).SetImageResource(favouriteResId);
            }

        }

        public void OnProgressStart()
        {
            _progressBarLayout.Visibility = ViewStates.Visible;
            _progressBar.Progress = 0;
        }

        public void OnProgressFinish()
        {
            _progressBarLayout.Visibility = ViewStates.Invisible;
        }

        public void OnProgressChange(int percent)
        {
            _progressBar.Progress = percent;
        }

        #region Required abstract methods
        public virtual bool OnDown(MotionEvent e) { return false; }
        public virtual bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (velocityX > 4000)
            {
                OnPreviousImage();
                return true;
            }

            if (velocityX < -4000)
            {
                OnNextImage();
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
            if (!ImageCroppingEnabled)
            {
                var newSelectedPoi = _compassView.GetPoiByScreenLocation(e.GetX(0), e.GetY(0), Context.DisplayOverlapped);

                if (Context.SelectedPoi == null && newSelectedPoi == null)
                {
                    if (IsControlsVisible())
                    {
                        HideControls();
                    }
                    else
                    {
                        ShowControls();
                    }

                    return false;
                }

                if (Context.SelectedPoi != null)
                {
                    Context.SelectedPoi.Selected = false;
                }

                if (newSelectedPoi != null)
                {
                    Context.SelectedPoi = newSelectedPoi;
                    Context.SelectedPoi.Selected = true;
                }
                else
                {
                    Context.SelectedPoi = null;
                }

                OnPointSelected(Context.SelectedPoi);

                _compassView.Invalidate();
            }

            return false;
        }
        #endregion Required abstract methods
    }
}