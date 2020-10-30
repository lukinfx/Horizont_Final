using Android.Graphics;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawerEachPoiSeparate : CompassViewDrawer
    {
        public override void OnDrawBackground(Canvas canvas)
        {
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            canvas.DrawRect(0, -startX + 50, endY - 50, -startX - 50, paintRect);
            canvas.DrawLine(0, -startX, endY, -startX, paint);

            canvas.DrawText(item.Poi.Name, 10, -startX - 10, textpaint);
            canvas.DrawText($"{item.Poi.Altitude} m / {(item.Distance / 1000):F2} km", 10, -startX + 35, textpaint);
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 4;
        }
    }
}