using Android.Graphics;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawerFullScreenRectangle : CompassViewDrawer
    {
        public override void OnDrawBackground(Canvas canvas)
        {
            canvas.DrawRect(0, 0, canvas.Width, canvas.Height / 3, paintRect);
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float startX, float endY)
        {
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