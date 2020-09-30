using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Xamarin.Essentials;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Utilities;
using HorizontApp.Views.Compass;
using HorizontApp.Domain.Models;
using System;

namespace HorizontApp.Views
{
    public class CompassView : View
    {
        public static IOrderedEnumerable<PoiViewItem> list;
        private CompassViewFilter _compassViewFilter = new CompassViewFilter();
        public double Heading { get; set; }
        private Paint _paint;
        private static double _headingCorrector = 0;
        private ElevationProfileData _elevationProfile;
        public bool IsLoading { get; set; }
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
        

        public CompassView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
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
        }

        public CompassView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle) 
        {
            Initialize();
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

        private void Initialize()
        {
            CompassViewSettings.Instance().SettingsChanged += OnSettingsChanged;
            
            InitializeViewDrawer();

            _paint = new Paint();
            _paint.SetARGB(255, 255, 255, 0);
            _paint.SetStyle(Paint.Style.Stroke);
            _paint.StrokeWidth = 3;
        }

        private void InitializeViewDrawer()
        {
            switch (CompassViewSettings.Instance().AppStyle)
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

            compassViewDrawer.ViewAngleHorizontal = CompassViewSettings.Instance().ViewAngleHorizontal;
            compassViewDrawer.ViewAngleVertical = CompassViewSettings.Instance().ViewAngleVertical;
            compassViewDrawer.Initialize();
        }

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            compassViewDrawer.OnDrawBackground(canvas);

            if (IsLoading)
                return;

            PaintProfile(canvas);

            canvas.Rotate(90, 0, 0);

            if (list != null)
            {
                foreach (var item in list)
                {
                    if (item.Visibility)
                        compassViewDrawer.OnDrawItem(canvas, item, (float)Heading);
                }
            }
        }

        public void OnScroll(float distanceX)
        {
            var viewAngleHorizontal = CompassViewSettings.Instance().ViewAngleHorizontal;
            HeadingCorrector = HeadingCorrector + CompassViewUtils.GetHeadingDifference(viewAngleHorizontal, Width, distanceX);
            Invalidate();
        }

        public void ResetHeadingCorrector()
        {
            HeadingCorrector = 0;
            Invalidate();
        }

        public void SetElevationProfile(ElevationProfileData elevationProfile)
        {
            _elevationProfile = elevationProfile;
            Invalidate();
        }

        /*private void PaintElevationProfile(Android.Graphics.Canvas canvas)
        {
            if (_elevationProfile == null)
            {
                canvas.DrawLine(0, canvas.Height / (float)2.0, canvas.Width, canvas.Height / (float)2.0, _paint);
                return;
            }

            var viewAngleHorizontal = CompassViewSettings.Instance().ViewAngleHorizontal;
            var viewAngleVertical = CompassViewSettings.Instance().ViewAngleVertical;

            foreach (var point in ElevationProfileData.displayedPoints)
            {
                var x = CompassViewUtils.GetXLocationOnScreen((float)Heading, (float)point.Item1, canvas.Width, viewAngleHorizontal);
                var y = CompassViewUtils.GetYLocationOnScreen(point.Item2, canvas.Height, viewAngleVertical);
            }

            float? lastX = null;
            float? lastY = null;
            for (int i=0; i<=360; i++)
            {
                var x = CompassViewUtils.GetXLocationOnScreen((float)Heading, (float)i, canvas.Width, viewAngleHorizontal);
                var y = CompassViewUtils.GetYLocationOnScreen(_elevationProfile.Get(i%360), canvas.Height, viewAngleVertical);
                if (lastX.HasValue && lastY.HasValue && x.HasValue)
                {
                    canvas.DrawLine(lastX.Value, lastY.Value, x.Value, y, _paint);
                }

                lastX = x;
                lastY = y;
            }
        }*/

        void PaintProfile(Android.Graphics.Canvas canvas)
        {
            var viewAngleHorizontal = CompassViewSettings.Instance().ViewAngleHorizontal;
            var viewAngleVertical = CompassViewSettings.Instance().ViewAngleVertical;

            foreach (var point in ElevationProfileData.displayedPoints)
            {
                foreach (var otherPoint in ElevationProfileData.displayedPoints)
                {
                    if (Math.Abs(point.Item1 - otherPoint.Item1) < 2 && Math.Abs(point.Item2 - otherPoint.Item2) < 10)
                    {
                        var y1 = CompassViewUtils.GetYLocationOnScreen(point.Item2, canvas.Height, viewAngleHorizontal);
                        var x1 = CompassViewUtils.GetXLocationOnScreen((float)Heading, (float)point.Item1, canvas.Height, viewAngleHorizontal);

                        var y2 = CompassViewUtils.GetYLocationOnScreen(otherPoint.Item2, canvas.Height, viewAngleHorizontal);
                        var x2 = CompassViewUtils.GetXLocationOnScreen((float)Heading, (float)otherPoint.Item1, canvas.Height, viewAngleHorizontal);
                        if (x1.HasValue && x2.HasValue)
                        {
                            if (Math.Sqrt(Math.Pow(x1.Value - x2.Value, 2) + Math.Pow(y1 - y2, 2)) < 500)
                                canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, _paint);
                        }

                    }
                }
            }

            /*for (int i = Heading - viewAngleVertical/2; i < Heading + 35; i++)
            {
                var dg = (i + 360) % 360;
                double x = (i - _heading + 35) * DG_WIDTH;

                //e.Graphics.DrawLine(new Pen(Brushes.Blue), (float)x, 250, (float)x, (float)(250-y));
                if (i % 10 == 0)
                {
                    e.Graphics.DrawString(i.ToString(), new Font("Arial", 10), new SolidBrush(Color.Black), (float)x, 10);
                }
            }*/
        }
    }
}