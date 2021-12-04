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

        public override void OnDrawItem(Canvas canvas, PoiViewItem item, float startX, float endY, bool displayOverlapped, bool highLightSelected)
        {
            //   x1   x2
            //    /--\  yIconStart
            //   |ICON| yIconMiddle
            //   |\__/| yIconEnd
            //   | T  | yTextAreaEnd
            //   | e  |
            //   | x  | yTipBend
            //   \ t /  yTextAreaEnd
            //    \ /
            //     V    yTipEnd

            //float yIconStart = 0;
            float yIconEnd = ToPixels(70);
            float yIconMiddle = yIconEnd / 2 + 1;
            float yTextAreaStart = ToPixels(90);
            float yTextAreaEnd = endY - ToPixels(25);
            float yTipBend = endY - ToPixels(50);
            float yTipEnd = endY;

            if (!item.Overlapped)
            {
                float x1 = startX - ToPixels(35);
                float x2 = startX + ToPixels(35);

                var path = new Path();
                path.MoveTo(yIconMiddle, -x1);
                path.LineTo(yTipBend, -x1);
                path.LineTo(yTipEnd, -startX);
                path.LineTo(yTipBend, -x2);
                path.LineTo(yIconMiddle, -x2);
                canvas.DrawPath(path, GetRectPaint(item, highLightSelected));

                var dyTextSpace = ToPixels(10);
                var dyImportantIcon = ToPixels(30);
                var dyFavouriteIcon = ToPixels(40);
                var dyIconSpace = ToPixels(3);

                var textWidth = yTextAreaEnd - yTextAreaStart - dyTextSpace - (item.IsImportant() ? dyImportantIcon + dyIconSpace : 0) - (item.Poi.Favorite ? dyFavouriteIcon + dyIconSpace : 0);
                var text1 = EllipsizeText(item.Poi.Name, textWidth / multiplier);
                var text2 = EllipsizeText($"{item.Poi.Altitude} m / {(item.GpsLocation.Distance / 1000):F2} km", textWidth / multiplier);

                var textPaint = GetTextPaint(item);
                canvas.DrawText(text1, yTextAreaStart, -startX - ToPixels(4), textPaint);
                canvas.DrawText(text2, yTextAreaStart, -startX + ToPixels(29), textPaint);

                Rect bounds = new Rect();
                textPaint.GetTextBounds(text1.ToCharArray(), 0, text1.Length, bounds);

                float iconOffset = yTextAreaStart + bounds.Width() + dyTextSpace;
                if (item.IsImportant() && iconOffset + dyImportantIcon <= yTextAreaEnd)
                {
                    canvas.DrawBitmap(item.Selected ? infoBitmapBlack : infoBitmapYellow, iconOffset, -startX - ToPixels(30), null);
                    iconOffset += dyImportantIcon + dyIconSpace;
                }

                if (item.Poi.Favorite && iconOffset + dyFavouriteIcon <= yTextAreaEnd)
                {
                    canvas.DrawBitmap(favouriteBitmap, iconOffset, -startX - ToPixels(40), null);
                    iconOffset += dyFavouriteIcon + dyIconSpace;
                }
            }

            if (displayOverlapped)
            {
                if (item.Poi.Favorite)
                {
                    canvas.DrawCircle(endY, -startX, ToPixels(30), paintTapFavourite);
                    canvas.DrawCircle(endY, -startX, ToPixels(6), paintWhite);
                }
                else if (item.Selected)
                {
                    canvas.DrawCircle(endY, -startX, ToPixels(30), paintRectSelectedItem);
                    canvas.DrawCircle(endY, -startX, ToPixels(6), paintBlack);
                }
                else
                {
                    canvas.DrawCircle(endY, -startX, ToPixels(30), paintTapArea);
                    canvas.DrawCircle(endY, -startX, ToPixels(6), paintWhite);
                }
            }

        }

        public override void OnDrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            {
                int circleSize = 42;
                canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize), paintBlack);
                canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize - 3), item.Selected ? paintWhite : paintGray);
                var bmp = poiCategoryBitmapProvider.GetCategoryIcon(item.Poi.Category);
                canvas.DrawBitmap(bmp, startX - ToPixels(33), ToPixels(circleSize - 33), ColorFilterPoiItem.GetGrayScalePaint());
            }

            //{
            //    int circleSize = 42;
            //    canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize), paintBlack);
            //    canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize - 3), item.Selected ? paintWhite : paintGray);
            //    canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize - 8), paintBlack);
            //    canvas.DrawCircle(startX, ToPixels(circleSize), ToPixels(circleSize - 14), GetRectPaint(item, false));

            //    var bmp = poiCategoryBitmapProvider.GetCategoryIcon(item.Poi.Category);

            //    var bmpStartX = (int) (startX - ToPixels(20));
            //    var bmpStartY = (int) (ToPixels(circleSize - 20));
            //    canvas.DrawBitmap(bmp, null, new Rect(bmpStartX, bmpStartY, bmpStartX + (int) ToPixels(40), bmpStartY + (int) ToPixels(40)), null /*ColorFilterPoiItem.GetPaintFilter(item)*/);
            //}
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