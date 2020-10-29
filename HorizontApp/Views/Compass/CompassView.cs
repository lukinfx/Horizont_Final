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

namespace HorizontApp.Views
{
    public class CompassView : View
    {
        private static string TAG = "Horizon-CompassView";

        private static IOrderedEnumerable<PoiViewItem> list;
        private CompassViewFilter _compassViewFilter = new CompassViewFilter();
        private IAppContext _context { get; set; }
        public double Heading { get; set; }
        private Paint _paint;
        private static double _headingCorrector = 0;
        private ElevationProfileData _elevationProfile;
        private double _leftTiltCorrector = 0;
        private double _rightTiltCorrector = 0;

        public double HeadingCorrector
        {
            get
            {
                return _headingCorrector;
            }
            set
            {
                _headingCorrector = GpsUtils.Normalize180(value);
            }
        }

        private CompassViewDrawer compassViewDrawer;
        private ElevationProfileBitmapDrawer elevationProfileBitmapDrawer;


        public CompassView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
        }

        public void SetPoiViewItemList(PoiViewItemList list2)
        {
            list = list2.OrderByDescending(poi => poi.Poi.Altitude).ThenBy(poi => poi.Distance);
            
            var minAngleDiff = compassViewDrawer.GetMinItemAngleDiff(this.Width);
            _compassViewFilter.Reset();
            foreach (var item in list)
            {
                item.Visibility = _compassViewFilter.Filter(item, minAngleDiff);
            }
            Invalidate();
        }

        public CompassView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle) 
        {
        }

        public void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            InitializeViewDrawer();
            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
            {
                HeadingCorrector = -90;
            }
            else
            {
                HeadingCorrector = 0;
            }
            
        }

        public void Initialize(IAppContext context)
        {
            _context = context;

            _context.Settings.SettingsChanged += OnSettingsChanged;

            elevationProfileBitmapDrawer = new ElevationProfileBitmapDrawer(_context);

            InitializeViewDrawer();

            _paint = new Paint();
            _paint.SetARGB(255, 255, 255, 0);
            _paint.SetStyle(Paint.Style.Stroke);
            _paint.StrokeWidth = 3;
        }

        private void InitializeViewDrawer()
        {
            switch (_context.Settings.AppStyle)
                {
                    case AppStyles.EachPoiSeparate:
                        compassViewDrawer = new CompassViewDrawerEachPoiSeparate();
                        break;
                    case AppStyles.FullScreenRectangle:
                        compassViewDrawer = new CompassViewDrawerFullScreenRectangle();
                        break;
                    case AppStyles.Simple:
                        compassViewDrawer = new CompassViewDrawerSimple();
                        break;
                    case AppStyles.SimpleWithDistance:
                        compassViewDrawer = new CompassViewDrawerSimpleWithDistance();
                        break;
                    case AppStyles.SimpleWithHeight:
                        compassViewDrawer = new CompassViewDrawerSimpleWithHeight();
                        break;
            }

            var (adjustedViewAngleHorizontal, adjustedViewAngleVertical) = CompassViewUtils.AdjustViewAngles(
                _context.Settings.ViewAngleHorizontal, _context.Settings.ViewAngleVertical,
                new System.Drawing.Size(this.Width, this.Height), 
                _context.Settings.CameraPictureSize);

            Log.WriteLine(LogPriority.Debug, TAG, $"ViewAngle: {_context.Settings.ViewAngleHorizontal:F1}/{_context.Settings.ViewAngleVertical:F1}");
            Log.WriteLine(LogPriority.Debug, TAG, $"AdjustedViewAngle: {adjustedViewAngleHorizontal:F1}/{adjustedViewAngleVertical:F1}");

            compassViewDrawer.Initialize(adjustedViewAngleHorizontal, adjustedViewAngleVertical);
            elevationProfileBitmapDrawer.Initialize(adjustedViewAngleHorizontal, adjustedViewAngleVertical);
        }

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            compassViewDrawer.OnDrawBackground(canvas);

            PaintElevationProfileBitmap(canvas);

            PaintVisiblePois(canvas);

            PaintHorizonLine(canvas);
        }
         
        private void PaintHorizonLine(Canvas canvas)
        {
            float startX = 0;
            float startY = (float) ((canvas.Height / 2) + _leftTiltCorrector);

            float endX = canvas.Width;
            float endY = (float) ((canvas.Height / 2) + _rightTiltCorrector);
            canvas.DrawLine(startX, startY, endX, endY, _paint);
        }

        private void PaintVisiblePois(Canvas canvas)
        {
            canvas.Rotate(90, 0, 0);

            if (list != null)
            {
                foreach (var item in list)
                {
                    if (item.Visibility && (_leftTiltCorrector != 0 || _rightTiltCorrector != 0))
                        compassViewDrawer.OnDrawItem(canvas, item, (float)Heading, _leftTiltCorrector, _rightTiltCorrector, canvas.Width);
                    else if (item.Visibility)
                        compassViewDrawer.OnDrawItem(canvas, item, (float)Heading);
                }
            }
            
            canvas.Rotate(-90, 0, 0);
        }

        public void OnScroll(float distanceX)
        {
            var viewAngleHorizontal = _context.Settings.ViewAngleHorizontal;
            HeadingCorrector = HeadingCorrector + CompassViewUtils.GetHeadingDifference(viewAngleHorizontal, Width, distanceX);
            Invalidate();
        }

        public void OnScroll(float distanceY, bool isLeft)
        {
            if (isLeft)
            {
                _leftTiltCorrector -= distanceY;
            }
            else 
            {
                _rightTiltCorrector -= distanceY;
            }
            
        }

        public void ResetHeadingCorrector()
        {
            HeadingCorrector = 0;
            Invalidate();
        }

        public void SetElevationProfile(ElevationProfileData elevationProfile)
        {
            _elevationProfile = elevationProfile;
            GenerateElevationProfileBitmap();

            Invalidate();
        }

        private void GenerateElevationProfileBitmap()
        {
            elevationProfileBitmapDrawer.GenerateElevationProfileBitmap(_elevationProfile, Width, Height);
        }

        private void PaintElevationProfileBitmap(Canvas canvas)
        {
            elevationProfileBitmapDrawer.PaintElevationProfileBitmap(canvas, Heading);
        }
    }
}