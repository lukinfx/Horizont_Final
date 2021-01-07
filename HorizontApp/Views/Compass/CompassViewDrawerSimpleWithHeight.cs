using Android.Graphics;
using HorizontApp.Providers;
using HorizontLib.Domain.ViewModel;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawerSimpleWithHeight : CompassViewDrawer
    {
        public CompassViewDrawerSimpleWithHeight(IPoiCategoryBitmapProvider poiCategoryBitmapProvider)
            : base(poiCategoryBitmapProvider)
        {
        }

        public override void OnDrawBackground(Canvas canvas)
        {
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            canvas.DrawRect(0, -startX, endY - ToPixels(50), -startX - ToPixels(40), GetRectPaint(item));
            canvas.DrawLine(ToPixels(30), -startX, endY, -startX, GetPaint(item));
            canvas.DrawText($"{item.Poi.Name} ({item.Poi.Altitude}m)", ToPixels(70), -startX - ToPixels(10), GetTextPaint(item));
        }

        public override void OnDrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            var bmp = poiCategoryBitmapProvider.GetCategoryIcon(item.Poi.Category);
            canvas.DrawBitmap(bmp, startX, 5, null);
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 4000.0 * multiplier / (double)canvasWidth;
        }
    }
}