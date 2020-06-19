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
        private static PoiViewItemList list;
        private CompassProvider compassProvider = new CompassProvider();
        

        public CompassView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public static void SetPoiViewItemList(PoiViewItemList list2)
        {
            list = list2;
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
            compassProvider.ToggleCompass(); 
        }

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            if (list != null)
            {
                foreach (var item in list.List)
                {
                    var startX = CompassViewUtils.GetLocationOnScreen((float)compassProvider.Heading, (float)item.Heading, canvas.Width, 30/*TODO: camera veiw angle*/);
                    if (startX != null)
                    {
                        canvas.DrawLine(startX.Value, 0, startX.Value, 100, paint);
                    }
                }
            }
        }
    }
}