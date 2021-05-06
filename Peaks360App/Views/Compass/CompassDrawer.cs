using Android.Graphics;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using System;
using System.Collections.Generic;
using Android.Content.Res;
using Android.Text;
using Android.Util;
using Peaks360App.Providers;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.Views.Compass
{
    public class CompassViewDrawer
    {
        protected Paint paintVisible;
        protected Paint paintPartialyVisible;
        protected Paint paintRect;
        protected Paint paintRectPartialyVisible;
        protected Paint paintRectSelectedItem;
        protected Paint textpaint;
        protected Paint textpaintSelected;
        protected Paint textpaintUnimportant;
        protected Paint paintWhite;
        protected Paint paintGray;
        protected TextPaint textPaintForEllipsize;
        protected Paint textpaintPartialyVisible;
        protected float viewAngleHorizontal;
        protected float viewAngleVertical;
        protected float adjustedViewAngleHorizontal;
        protected float adjustedViewAngleVertical;
        protected float multiplier;
        protected IPoiCategoryBitmapProvider poiCategoryBitmapProvider;

        public CompassViewDrawer(IPoiCategoryBitmapProvider poiCategoryBitmapProvider)
        {
            this.poiCategoryBitmapProvider = poiCategoryBitmapProvider;

            paintVisible = new Paint();
            paintVisible.SetARGB(255, 200, 255, 0);
            paintVisible.SetStyle(Paint.Style.FillAndStroke);
            paintVisible.StrokeWidth = 4;

            paintPartialyVisible = new Paint();
            paintPartialyVisible.SetARGB(120, 200, 255, 0);
            paintPartialyVisible.SetStyle(Paint.Style.FillAndStroke);
            paintPartialyVisible.StrokeWidth = 4;

            paintRect = new Paint();
            paintRect.SetARGB(150, 0, 0, 0);
            paintRect.SetStyle(Paint.Style.FillAndStroke);
            paintRect.StrokeWidth = 4;

            paintRectPartialyVisible = new Paint();
            paintRectPartialyVisible.SetARGB(75, 0, 0, 0);
            paintRectPartialyVisible.SetStyle(Paint.Style.FillAndStroke);
            paintRectPartialyVisible.StrokeWidth = 4;

            Typeface normal = Typeface.Create("Arial", TypefaceStyle.Normal);

            paintRectSelectedItem = new Paint();
            paintRectSelectedItem.SetARGB(150, 255, 255, 255);
            paintRectSelectedItem.SetStyle(Paint.Style.FillAndStroke);
            paintRectSelectedItem.StrokeWidth = 4;

            textpaint = new Paint();
            textpaint.SetARGB(255, 200, 255, 0);
            textpaint.TextSize = 36;
            textpaint.AntiAlias = true;
            textpaint.SetTypeface(normal);

            textpaintSelected = new Paint();
            textpaintSelected.SetARGB(255, 0, 0, 0);
            textpaintSelected.TextSize = 36;
            textpaintSelected.AntiAlias = true;
            textpaintSelected.SetTypeface(normal);

            textpaintUnimportant = new Paint();
            textpaintUnimportant.SetARGB(255, 217, 231, 174);
            textpaintUnimportant.TextSize = 36;
            textpaintUnimportant.AntiAlias = true;
            textpaintUnimportant.SetTypeface(normal); 

            textPaintForEllipsize = new TextPaint();
            textPaintForEllipsize.SetARGB(255, 200, 255, 0);
            textPaintForEllipsize.SetStyle(Paint.Style.Fill);
            textPaintForEllipsize.TextSize  = 36;
            textPaintForEllipsize.AntiAlias = true;
            textPaintForEllipsize.TextAlign = Paint.Align.Left;
            textPaintForEllipsize.LinearText = true;

            textpaintPartialyVisible = new Paint();
            textpaintPartialyVisible.SetARGB(120, 200, 255, 0);
            textpaintPartialyVisible.TextSize = 36;
            textpaintPartialyVisible.SetTypeface(normal);

            paintWhite = new Paint();
            paintWhite.SetARGB(255, 255, 255, 255);
            paintWhite.TextSize = 36;
            paintWhite.SetTypeface(normal);

            paintGray = new Paint();
            paintGray.SetARGB(255, 128, 128, 128);
            paintGray.TextSize = 36;
            paintGray.SetTypeface(normal);

            multiplier = 1;
        }

        protected string EllipsizeText(string text, float textWidth)
        {
            return TextUtils.Ellipsize(text, textPaintForEllipsize, textWidth, TextUtils.TruncateAt.End);
        }

        protected Paint GetTextPaint(PoiViewItem item)
        {
            if (item.Selected)
                return textpaintSelected;

            if (string.IsNullOrEmpty(item.Poi.Wikidata) && string.IsNullOrEmpty(item.Poi.Wikidata))
                return textpaintUnimportant;

            return item.Visibility == Visibility.Visible ? textpaint : textpaintPartialyVisible;
        }

        protected Paint GetPaint(PoiViewItem item)
        {
            return item.Visibility == Visibility.Visible ? paintVisible : paintPartialyVisible;
        }

        protected Paint GetRectPaint(PoiViewItem item)
        {
            if (item.Selected)
                return paintRectSelectedItem;

            return item.Visibility == Visibility.Visible ? paintRect : paintRectPartialyVisible;
        }

        protected float ToPixels(float dpi)
        {
            return multiplier * dpi;
        }

        public virtual void Initialize(Resources resources, float viewAngleHorizontal, float viewAngleVertical, float multiplier)
        {
            this.multiplier = multiplier;

            this.viewAngleHorizontal = viewAngleHorizontal;
            this.viewAngleVertical = viewAngleVertical;
            adjustedViewAngleHorizontal = viewAngleHorizontal;
            adjustedViewAngleVertical = viewAngleVertical;

            paintVisible.StrokeWidth = ToPixels(4);
            paintPartialyVisible.StrokeWidth = ToPixels(4);
            paintRect.StrokeWidth = ToPixels(4);
            paintRectPartialyVisible.StrokeWidth = ToPixels(4);

            textpaint.TextSize = ToPixels(36);
            textpaintPartialyVisible.TextSize = ToPixels(36);

            poiCategoryBitmapProvider.Initialize(resources, new Size((int)ToPixels(60), (int)ToPixels(60)));
        }

        public virtual double GetMinItemAngleDiff(int canvasWidth) { return 0; }
        /// <summary>
        /// Draws background of canvas
        /// </summary>
        /// <param name="canvas"></param>
        public virtual void OnDrawBackground(Android.Graphics.Canvas canvas) { }
        /// <summary>
        /// Draws item into given canvas
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="item"></param>
        /// <param name="heading"></param>
        public virtual void OnDrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY) { }

        public virtual void OnDrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY) { }

        public (float? x,float? y) GetXY(PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, float canvasWidth, float canvasHeight)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.GpsLocation.Bearing, canvasWidth, adjustedViewAngleHorizontal, offsetX);

            if (!startX.HasValue)
                return (null, null);
            
            double verticalAngleCorrection = CompassViewUtils.GetTiltCorrection(item.GpsLocation.Bearing.Value, heading, viewAngleHorizontal, leftTiltCorrector, rightTiltCorrector);
            double verticalAngle = item.VerticalViewAngle;

            float endY = CompassViewUtils.GetYLocationOnScreen(verticalAngle + verticalAngleCorrection, canvasHeight, adjustedViewAngleVertical);
            return (startX, endY);
        }

        public void DrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, double canvasWidth)
        {
            var (x, y) = GetXY(item, heading, offsetX, offsetY, leftTiltCorrector, rightTiltCorrector, canvas.Width, canvas.Height);
            if (x != null && y != null)
            {
                OnDrawItem(canvas, item, x.Value, y.Value + offsetY);
            }
        }

        public void DrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, double canvasWidth)
        {
            var (x, y) = GetXY(item, heading, offsetX, offsetY, leftTiltCorrector, rightTiltCorrector, canvas.Width, canvas.Height);
            if (x != null && y != null)
            {
                OnDrawItemIcon(canvas, item, x.Value, y.Value + offsetY);
            }
        }

        public virtual float GetItemWidth()
        {
            return 0;
        }

        public bool IsItemClicked(PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, float canvasWidth, float canvasHeight, float clickX, float clickY)
        {
            var (itemX, itemY) = GetXY(item, heading, offsetX, offsetY, leftTiltCorrector, rightTiltCorrector, canvasWidth, canvasHeight);
            if (itemX.HasValue && itemY.HasValue)
            { 
                float xDiff = itemX.Value - clickX;
                if (Math.Abs(xDiff) < GetItemWidth() && clickY < itemY + offsetY)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetScaledViewAngle(float scaledViewAngleHorizontal, float scaledViewAngleVertical)
        {
            adjustedViewAngleHorizontal = scaledViewAngleHorizontal;
            adjustedViewAngleVertical = scaledViewAngleVertical;
        }
    }
}