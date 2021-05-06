﻿using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Peaks360App.Providers;
using Peaks360App.Utilities;
using Peaks360Lib.Domain.Models;
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
            //   /    \ y2
            //   |    | y3
            //   |    |
            //   |    |
            //   |    | y4
            //   \   /
            //    \ /
            //     V    y5

            float y1 = 0;
            float y2 = ToPixels(70);
            float y3 = y2 / 2 + 1;
            float y4 = endY - ToPixels(50);
            float y5 = endY;

            float x1 = startX - ToPixels(35);
            float x2 = startX + ToPixels(35);

            canvas.DrawArc(new RectF(y1, -x2, y2, -x1), 90, 180, true, GetRectPaint(item));

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
            canvas.DrawText(text1, ToPixels(75), -startX - ToPixels(4), GetTextPaint(item));
            canvas.DrawText(text2, ToPixels(75), -startX + ToPixels(29), GetTextPaint(item));
        }

        public override void OnDrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY)
        {
            if (string.IsNullOrEmpty(item.Poi.Wikidata) && string.IsNullOrEmpty(item.Poi.Wikidata))
            {
                canvas.DrawCircle(startX, ToPixels(35), ToPixels(35), paintGray);
            }
            else
            {
                canvas.DrawCircle(startX, ToPixels(35), ToPixels(35), paintWhite);
            }

            var bmp = poiCategoryBitmapProvider.GetCategoryIcon(item.Poi.Category);
            canvas.DrawBitmap(bmp, startX - ToPixels(30), ToPixels(5), ImageViewHelper.GetImportancePaint(item.Poi));

            //details icon
            /*var img = BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Android.Resource.Drawable.IcMenuInfoDetails);
            var bmp2 = Bitmap.CreateScaledBitmap(img, (int)ToPixels(60), (int)ToPixels(60), false);
            canvas.DrawBitmap(bmp2, startX - ToPixels(30), endY - ToPixels(60), null);*/
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