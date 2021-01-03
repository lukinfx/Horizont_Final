﻿using System.Linq;
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

namespace HorizontApp.Views
{
    public class CompassView : View
    {
        private static string TAG = "Horizon-CompassView";

        private static IOrderedEnumerable<PoiViewItem> list;
        private CompassViewFilter _compassViewFilter = new CompassViewFilter();
        private IAppContext _context { get; set; }

        private Paint _paint;
        private double _headingCorrector = 0;
        private float _scale = 1;
        private double _offsetY = 0;
        private double _offsetX = 0;
        private float scaledViewAngleHorizontal = 0;
        private float scaledViewAngleVertical = 0;

        private ElevationProfileData _elevationProfile;
        private double _leftTiltCorrector = 0;
        private double _rightTiltCorrector = 0;
        private bool _allowRotation = true;
        private System.Drawing.Size _pictureSize;

        public float ViewAngleHorizontal { get; private set; } = 0;
        public float ViewAngleVertical { get; private set; } = 0;

        public double LeftTiltCorrector { get { return _leftTiltCorrector; } }
        public double RightTiltCorrector { get { return _rightTiltCorrector; } }

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

        private CompassViewDrawer compassViewDrawer = new CompassViewDrawer();
        private ElevationProfileBitmapDrawer elevationProfileBitmapDrawer;


        public CompassView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
        }

/*        public CompassView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
        }
*/
        public void Initialize(IAppContext context, bool allowRotation, System.Drawing.Size pictureSize, float leftTiltCorrector = 0, float rightTiltCorrector = 0, float headingCorrector = 0)
        {
            _context = context;
            _allowRotation = allowRotation;
            _pictureSize = pictureSize;
            _leftTiltCorrector = leftTiltCorrector;
            _rightTiltCorrector = rightTiltCorrector;
            _headingCorrector = headingCorrector;

            _context.Settings.SettingsChanged += OnSettingsChanged;

            elevationProfileBitmapDrawer = new ElevationProfileBitmapDrawer(_context);

            _paint = new Paint();
            _paint.SetARGB(255, 255, 255, 0);
            _paint.SetStyle(Paint.Style.Stroke);
            _paint.StrokeWidth = 3;
        }

        public void SetPoiViewItemList(PoiViewItemList list2)
        {
            list = list2.OrderByDescending(poi => poi.Poi.Altitude).ThenBy(poi => poi.Distance);
            
            var minAngleDiff = compassViewDrawer.GetMinItemAngleDiff(this.Width);
            _compassViewFilter.Reset();
            foreach (var item in list)
            {
                item.Visibility = CompassViewUtils.IsPoiVisible(item, _context.ElevationProfileData);

                if (item.Visibility == HorizonLib.Domain.Enums.Visibility.Invisible)
                {
                    continue;
                }

                if (_compassViewFilter.IsOverlapping(item, minAngleDiff))
                {
                    item.Visibility = HorizonLib.Domain.Enums.Visibility.Invisible;
                }
            }
            Invalidate();
        }

        public void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            InitializeViewDrawer(new System.Drawing.Size(this.Width, this.Height), _pictureSize);
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            var compassViewSize = new System.Drawing.Size(this.Width, this.Height);
            InitializeViewDrawer(compassViewSize, _pictureSize);
        }


        public void InitializeViewDrawer(System.Drawing.Size compassViewSize, System.Drawing.Size pictureSize)
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

            (ViewAngleHorizontal, ViewAngleVertical) = CompassViewUtils.AdjustViewAngles(
                _context.ViewAngleHorizontal, _context.ViewAngleVertical,
                compassViewSize, pictureSize, _allowRotation);

            float multiplier = (float)Math.Sqrt(compassViewSize.Width * compassViewSize.Height / 2000000.0);
            compassViewDrawer.Initialize(ViewAngleHorizontal, ViewAngleVertical, multiplier);
            elevationProfileBitmapDrawer.Initialize(ViewAngleHorizontal, ViewAngleVertical);

            Log.WriteLine(LogPriority.Debug, TAG, $"ViewAngle: {_context.ViewAngleHorizontal:F1}/{_context.ViewAngleVertical:F1}");
            Log.WriteLine(LogPriority.Debug, TAG, $"AdjustedViewAngle: {ViewAngleHorizontal:F1}/{ViewAngleVertical:F1}");

            RecalculateViewAngles(_scale);
        }

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            var heading = _context.Heading + HeadingCorrector;

            compassViewDrawer.OnDrawBackground(canvas);

            if (_context.Settings.ShowElevationProfile)
                PaintElevationProfileLines(canvas, heading);

            PaintVisiblePois(canvas, heading);
        }

        private void PaintVisiblePois(Canvas canvas, double heading)
        {
            canvas.Rotate(90, 0, 0);

            if (list != null)
            {
                foreach (var item in list)
                {
                    if (item.Visibility != HorizonLib.Domain.Enums.Visibility.Invisible)
                    {
                        compassViewDrawer.DrawItem(canvas, item, (float) heading, (float)_offsetX, (float) _offsetY, _leftTiltCorrector, _rightTiltCorrector, canvas.Width);
                    }
                }
            }
            
            canvas.Rotate(-90, 0, 0);
        }

        public void OnScroll(float distanceX)
        {
            var viewAngleHorizontal = _context.ViewAngleHorizontal;
            HeadingCorrector = HeadingCorrector + CompassViewUtils.GetHeadingDifference(viewAngleHorizontal, Width, distanceX / _scale);
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
            HeadingCorrector = 0;
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
            Invalidate();
        }
    }
} 
