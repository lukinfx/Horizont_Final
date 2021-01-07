using Android.Graphics;
using HorizontApp.Providers;
using HorizontLib.Domain.ViewModel;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawerFullScreenRectangle : CompassViewDrawer
    {
        public CompassViewDrawerFullScreenRectangle(PoiCategoryBitmapProvider poiCategoryBitmapProvider)
            : base(poiCategoryBitmapProvider)
        {
        }

        public override void OnDrawBackground(Canvas canvas)
        {
            canvas.DrawRect(0, 0, canvas.Width, canvas.Height / 3, paintRect);
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            canvas.DrawLine(ToPixels(60), -startX, endY, -startX, GetPaint(item));
            canvas.DrawText(item.Poi.Name, ToPixels(70), -startX - ToPixels(10), GetTextPaint(item));
            canvas.DrawText($"{item.Poi.Altitude} m / {(item.Distance / 1000):F2} km", ToPixels(70), -startX + ToPixels(35), GetTextPaint(item));
        }

        public override void OnDrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            var bmp = poiCategoryBitmapProvider.GetCategoryIcon(item.Poi.Category);
            canvas.DrawBitmap(bmp, startX - ToPixels(30), 5, null);
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 8500.0 * multiplier / (double)canvasWidth;
        }
    }
}