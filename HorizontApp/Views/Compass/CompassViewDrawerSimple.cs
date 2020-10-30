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

        public override void OnDrawItem(Canvas canvas, PoiViewItem item)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.Bearing, canvas.Width, adjustedViewAngleHorizontal);

            if (startX != null)
            {
                var endY = CompassViewUtils.GetYLocationOnScreen(item.Distance, item.AltitudeDifference, canvas.Height, adjustedViewAngleVertical);

                canvas.DrawRect(0, -startX.Value, endY - 50, -startX.Value - 40, paintRect);
                canvas.DrawLine(0, -startX.Value, endY, -startX.Value, paint);

                canvas.DrawText($"{item.Poi.Name}", 10, -startX.Value - 10, textpaint);
            }
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float heading, double leftTiltCorrector, double rightTiltCorrector, double canvasWidth)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.Bearing, canvas.Width, adjustedViewAngleHorizontal);

            if (startX != null)
            {
                var endY = CompassViewUtils.GetYLocationOnScreen(item.Distance, item.AltitudeDifference, canvas.Height, adjustedViewAngleVertical, startX.Value, leftTiltCorrector, rightTiltCorrector, canvasWidth);

                canvas.DrawRect(0, -startX.Value, endY - 50, -startX.Value - 40, paintRect);
                canvas.DrawLine(0, -startX.Value, endY, -startX.Value, paint);

                canvas.DrawText($"{item.Poi.Name}", 10, -startX.Value - 10, textpaint);
            }
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            return 1.7;
        }
    }
}