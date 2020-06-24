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
    public class CompassView : View
    {
        private Android.Graphics.Paint paint;
        private Android.Graphics.Paint textpaint;
        private PoiViewItemList list;
        public double Heading { get; set; }
        

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
            
            textpaint = new Android.Graphics.Paint();
            textpaint.SetARGB(255, 200, 255, 0);
            textpaint.TextSize = 24;
            Typeface normal = Typeface.Create("Arial", TypefaceStyle.Normal);
            textpaint.SetTypeface(normal);
        }

        //protected override void OnDraw(Android.Graphics.Canvas canvas)
        //{
        //    canvas.DrawLine(100, 0, 200, 100, paint);
        //}

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            canvas.Rotate(90, 0, 0);
            
            for (var x = 0; x < 360; x += 10)
            {
                var startX = CompassViewUtils.GetLocationOnScreen((float)Heading, (float)x, canvas.Width, 60/*TODO:camera vierw angle*/);
                if (startX != null)
                {
                    canvas.DrawLine(0, -startX.Value, 50, -startX.Value, paint);

                    canvas.DrawText($"{x}", 10, -startX.Value-5, textpaint);
                    //canvas.Restore();
                }
            }
            
            if (list != null)
            {
                foreach (var item in list)
                {
                    var startX = CompassViewUtils.GetLocationOnScreen((float)Heading, (float)item.Bearing, canvas.Width, 60); //TODO:camera vierw angle
                    if (startX != null)
                    {
                        
                        canvas.DrawLine(0, -startX.Value, 200, -startX.Value, paint);

                        canvas.DrawText(item.Name, 10, -startX.Value-5, textpaint);
                    }
                }

            }
        }
    }
}