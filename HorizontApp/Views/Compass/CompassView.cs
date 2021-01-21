using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Xamarin.Essentials;
using HorizontLib.Utilities;
using HorizontLib.Domain.ViewModel;
using HorizontApp.Utilities;
using HorizontApp.Views.Compass;
using HorizontApp.AppContext;
using GpsUtils = HorizontApp.Utilities.GpsUtils;
using System.Runtime.InteropServices.ComTypes;
using System;
using System.Threading;
using HorizontApp.Providers;

namespace HorizontApp.Views
{
    public class CompassView : View
    {
        private readonly object syncLock = new object(); 
        private static string TAG = "Horizon-CompassView";

        private static IOrderedEnumerable<PoiViewItem> list;
        private CompassViewFilter _compassViewFilter = new CompassViewFilter();
        private IAppContext _context { get; set; }

        private Paint _paint;
        private float _scale = 1;
        private double _offsetY = 0;
        private double _offsetX = 0;
        private float scaledViewAngleHorizontal = 0;
        private float scaledViewAngleVertical = 0;
        private bool _showElevationProfile;
        private bool _showPointsOfInterest;

        private ElevationProfileData _elevationProfile;
        private double _leftTiltCorrector = 0;
        private double _rightTiltCorrector = 0;
        private bool _allowRotation = true;
        private System.Drawing.Size _pictureSize;

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

        public double LeftTiltCorrector { get { return _leftTiltCorrector; } }
        public double RightTiltCorrector { get { return _rightTiltCorrector; } }

        private IPoiCategoryBitmapProvider poiCategoryBitmapProvider;
        private CompassViewDrawer compassViewDrawer;
        private ElevationProfileBitmapDrawer elevationProfileBitmapDrawer;

        public CompassView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            poiCategoryBitmapProvider = new PoiCategoryBitmapProvider();
            compassViewDrawer = new CompassViewDrawer(poiCategoryBitmapProvider);
        }

/*        public CompassView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
        }
*/
        public void Initialize(IAppContext context, bool allowRotation, System.Drawing.Size pictureSize, float leftTiltCorrector = 0, float rightTiltCorrector = 0)
        {
            _context = context;
            _allowRotation = allowRotation;
            _pictureSize = pictureSize;
            _leftTiltCorrector = leftTiltCorrector;
            _rightTiltCorrector = rightTiltCorrector;

            _context.Settings.SettingsChanged += OnSettingsChanged;

            elevationProfileBitmapDrawer = new ElevationProfileBitmapDrawer(_context);

            _paint = new Paint();
            _paint.SetARGB(255, 255, 255, 0);
            _paint.SetStyle(Paint.Style.Stroke);
            _paint.StrokeWidth = 3;
        }

        public void SetPoiViewItemList(PoiViewItemList srcList)
        {
            lock (syncLock)
            {
                PoiViewItem[] listCopy = new PoiViewItem[srcList.Count];
                srcList.CopyTo(listCopy);
                foreach (var item in listCopy)
                {
                    item.Visibility = CompassViewUtils.IsPoiVisible(item, _context.ElevationProfileData);
                }

                list = listCopy.Where(poi => poi.Visibility != HorizonLib.Domain.Enums.Visibility.Invisible).OrderByDescending(poi => poi.Priority).ThenByDescending(poi => poi.GpsLocation.VerticalViewAngle);
                CheckOverlappingItems();
            }

            Invalidate();
        }

        private void CheckOverlappingItems()
        {
            lock (syncLock)
            {
                if (list == null)
                    return;

                var minAngleDiff = compassViewDrawer.GetMinItemAngleDiff((int) (this.Width * _scale));
                _compassViewFilter.Reset();
                foreach (var item in list)
                {
                    item.Overlapped = _compassViewFilter.IsOverlapping(item, minAngleDiff);
                }
            }
        }

        public void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            if (e.ChangedData == ChangedData.ViewOptions)
            {
                InitializeViewDrawer(new System.Drawing.Size(this.Width, this.Height), _pictureSize);
            }
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

            switch (_context.Settings.AppStyle)
            {
                case AppStyles.EachPoiSeparate:
                    compassViewDrawer = new CompassViewDrawerEachPoiSeparate(poiCategoryBitmapProvider);
                    break;
                case AppStyles.FullScreenRectangle:
                    compassViewDrawer = new CompassViewDrawerFullScreenRectangle(poiCategoryBitmapProvider);
                    break;
                case AppStyles.Simple:
                    compassViewDrawer = new CompassViewDrawerSimple(poiCategoryBitmapProvider);
                    break;
                case AppStyles.SimpleWithDistance:
                    compassViewDrawer = new CompassViewDrawerSimpleWithDistance(poiCategoryBitmapProvider);
                    break;
                case AppStyles.SimpleWithHeight:
                    compassViewDrawer = new CompassViewDrawerSimpleWithHeight(poiCategoryBitmapProvider);
                    break;
            }

