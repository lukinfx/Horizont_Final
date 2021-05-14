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

namespace Peaks360App.Activities
{
    public abstract class HorizonBaseActivity : Activity, IOnClickListener, GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener
    {
        private static string TAG = "Horizon-BaseActivity";

        protected abstract IAppContext Context { get; }

        protected CompassView _compassView;
        private TextView _textViewNotification;
        private TextView _textViewStatusLine;

        protected ImageButton _favouriteButton;
        protected ImageButton _displayTerrainButton;

        protected LinearLayout _seekBars;
        protected LinearLayout _poiInfo;

        private GestureDetector _gestureDetector;

        private SeekBar _distanceSeekBar;
        protected LinearLayout _activityControlBar;

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
        private CroppingHandle? m_croppingHandle;

        protected abstract bool MoveingAndZoomingEnabled { get; }
        protected abstract bool TwoPointTiltCorrectionEnabled { get; }
        protected abstract bool OnePointTiltCorrectionEnabled { get; }
        protected abstract bool HeadingCorrectionEnabled { get; }
        protected abstract bool ViewAngleCorrectionEnabled { get; }
        protected abstract bool ImageCroppingEnabled { get; }

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
        protected void InitializeBaseActivityUI()
        {
            AppContextLiveData.Instance.SetLocale(this);

            _gestureDetector = new GestureDetector(this);

            _textViewNotification = FindViewById<TextView>(Resource.Id.textViewNotification);
            
            _textViewStatusLine = FindViewById<TextView>(Resource.Id.textViewStatusLine);
            _textViewStatusLine.Selected = true;

            _distanceSeekBar = FindViewById<SeekBar>(Resource.Id.seekBarDistance);
            _distanceSeekBar.Progress = Context.Settings.MaxDistance;
            _distanceSeekBar.ProgressChanged += OnMaxDistanceChanged;

            _poiInfo = FindViewById<LinearLayout>(Resource.Id.mainActivityPoiInfo);
            _poiInfo.SetOnClickListener(this);
            _poiInfo.Visibility = ViewStates.Gone;

            _seekBars = FindViewById<LinearLayout>(Resource.Id.mainActivitySeekBars);
            _seekBars.Visibility = ViewStates.Visible;

            _displayTerrainButton = FindViewById<ImageButton>(Resource.Id.buttonDisplayTerrain);
            _displayTerrainButton.SetOnClickListener(this);
            _displayTerrainButton.SetImageResource(Context.Settings.ShowElevationProfile ? Resource.Drawable.ic_terrain : Resource.Drawable.ic_terrain_off);

            var _selectCategoryButton = FindViewById<ImageButton>(Resource.Id.buttonCategorySelect);
            _selectCategoryButton.SetOnClickListener(this);

            FindViewById<Button>(Resource.Id.buttonWiki).SetOnClickListener(this);
            FindViewById<Button>(Resource.Id.buttonMap).SetOnClickListener(this);

            _compassView = FindViewById<CompassView>(Resource.Id.compassView1);
            _compassView.LayoutChange += OnLayoutChanged;
        }

        private string _maxDistanceMinAltitudeTemplate;

        protected void Start()
        {
            //Finnaly setup OnDataChanged listener and Load all data
            //Context.DataChanged += DataChanged;
            //Context.HeadingChanged -= HeadingChanged;

            ElevationProfileProvider.Instance().ElevationProfileChanged += OnElevationProfileChanged;
            UpdateStatusBar();
        }

        protected override void OnPause()
        {
            base.OnPause();
            Context.DataChanged -= DataChanged;
            Context.HeadingChanged -= HeadingChanged;
        }

