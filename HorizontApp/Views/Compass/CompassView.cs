using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Xamarin.Essentials;
using HorizontLib.Utilities;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Utilities;
using HorizontApp.Views.Compass;
using HorizontLib.Domain.Models;
using System;
using GpsUtils = HorizontApp.Utilities.GpsUtils;
using System.Drawing;

namespace HorizontApp.Views
{
    public class CompassView : View
    {
        private static IOrderedEnumerable<PoiViewItem> list;
        private CompassViewFilter _compassViewFilter = new CompassViewFilter();
        public double Heading { get; set; }
        private Paint _paint;
        private static double _headingCorrector = 0;
        private ElevationProfileData _elevationProfile;
        private Android.Graphics.Bitmap _elevationProfileBitmap;
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

            elevationProfileBitmapDrawer = new ElevationProfileBitmapDrawer();

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

            PaintElevationProfileBitmap(canvas);

            PaintVisiblePois(canvas);
        }

        private void PaintVisiblePois(Canvas canvas)
        {
            canvas.Rotate(90, 0, 0);

            if (list != null)
            {
                foreach (var item in list)
                {
                    if (item.Visibility)
                        compassViewDrawer.OnDrawItem(canvas, item, (float)Heading);
                }
            }
            
            canvas.Rotate(-90, 0, 0);
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
            GenerateElevationProfileBitmap();

            Invalidate();
        }

        private void GenerateElevationProfileBitmap()
        {
            elevationProfileBitmapDrawer.SetElevationProfile(_elevationProfile, Width, Height);
            var profileImageData = elevationProfileBitmapDrawer.GetElevationBitmap().ToArray();
            _elevationProfileBitmap = BitmapFactory.DecodeByteArray(profileImageData, 0, profileImageData.Length);
        }

        private void PaintElevationProfileBitmap(Canvas canvas)
        {
            if (_elevationProfileBitmap != null)
            {
                float offset = (float)(_elevationProfileBitmap.Width * (Heading - CompassViewSettings.Instance().ViewAngleHorizontal / 2) / 360);
                canvas.DrawBitmap(_elevationProfileBitmap, -offset, (float)0, _paint);
                if (Heading > 360 - CompassViewSettings.Instance().ViewAngleHorizontal)
                {
                    canvas.DrawBitmap(_elevationProfileBitmap, -offset + _elevationProfileBitmap.Width, (float)0, _paint);
                }
                if (Heading < CompassViewSettings.Instance().ViewAngleHorizontal)
                {
                    canvas.DrawBitmap(_elevationProfileBitmap, -offset - _elevationProfileBitmap.Width, (float)0, _paint);
                }
            }
        }
    }
}