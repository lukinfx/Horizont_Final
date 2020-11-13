using Android.Graphics;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawerSimpleWithHeight : CompassViewDrawer
    {
        public override void OnDrawBackground(Canvas canvas)
        {
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            if (item.Visibility == HorizonLib.Domain.Enums.Visibility.Visible)
            {
                canvas.DrawRect(0, -startX, endY - 50, -startX - 40, paintRect);
                canvas.DrawLine(0, -startX, endY, -startX, paintVisible);
                canvas.DrawText($"{item.Poi.Name} ({item.Poi.Altitude}m)", 10, -startX - 10, textpaint);
            }

            else if (item.Visibility == HorizonLib.Domain.Enums.Visibility.PartialyVisible)
            {
                canvas.DrawRect(0, -startX, endY - 50, -startX - 40, paintRectPartialyVisible);
                canvas.DrawLine(0, -startX, endY, -startX, paintPartialyVisible);
                canvas.DrawText($"{item.Poi.Name} ({item.Poi.Altitude}m)", 10, -startX - 10, textpaintPartialyVisible);
            }
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 1.7;
        }
    }
}