            (ViewAngleHorizontal, ViewAngleVertical) = CompassViewUtils.AdjustViewAngles(
                _context.ViewAngleHorizontal, _context.ViewAngleVertical,
                compassViewSize, pictureSize, _allowRotation);

            float multiplier = (float)Math.Sqrt(compassViewSize.Width * compassViewSize.Height / 2000000.0);
            compassViewDrawer.Initialize(Resources, ViewAngleHorizontal, ViewAngleVertical, multiplier);
            elevationProfileBitmapDrawer.Initialize(ViewAngleHorizontal, ViewAngleVertical);

            Log.WriteLine(LogPriority.Debug, TAG, $"ViewAngle: {_context.ViewAngleHorizontal:F1}/{_context.ViewAngleVertical:F1}");
            Log.WriteLine(LogPriority.Debug, TAG, $"AdjustedViewAngle: {ViewAngleHorizontal:F1}/{ViewAngleVertical:F1}");

            RecalculateViewAngles(_scale);
        }

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            if (_context == null || compassViewDrawer == null)
            {
                return;
            }

            var heading = _context.Heading + _context.HeadingCorrector;

            compassViewDrawer.OnDrawBackground(canvas);

            if (ShowElevationProfile)
                PaintElevationProfileLines(canvas, heading);

            if (ShowPointsOfInterest)
                PaintVisiblePois(canvas, heading);
        }

        private void PaintVisiblePois(Canvas canvas, double heading)
        {
            lock (syncLock)
            {
                if (list != null)
                {
                    canvas.Rotate(90, 0, 0);

                    foreach (var item in list)
                    {
                        if (item.Visibility != HorizonLib.Domain.Enums.Visibility.Invisible && !item.Overlapped)
                        {
                            compassViewDrawer.DrawItem(canvas, item, (float) heading, (float) _offsetX, (float) _offsetY, _leftTiltCorrector, _rightTiltCorrector, canvas.Width);
                        }
                    }

                    canvas.Rotate(-90, 0, 0);

                    foreach (var item in list)
                    {
                        if (item.Visibility != HorizonLib.Domain.Enums.Visibility.Invisible && !item.Overlapped)
                        {
                            compassViewDrawer.DrawItemIcon(canvas, item, (float)heading, (float)_offsetX, (float)_offsetY, _leftTiltCorrector, _rightTiltCorrector, canvas.Width);
                        }
                    }

                }
            }

        }

        public void OnScroll(float distanceX)
        {
            var viewAngleHorizontal = _context.ViewAngleHorizontal;
            _context.HeadingCorrector = _context.HeadingCorrector + CompassViewUtils.GetHeadingDifference(viewAngleHorizontal, Width, distanceX / _scale);
            Invalidate();
        }

        public void OnScroll(float distanceY, bool isLeft)
        {
            var dY = (distanceY / Height) * scaledViewAngleVertical;
            if (isLeft)
            {
                _leftTiltCorrector += dY;
            }
            else 
            {
                _rightTiltCorrector += dY;
            }
            Invalidate();
        }

        public void ResetHeadingCorrector()
        {
            _context.HeadingCorrector = 0;
            Invalidate();
        }

        public void SetElevationProfile(ElevationProfileData elevationProfile)
        {
            _elevationProfile = elevationProfile;
            GenerateElevationProfileLines();

            Invalidate();
        }

        private void GenerateElevationProfileLines()
        {
            elevationProfileBitmapDrawer.GenerateElevationProfileLines(_elevationProfile, Width, Height);
        }

        private void PaintElevationProfileLines(Canvas canvas, double heading)
        {
            elevationProfileBitmapDrawer.PaintElevationProfileLines(canvas, heading, _leftTiltCorrector, _rightTiltCorrector, (float)_offsetX, (float)_offsetY);
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

            CheckOverlappingItems();
            Invalidate();
        }

        public PoiViewItem GetPoiByScreenLocation(float x, float y)
        {
            if (ShowPointsOfInterest)
            {
                (x, y) = ToLocationOnScreen(x, y);
                
                var heading = _context.Heading + _context.HeadingCorrector;

                foreach (var item in list)
                {
                    if (item.Visibility != HorizonLib.Domain.Enums.Visibility.Invisible && !item.Overlapped)
                    {
                        if (compassViewDrawer.IsItemClicked(item, (float) heading, (float) _offsetX, (float) _offsetY, _leftTiltCorrector, _rightTiltCorrector, Width, Height, x, y))
                        {
                            return item;
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
    }
} 
