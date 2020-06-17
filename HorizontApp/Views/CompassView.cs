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

namespace HorizontApp.Views
{
    public class CompassView : View
    {
        private Android.Graphics.Paint paint;
        private PoiViewItemList list;

        public CompassView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public void SetPoiViewItemList(PoiViewItemList list)
        {
            this.list = list;
        }

        public CompassView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private void Initialize()
        {
            paint = new Android.Graphics.Paint();
            paint.SetARGB(255, 200, 255, 0);
            paint.SetStyle(Paint.Style.FillAndStroke);
            paint.StrokeWidth = 4;
        }

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            //foreach(var item in list) ...

            canvas.DrawRect(new Android.Graphics.Rect(10, 10, 100, 200), paint);
        }
    }
}