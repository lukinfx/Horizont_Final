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

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float heading)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.Bearing, canvas.Width, adjustedViewAngleHorizontal);

            if (startX != null)
            {
                var endY = CompassViewUtils.GetYLocationOnScreen(item.Distance, item.AltitudeDifference, canvas.Height, adjustedViewAngleVertical);

                canvas.DrawRect(0, -startX.Value + 50, endY - 50, -startX.Value - 50, paintRect);
                canvas.DrawLine(0, -startX.Value, endY, -startX.Value, paint);

                canvas.DrawText(item.Poi.Name, 10, -startX.Value - 10, textpaint);
                canvas.DrawText($"{item.Poi.Altitude} m / {(item.Distance / 1000):F2} km", 10, -startX.Value + 35, textpaint);
            }
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 4;
        }
    }
}