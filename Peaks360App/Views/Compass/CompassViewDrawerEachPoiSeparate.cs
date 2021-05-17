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
            float yDrawableAreaEnd = endY - ToPixels(25);
            float y5 = endY;
            float yDrawableAreaStart = ToPixels(90);

            float x1 = startX - ToPixels(35);
            float x2 = startX + ToPixels(35);

            var path = new Path();
            path.MoveTo(y3, -x1);
            path.LineTo(y4, -x1);
            path.LineTo(y5, -startX);
            path.LineTo(y4, -x2);
            path.LineTo(y3, -x2);
            canvas.DrawPath(path, GetRectPaint(item));

            var dyTextSpace = ToPixels(10);
            var dyImportantIcon = ToPixels(30);
            var dyFavouriteIcon = ToPixels(40);
            var dyIconSpace = ToPixels(3);

            var textWidth = yDrawableAreaEnd - yDrawableAreaStart - dyTextSpace - (item.IsImportant()? dyImportantIcon+dyIconSpace : 0) - (item.Poi.Favorite? dyFavouriteIcon+dyIconSpace : 0);
            var text1 = EllipsizeText(item.Poi.Name, textWidth/multiplier);
            var text2 = EllipsizeText($"{item.Poi.Altitude} m / {(item.GpsLocation.Distance / 1000):F2} km", textWidth/multiplier);

            var textPaint = GetTextPaint(item);
            canvas.DrawText(text1, yDrawableAreaStart, -startX - ToPixels(4), textPaint);
            canvas.DrawText(text2, yDrawableAreaStart, -startX + ToPixels(29), textPaint);

            Rect bounds = new Rect();
            textPaint.GetTextBounds(text1.ToCharArray(), 0, text1.Length, bounds);

            float iconOffset = yDrawableAreaStart + bounds.Width() + dyTextSpace;
            if (item.IsImportant() && iconOffset + dyImportantIcon <= yDrawableAreaEnd)
            {
                canvas.DrawBitmap(item.Selected ? infoBitmapBlack : infoBitmapYellow, iconOffset, -startX - ToPixels(30), null);
                iconOffset += dyImportantIcon + dyIconSpace;
            }

            if (item.Poi.Favorite && iconOffset + dyFavouriteIcon <= yDrawableAreaEnd)
            {
                canvas.DrawBitmap(favouriteBitmap, iconOffset, -startX - ToPixels(40), null);
                iconOffset += dyFavouriteIcon + dyIconSpace;
            }

        }

        public override void OnDrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            int circleSize = 42;
            canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize), paintBlack);
            canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize - 3), item.Selected ? paintWhite : paintGray);

            var bmp = poiCategoryBitmapProvider.GetCategoryIcon(item.Poi.Category);
            canvas.DrawBitmap(bmp, startX - ToPixels(33), ToPixels(circleSize - 33), null/*ColorFilterPoiItem.GetPaintFilter(item)*/);
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