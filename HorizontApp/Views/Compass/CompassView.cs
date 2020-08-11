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

        private float viewAngleHorizontal;
        private float viewAngleVertical;
        public float ViewAngleHorizontal { 
            get
            {
                return viewAngleHorizontal;
            }
            set 
            { 
                compassViewDrawer.ViewAngleHorizontal = value;
                viewAngleHorizontal = value;
            } 
        }
        public float ViewAngleVertical
        {
            get
            {
                return viewAngleHorizontal;
            }
            set
            {
                compassViewDrawer.ViewAngleVertical = value;
                viewAngleVertical = value;
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
            list = list2.OrderByDescending(poi => poi.Poi.Altitude);

            _compassViewFilter.Reset();
            foreach (var item in list)
            {
                item.Visibility = _compassViewFilter.Filter(item);
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

            compassViewDrawer.ViewAngleHorizontal = ViewAngleHorizontal;
            compassViewDrawer.ViewAngleVertical = ViewAngleVertical;
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
            Heading += CompassViewUtils.GetHeadingDifference(viewAngleHorizontal, Width, distanceX);
            Invalidate();
        }
    }
}