using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Utilities;
using HorizontApp.Views.Compass;
using System;
using System.Linq;

namespace HorizontApp.Views
{
    public class CompassView : View
    {
        public static IOrderedEnumerable<PoiViewItem> list;
        private CompassViewFilter _compassViewFilter = new CompassViewFilter();
        public double Heading { get; set; }

        private CompassViewDrawer compassViewDrawer;

        public CompassView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public void SetPoiViewItemList(PoiViewItemList list2)
        {
            list = list2.OrderByDescending(poi => poi.Poi.Altitude);
            
            var d = compassViewDrawer.GetMinItemAngleDiff(this.Width);

            _compassViewFilter.Reset();
            foreach (var item in list)
            {
                item.Visibility = _compassViewFilter.Filter(item, d);
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
        }

        private void Initialize()
        {
            CompassViewSettings.Instance().SettingsChanged += OnSettingsChanged;
            
            InitializeViewDrawer();
        }

        private void InitializeViewDrawer()
        {
            switch (CompassViewSettings.Instance().AppStyle)
                {
                    case AppStyles.NewStyle:
                        compassViewDrawer = new CompassViewDrawerNewStyle();
                        break;
                    case AppStyles.OldStyle:
                        compassViewDrawer = new CompassViewDrawerOldStyle();
                        break;
                }

            compassViewDrawer.ViewAngleHorizontal = CompassViewSettings.Instance().ViewAngleHorizontal;
            compassViewDrawer.ViewAngleVertical = CompassViewSettings.Instance().ViewAngleVertical;
            compassViewDrawer.Initialize();
        }
        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            compassViewDrawer.OnDrawBackground(canvas);
            
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
            Heading += CompassViewUtils.GetHeadingDifference(viewAngleHorizontal, Width, distanceX);
            Invalidate();
        }
    }
}