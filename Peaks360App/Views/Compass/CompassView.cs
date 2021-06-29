using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Peaks360Lib.Utilities;
using Peaks360Lib.Domain.ViewModel;
using Peaks360App.Utilities;
using Peaks360App.Views.Compass;
using Peaks360App.AppContext;
using Peaks360App.Providers;
using System.Collections.Generic;
using Java.Lang;
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Views
{
    public enum TiltCorrectionType
    {
        Left,
        Right,
        Both
    }

    public class CompassView : View
    {
        private readonly object syncLock = new object(); 
        private static string TAG = "Horizon-CompassView";
        private static float MIN_DISTANCE_METERS = 100;

        private List<PoiViewItem> _poisInVisibilityRange;
        private IOrderedEnumerable<PoiViewItem> _poisToBeDisplayed;
        private CompassViewFilter _compassViewFilter = new CompassViewFilter();
        private IAppContext _context { get; set; }

        private float _scale = 1;
        private double _offsetY = 0;
        private double _offsetX = 0;
        private float scaledViewAngleHorizontal = 0;
        private float scaledViewAngleVertical = 0;
        private bool _showElevationProfile = true;
        private bool _showPointsOfInterest = true;

        private ElevationProfileData _elevationProfile;
        private bool _allowRotation = true;
        private System.Drawing.Size _pictureSize;
        
        private Paint _textPaint;
        private Paint _backgroundPaint;
        private string _textNoGps;
        private string _textNoPOIs;

        public bool ShowElevationProfile
        {
            get { return _showElevationProfile; }
            set { _showElevationProfile = value; Invalidate(); }
        }
        public bool ShowPointsOfInterest
        {
            get { return _showPointsOfInterest; }
            set { _showPointsOfInterest = value; Invalidate(); }
        }

        public float ViewAngleHorizontal { get; private set; } = 0;
        public float ViewAngleVertical { get; private set; } = 0;

        private IPoiCategoryBitmapProvider poiCategoryBitmapProvider;
        private CompassViewDrawer compassViewDrawer;
        private ElevationProfileBitmapDrawer elevationProfileBitmapDrawer;

        public CompassView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            poiCategoryBitmapProvider = new PoiCategoryBitmapProvider();
            compassViewDrawer = new CompassViewDrawerEachPoiSeparate(poiCategoryBitmapProvider);

            _textPaint = new Paint();
            _textPaint.SetARGB(255, 200, 255, 0);
            _textPaint.TextSize = 42;
            _textPaint.AntiAlias = true;
            _textPaint.TextAlign = Paint.Align.Center;

            _backgroundPaint = new Paint();
            _backgroundPaint.SetARGB(150, 0, 0, 0);
            _backgroundPaint.SetStyle(Paint.Style.Fill);
            _backgroundPaint.StrokeWidth = 0;
        }

        public void Initialize(IAppContext context, bool allowRotation, System.Drawing.Size pictureSize)
        {
            _scale = 1;
            _offsetY = 0;
            _offsetX = 0;

            _context = context;
            _allowRotation = allowRotation;
            _pictureSize = pictureSize;

            //TODO: we should not add OnSettingsChanged, if it was already added
            _context.Settings.SettingsChanged += OnSettingsChanged;

            elevationProfileBitmapDrawer = new ElevationProfileBitmapDrawer(_context);

            _showElevationProfile = _context.Settings.ShowElevationProfile;
            
            Invalidate();
        }

        public void SetPoiViewItemList(PoiViewItemList srcList)
        {
            lock (syncLock)
            {
                _poisInVisibilityRange = srcList;
                
                _poisToBeDisplayed = FilterPoisDoBeDisplayed(_poisInVisibilityRange);
            }

            Invalidate();
        }

        public void RefreshPoisDoBeDisplayed()
        {
            lock (syncLock)
            {
                _poisToBeDisplayed = FilterPoisDoBeDisplayed(_poisInVisibilityRange);
            }

            Invalidate(); 
        }

        public void SetElevationProfile(ElevationProfileData elevationProfile)
        {
            _elevationProfile = elevationProfile;
            GenerateElevationProfileLines();

            _poisToBeDisplayed = FilterPoisDoBeDisplayed(_poisInVisibilityRange);

            Invalidate();
        }

        private IOrderedEnumerable<PoiViewItem> FilterPoisDoBeDisplayed(List<PoiViewItem> srcList)
        {
            if (srcList == null)
            {
                return null;
            }

            PoiViewItem[] listCopy = new PoiViewItem[srcList.Count];
            srcList.CopyTo(listCopy);
            foreach (var item in listCopy)
            {
                item.Visibility = CompassViewUtils.IsPoiVisible(item, _context.ElevationProfileData);
            }

            _poisToBeDisplayed = listCopy.Where(poi => 
                poi.Visibility != Peaks360Lib.Domain.Enums.Visibility.Invisible
                && (!_context.ShowFavoritesOnly || poi.Poi.Favorite))
                .OrderByDescending(poi => poi.Priority)
                .ThenByDescending(poi => poi.GpsLocation.VerticalViewAngle);

            CheckOverlappingItems(_poisToBeDisplayed);
            return _poisToBeDisplayed;
        }

        private void CheckOverlappingItems(IEnumerable<PoiViewItem> srcList)
        {
            lock (syncLock)
            {
                if (srcList == null)
                    return;

                var minAngleDiff = compassViewDrawer.GetMinItemAngleDiff((int) (this.Width * _scale));
                _compassViewFilter.Reset();
                foreach (var item in srcList)
                {
                    item.Overlapped = _compassViewFilter.IsOverlapping(item, minAngleDiff);
                }
            }
        }

        public void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            if (e.ChangedData == ChangedData.ViewOptions)
            {
                _textNoGps = null;
                _textNoPOIs = null;
                InitializeViewDrawer(new System.Drawing.Size(this.Width, this.Height), _pictureSize);
            }
        }

        private string GetTextNoGPS()
        {
            if (_textNoGps == null)
            {
                _textNoGps = Resources.GetText(Resource.String.Main_WaitingForGps);
            }
            return _textNoGps;
        }

        private string GetTextNoPOIs()
        {
            if (_textNoPOIs == null)
            {
                _textNoPOIs = Resources.GetText(Resource.String.Common_NoDataToDisplay);
            }
            return _textNoPOIs;
            
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            if (_pictureSize != null)
            {
                var compassViewSize = new System.Drawing.Size(this.Width, this.Height);
                InitializeViewDrawer(compassViewSize, _pictureSize);
            }
        }


        public void InitializeViewDrawer(System.Drawing.Size compassViewSize, System.Drawing.Size pictureSize)
        {
            if (_context == null || poiCategoryBitmapProvider == null)
            {
                return;
            }

            try
            {
                (ViewAngleHorizontal, ViewAngleVertical) = CompassViewUtils.AdjustViewAngles(
                    _context.ViewAngleHorizontal, _context.ViewAngleVertical,
                    compassViewSize, pictureSize, _allowRotation);

                //2000 x 1000 px is a default view size. All drawings ale calculated to this size
                var isPortrait = compassViewSize.Height > compassViewSize.Width;
                var defaultCompassSize = isPortrait ? 1000f : 2000f;

                //so we need to calculate mutliplier to adjust them for current resolution
                float multiplier = compassViewSize.Width / defaultCompassSize;

                compassViewDrawer.Initialize(Resources, ViewAngleHorizontal, ViewAngleVertical, multiplier);
                elevationProfileBitmapDrawer.Initialize(ViewAngleHorizontal, ViewAngleVertical, multiplier);

                Log.WriteLine(LogPriority.Debug, TAG, $"ViewAngle: {_context.ViewAngleHorizontal:F1}/{_context.ViewAngleVertical:F1}");
                Log.WriteLine(LogPriority.Debug, TAG, $"AdjustedViewAngle: {ViewAngleHorizontal:F1}/{ViewAngleVertical:F1}");

                RecalculateViewAngles(_scale);
            }
            catch (Exception)
            {
                //TODO: log error Context.Log
            }
        }

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            if (_context == null || compassViewDrawer == null)
            {
                return;
            }

            var heading = Peaks360Lib.Utilities.GpsUtils.Normalize180((_context.HeadingX ?? 0) + _context.HeadingCorrector);

            compassViewDrawer.OnDrawBackground(canvas);

            //PaintCompass(canvas, heading);

            if (ShowElevationProfile)
                PaintElevationProfileLines(canvas, heading);

            if (ShowPointsOfInterest)
            {
                if (_poisToBeDisplayed == null)
                {
                    ShowInfoText(canvas, GetTextNoGPS());
                }
                else if (!_poisToBeDisplayed.Any())
                {
                    ShowInfoText(canvas, GetTextNoPOIs());
                }
                else
                {
                    PaintVisiblePois(canvas, heading);
                }
            }
        }

        private void ShowInfoText(Canvas canvas, string text)
        {
            var lines = text.Split("\n");

            float textHeight = _textPaint.Descent() - _textPaint.Ascent();
            float x = canvas.Width / 2f;
            float y = textHeight;

            canvas.DrawRect(0, 0, canvas.Width, (lines.Length + 3) * textHeight, _backgroundPaint);
            foreach (var line in lines)
            {
                y += textHeight;
                canvas.DrawText(line, x, y, _textPaint);
            }
        }

        private void PaintVisiblePois(Canvas canvas, double heading)
        {
            lock (syncLock)
            {
                canvas.Rotate(90, 0, 0);

                foreach (var item in _poisToBeDisplayed)
                {
                    if (item.GpsLocation.Distance < MIN_DISTANCE_METERS)
                        continue;
                    if (item.Visibility == Peaks360Lib.Domain.Enums.Visibility.Invisible)
                        continue;
                    if (!_context.DisplayOverlapped && item.Overlapped)
                        continue;
                    
                    compassViewDrawer.DrawItem(canvas, item, (float) heading, (float) _offsetX, (float) _offsetY, _context.LeftTiltCorrector, _context.RightTiltCorrector, _context.DisplayOverlapped, canvas.Width);
                }

                canvas.Rotate(-90, 0, 0);

                foreach (var item in _poisToBeDisplayed)
                {
                    if (item.GpsLocation.Distance < MIN_DISTANCE_METERS)
                        continue;
                    if (item.Visibility == Peaks360Lib.Domain.Enums.Visibility.Invisible)
                        continue;
                    if (item.Overlapped)
                        continue;
                    
                    compassViewDrawer.DrawItemIcon(canvas, item, (float)heading, (float)_offsetX, (float)_offsetY, _context.LeftTiltCorrector, _context.RightTiltCorrector, canvas.Width);
                }
            }

        }

        public void OnHeadingCorrection(float distanceX)
        {
            var viewAngleHorizontal = _context.ViewAngleHorizontal;
            _context.HeadingCorrector = Peaks360Lib.Utilities.GpsUtils.Normalize180(_context.HeadingCorrector + CompassViewUtils.GetHeadingDifference(viewAngleHorizontal, Width, distanceX / _scale));
            Invalidate();
        }

        public void OnTiltCorrection(float distanceY, TiltCorrectionType tiltCorrectionType)
        {
            var dY = (distanceY / Height) * scaledViewAngleVertical;
            if (tiltCorrectionType == TiltCorrectionType.Left)
            {
                _context.LeftTiltCorrector += dY;
            }
            else if (tiltCorrectionType == TiltCorrectionType.Right)
            {
                _context.RightTiltCorrector += dY;
            }
            else if (tiltCorrectionType == TiltCorrectionType.Both)
            {
                _context.LeftTiltCorrector += dY;
                _context.RightTiltCorrector += dY;
            }

            Invalidate();
        }

        private void GenerateElevationProfileLines()
        {
            elevationProfileBitmapDrawer.GenerateElevationProfileLines(_elevationProfile, Width, Height);
        }

        private void PaintElevationProfileLines(Canvas canvas, double heading)
        {
            elevationProfileBitmapDrawer.PaintElevationProfileLines(canvas, heading, _context.LeftTiltCorrector, _context.RightTiltCorrector, (float)_offsetX, (float)_offsetY);
        }

        public void Move(double offsetX, double offsetY)
        {
            _offsetX = offsetX;
            _offsetY = offsetY;
            Invalidate();
        }

        public void ScaleHorizontalViewAngle(float scale)
        {
            _context.Settings.SetCameraParameters(_context.ViewAngleHorizontal / scale, _context.ViewAngleVertical,
                _context.Settings.CameraPictureSize.Width, _context.Settings.CameraPictureSize.Height);

            (ViewAngleHorizontal, ViewAngleVertical) = CompassViewUtils.AdjustViewAngles(
                _context.ViewAngleHorizontal, _context.ViewAngleVertical,
                new System.Drawing.Size(Width, Height),
                _context.Settings.CameraPictureSize, _allowRotation);

            RecalculateViewAngles(_scale);
        }

        public void ScaleVerticalViewAngle(float scale)
        {
            _context.Settings.SetCameraParameters(_context.ViewAngleHorizontal, _context.ViewAngleVertical / scale,
                _context.Settings.CameraPictureSize.Width, _context.Settings.CameraPictureSize.Height);

            (ViewAngleHorizontal, ViewAngleVertical) = CompassViewUtils.AdjustViewAngles(
                _context.ViewAngleHorizontal, _context.ViewAngleVertical,
                new System.Drawing.Size(Width, Height),
                _context.Settings.CameraPictureSize, _allowRotation);

            RecalculateViewAngles(_scale);
        }

        public void RecalculateViewAngles(float scale)
        {
            _scale = scale;

            scaledViewAngleHorizontal = ViewAngleHorizontal / scale;
            scaledViewAngleVertical = ViewAngleVertical / scale;

            compassViewDrawer.SetScaledViewAngle(scaledViewAngleHorizontal, scaledViewAngleVertical);
            elevationProfileBitmapDrawer.SetScaledViewAngle(scaledViewAngleHorizontal, scaledViewAngleVertical);

            CheckOverlappingItems(_poisToBeDisplayed);
            Invalidate();
        }

        public PoiViewItem GetPoiByScreenLocation(float x, float y, bool displayOverlapped)
        {
            if (ShowPointsOfInterest && _poisToBeDisplayed != null)
            {
                (x, y) = ToLocationOnScreen(x, y);
                
                var heading = (_context.HeadingX ?? 0) + _context.HeadingCorrector;

                if (displayOverlapped)
                {
                    PoiViewItem selectedItem = null;
                    float selectedItemDistance = float.MaxValue;
                    foreach (var item in _poisToBeDisplayed)
                    {
                        if (item.Visibility != Peaks360Lib.Domain.Enums.Visibility.Invisible)
                        {
                            var dist = compassViewDrawer.GetClickDistance(item, (float)heading, (float)_offsetX, (float)_offsetY, _context.LeftTiltCorrector, _context.RightTiltCorrector, Width, Height, x, y);
                            if (dist < selectedItemDistance)
                            {
                                selectedItemDistance = dist;
                                selectedItem = item;
                            }
                        }
                    }

                    return selectedItemDistance < 30 ? selectedItem : null;
                }
                else
                {
                    foreach (var item in _poisToBeDisplayed)
                    {
                        if (item.Visibility != Peaks360Lib.Domain.Enums.Visibility.Invisible && !item.Overlapped)
                        {
                            if (compassViewDrawer.IsItemClicked(item, (float)heading, (float)_offsetX, (float)_offsetY, _context.LeftTiltCorrector, _context.RightTiltCorrector, Width, Height, x, y))
                            {
                                return item;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private (float x, float y) ToLocationOnScreen(float x, float y)
        {
            var windowLocationOnScreen = new int[2];
            GetLocationInWindow(windowLocationOnScreen);
            x = x - windowLocationOnScreen[0];
            y = y - windowLocationOnScreen[1];
            return (x, y);
        }

        internal void Update(Poi poi)
        {
            var item = _poisToBeDisplayed.Single(x => x.Poi.Id == poi.Id);
            item.Poi = poi;
        }
    }
} 
