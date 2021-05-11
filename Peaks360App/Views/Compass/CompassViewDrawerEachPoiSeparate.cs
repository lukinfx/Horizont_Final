using Android.Graphics;
using Peaks360App.Providers;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.ViewModel;

namespace Peaks360App.Views.Compass
{
    public class CompassViewDrawerEachPoiSeparate : CompassViewDrawer
    {
        private Paint _importantPoiPaint = new Paint();
        public CompassViewDrawerEachPoiSeparate(IPoiCategoryBitmapProvider poiCategoryBitmapProvider)
            : base(poiCategoryBitmapProvider)
        {
        }

        public override void OnDrawBackground(Canvas canvas)
        {
        }

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            //   x1   x2
            //    ----  y1
            //   /    \ y3
            //   |    | y2
            //   | T  | y6
            //   | e  |
            //   | x  | y4
            //   \ t /
            //    \ /
            //     V    y5

            float y1 = 0;
            float y2 = ToPixels(70);
            float y3 = y2 / 2 + 1;
            float y4 = endY - ToPixels(50);
            float y5 = endY;
            float y6 = ToPixels(90);

            float x1 = startX - ToPixels(35);
            float x2 = startX + ToPixels(35);

            var path = new Path();
            path.MoveTo(y3, -x1);
            path.LineTo(y4, -x1);
            path.LineTo(y5, -startX);
            path.LineTo(y4, -x2);
            path.LineTo(y3, -x2);
            canvas.DrawPath(path, GetRectPaint(item));

            var textWidth = y4 - y2 - 10;
            var text1 = EllipsizeText(item.Poi.Name, textWidth/multiplier);
            var text2 = EllipsizeText($"{item.Poi.Altitude} m / {(item.GpsLocation.Distance / 1000):F2} km", textWidth/multiplier);
            canvas.DrawText(text1, y6, -startX - ToPixels(4), GetTextPaint(item));
            canvas.DrawText(text2, y6, -startX + ToPixels(29), GetTextPaint(item));
        }

        public override void OnDrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            int circleSize = 42;
            canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize), paintBlack);
            canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize - 3), paintWhite /*item.IsFullyVisible() ? paintWhite : paintGray*/);

            var bmp = poiCategoryBitmapProvider.GetCategoryIcon(item.Poi.Category);
            canvas.DrawBitmap(bmp, startX - ToPixels(33), ToPixels(circleSize - 33), ColorFilterPoiItem.GetPaintFilter(item));
        }

        public override double GetMinItemAngleDiff(int canvasWidth)
        {
            var itemWidth = 90; //px
            var percentInPixels = itemWidth / (canvasWidth / multiplier);
            var minAngle = percentInPixels * viewAngleHorizontal;
            return minAngle;
        }

        public override float GetItemWidth()
        {
            return ToPixels(35);
        }
    }
}