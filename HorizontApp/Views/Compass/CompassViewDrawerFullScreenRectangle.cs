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
            canvas.DrawLine(0, -startX, endY, -startX, GetPaint(item));
            canvas.DrawText(item.Poi.Name, ToPixels(10), -startX - ToPixels(10), GetTextPaint(item));
            canvas.DrawText($"{item.Poi.Altitude} m / {(item.Distance / 1000):F2} km", ToPixels(10), -startX + ToPixels(35), GetTextPaint(item));
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 8500.0 * multiplier / (double)canvasWidth;
        }
    }
}