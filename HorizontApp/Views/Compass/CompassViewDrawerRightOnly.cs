using Android.Graphics;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Utilities;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawerRightOnly : CompassViewDrawer
    {
        public override void OnDrawBackground(Canvas canvas)
        {
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float heading)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.Bearing, canvas.Width, ViewAngleHorizontal);

            if (startX != null)
            {
                var endY = CompassViewUtils.GetYLocationOnScreen(item.Distance, item.AltitudeDifference, canvas.Height, ViewAngleVertical);

                canvas.DrawRect(0, -startX.Value, endY - 50, -startX.Value - 50, paintRect);
                canvas.DrawLine(0, -startX.Value, endY, -startX.Value, paint);

                canvas.DrawText($"{item.Poi.Name}", 10, -startX.Value - 10, textpaint);
            }
        }

        public override double GetMinItemRightAngleDiff(int canvasWidth)
        {
            return 1.7;
        }
        public override double GetMinItemLeftAngleDiff(int canvasWidth)
        {
            return 1.7;
        }
    }
}