        protected override void OnResume()
        {
            base.OnResume();
            _maxDistanceMinAltitudeTemplate = Resources.GetText(Resource.String.Main_MaxDistanceMinAltitudeTemplate);
            
            Context.DataChanged += DataChanged;
            Context.HeadingChanged += HeadingChanged;
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

        public virtual void OnDataChanged(object sender, DataChangedEventArgs e)
        {
            _textViewNotification.Visibility = ViewStates.Invisible;
            UpdateStatusBar();
        }

        public virtual void OnHeadingChanged(object sender, HeadingChangedEventArgs e)
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
                            var distanceX = m_PreviousMoveX - (int)e.GetX();
                            var distanceY = m_PreviousMoveY - (int)e.GetY();
                            m_PreviousMoveX = (int)e.GetX();
                            m_PreviousMoveY = (int)e.GetY();

                            if (ImageCroppingEnabled && m_croppingHandle != null)
                            {
                                OnCropAdjustment(m_croppingHandle.Value, -distanceX, -distanceY);
                            }
                            else if (MoveingAndZoomingEnabled && !m_IsScaling)
                            {
                                OnMove(-distanceX, -distanceY);
                            }
                            else if (HeadingCorrectionEnabled && Math.Abs(m_FirstMoveX - e.GetX()) > Math.Abs(m_FirstMoveY - e.GetY()))
                            {
                                _compassView.OnHeadingCorrection(distanceX);
                            }
                            else if (OnePointTiltCorrectionEnabled && Math.Abs(m_FirstMoveY - e.GetY()) > Math.Abs(m_FirstMoveX - e.GetX()))
                            {
                                _compassView.OnTiltCorrection(distanceY, TiltCorrectionType.Both);
                                OnTiltChanged();
                            }
                            else if (TwoPointTiltCorrectionEnabled)
                            {
                                _compassView.OnTiltCorrection(distanceY, (e.RawX < Resources.DisplayMetrics.WidthPixels / 2) ? TiltCorrectionType.Left : TiltCorrectionType.Right);
                                OnTiltChanged();
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
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode == EditActivity.RESULT_OK || resultCode == EditActivity.RESULT_OK_AND_CLOSE_PARENT)
            {
                //TODO: use Poi model
                Context.ReloadData();
                
                var id = data.GetLongExtra("Id", 0);
                if (Context.SelectedPoi?.Poi.Id == id)
                {
                    OnPointSelected(Context.SelectedPoi);
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

            ElevationProfileProvider.Instance().CheckAndReloadElevationProfile(this, MaxDistance, Context);
        }

        protected bool IsControlsVisible()
        {
            return (_seekBars.Visibility == ViewStates.Visible && _activityControlBar.Visibility == ViewStates.Visible);
        }

        protected void HideControls()
        {
            _seekBars.Visibility = ViewStates.Gone;
            _activityControlBar.Visibility = ViewStates.Gone;
        }

        protected void ShowControls()
        {
            _seekBars.Visibility = ViewStates.Visible;
            _activityControlBar.Visibility = ViewStates.Visible;
        }

        protected void SetStatusLineText(string text, bool alert = false)
        {
            _textViewStatusLine.Text = text;
            _textViewStatusLine.SetTextColor(alert ? Android.Graphics.Color.Yellow : Android.Graphics.Color.GreenYellow);
        }

        public void OnElevationProfileChanged(object sender, ElevationProfileChangedEventArgs e)
        {
            Context.ElevationProfileData = e.ElevationProfileData;
            _compassView.SetElevationProfile(e.ElevationProfileData);
        }

        private void OnPointSelected(PoiViewItem item)
        {
            var altitudeText = $"{Resources.GetText(Resource.String.Common_Altitude)}: {item.Poi.Altitude} m";
            var distanceText = $"{Resources.GetText(Resource.String.Common_Distance)}: {(item.GpsLocation.Distance / 1000):F2} km";
            var verticalAngleText = $"{Resources.GetText(Resource.String.Common_VerticalViewAngle)}: {(item.VerticalViewAngle > 0 ? "+" : "")}{item.VerticalViewAngle:F3}°";
            var bearingText = $"{Resources.GetText(Resource.String.Common_Bearing)}: {(item.GpsLocation.Bearing > 0 ? "+" : "")}{item.GpsLocation.Bearing:F0}°";

            FindViewById<TextView>(Resource.Id.textViewPoiName).Text = item.Poi.Name;
            FindViewById<ImageView>(Resource.Id.imageViewInfoAvailable).Visibility = item.IsImportant() ? ViewStates.Visible : ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.textViewPoiPartiallyVisible).Visibility = item.IsFullyVisible() ? ViewStates.Gone : ViewStates.Visible;
            FindViewById<TextView>(Resource.Id.textViewPoiDescription).Text = bearingText + " / " + verticalAngleText;
            FindViewById<TextView>(Resource.Id.textViewPoiGpsLocation).Text = altitudeText + " / " + distanceText;
            FindViewById<TextView>(Resource.Id.textViewPoiData).Text = $"{Resources.GetText(Resource.String.Common_GPSLocation)}: {item.GpsLocation.LocationAsString()}";
            FindViewById<Button>(Resource.Id.buttonWiki).Visibility = WikiUtilities.HasWiki(item.Poi) ? ViewStates.Visible : ViewStates.Gone;
            FindViewById<Button>(Resource.Id.buttonWiki).Text = Resources.GetText(Resource.String.Common_Details);
            FindViewById<Button>(Resource.Id.buttonMap).Text = Resources.GetText(Resource.String.Common_Map);
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
                var newSelectedPoi = _compassView.GetPoiByScreenLocation(e.GetX(0), e.GetY(0));

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

                    HideControls();
                    _poiInfo.Visibility = ViewStates.Visible;

                    OnPointSelected(Context.SelectedPoi);
                }
                else
                {
                    Context.SelectedPoi = null;

                    ShowControls();
                    _poiInfo.Visibility = ViewStates.Gone;
                }

                _compassView.Invalidate();
            }

            return false;
        }
        #endregion Required abstract methods
    }
}