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
            canvas.DrawRect(0, -startX + ToPixels(50), endY - ToPixels(50), -startX - ToPixels(50), GetRectPaint(item));
            canvas.DrawLine(0, -startX, endY, -startX, GetPaint(item));
            canvas.DrawText(item.Poi.Name, ToPixels(10), -startX - ToPixels(10), GetTextPaint(item));
            canvas.DrawText($"{item.Poi.Altitude} m / {(item.Distance / 1000):F2} km", ToPixels(10), -startX + ToPixels(35), GetTextPaint(item));
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 4;
        }
    }
}