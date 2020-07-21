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

namespace HorizontApp.Views
{

    public class CompassViewDrawer
    {
        protected Android.Graphics.Paint paint;
        protected Android.Graphics.Paint paintRect;
        protected Android.Graphics.Paint textpaint;

        public float ViewAngleHorizontal { protected get; set; }
        public float ViewAngleVertical { protected get; set; }

        public virtual void Initialize()
        {
            paint = new Android.Graphics.Paint();
            paint.SetARGB(255, 200, 255, 0);
            paint.SetStyle(Paint.Style.FillAndStroke);
            paint.StrokeWidth = 4;

            paintRect = new Android.Graphics.Paint();
            paintRect.SetARGB(150, 0, 0, 0);
            paintRect.SetStyle(Paint.Style.FillAndStroke);
            paintRect.StrokeWidth = 4;

            textpaint = new Android.Graphics.Paint();
            textpaint.SetARGB(255, 200, 255, 0);
            textpaint.TextSize = 36;
            Typeface normal = Typeface.Create("Arial", TypefaceStyle.Normal);
            textpaint.SetTypeface(normal);
        }

        public virtual void OnDrawBackground(Android.Graphics.Canvas canvas) { }
        public virtual void OnDrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float heading) { }
    }

    public class CompassViewDrawerNewStyle : CompassViewDrawer
    {
        public override void OnDrawBackground(Canvas canvas)
        {
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float heading)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.Bearing, canvas.Width, ViewAngleHorizontal);

            if (startX != null)
            {
                var endY = CompassViewUtils.GetYLocationOnScreen(item.Distance, item.AltitudeDifference, canvas.Height, ViewAngleVertical);

                canvas.DrawRect(0, -startX.Value + 50, endY - 50, -startX.Value - 50, paintRect);
                canvas.DrawLine(0, -startX.Value, endY, -startX.Value, paint);

                canvas.DrawText(item.Poi.Name, 10, -startX.Value - 10, textpaint);
                canvas.DrawText($"{item.Poi.Altitude} m / {(item.Distance / 1000):F2} km", 10, -startX.Value + 35, textpaint);
            }
        }
    }

    public class CompassViewDrawerOldStyle : CompassViewDrawer
    {
        public override void OnDrawBackground(Canvas canvas)
        {
            canvas.DrawRect(0, 0, canvas.Width, canvas.Height / 3, paintRect);
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float heading)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.Bearing, canvas.Width, ViewAngleHorizontal);

            if (startX != null)
            {
                var endY = CompassViewUtils.GetYLocationOnScreen(item.Distance, item.AltitudeDifference, canvas.Height, ViewAngleVertical);

                canvas.DrawLine(0, -startX.Value, endY, -startX.Value, paint);

                canvas.DrawText(item.Poi.Name, 10, -startX.Value - 10, textpaint);
                canvas.DrawText($"{item.Poi.Altitude} m / {(item.Distance / 1000):F2} km", 10, -startX.Value + 35, textpaint);
            }
        }
    }

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