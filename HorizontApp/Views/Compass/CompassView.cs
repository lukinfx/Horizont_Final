using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Providers;
using HorizontApp.Utilities;
using HorizontApp.Views.Compass;

namespace HorizontApp.Views
{

    

    public class CompassView : View
    {
        public static PoiViewItemList list;
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
            list = list2;
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
                    compassViewDrawer.OnDrawItem(canvas, item, (float)Heading);
                }
            }
        }
    }
}