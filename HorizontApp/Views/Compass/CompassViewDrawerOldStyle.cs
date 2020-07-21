using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Utilities;

namespace HorizontApp.Views.Compass
{
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
}