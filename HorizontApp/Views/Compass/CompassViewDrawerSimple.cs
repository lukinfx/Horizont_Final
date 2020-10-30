using Android.Graphics;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawerSimple : CompassViewDrawer
    {
        public override void OnDrawBackground(Canvas canvas)
        {
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            canvas.DrawRect(0, -startX, endY - 50, -startX - 40, paintRect);
            canvas.DrawLine(0, -startX, endY, -startX, paint);

            canvas.DrawText($"{item.Poi.Name}", 10, -startX - 10, textpaint);
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 1.7;
        }
    }
}