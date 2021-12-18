using System;
using Android.Text;
using Android.Util;
using Android.Graphics;
using Android.Content.Res;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using Peaks360App.Providers;

namespace Peaks360App.Views.Compass
{
    public class CompassViewDrawer
    {
        protected Paint paintTapArea;
        protected Paint paintTapFavourite;
        protected Paint paintRect;
        protected Paint paintRectSelectedItem;
        protected Paint textpaint;
        protected Paint textpaintSelected;
        protected Paint paintWhite;
        protected Paint paintGray;
        protected Paint paintBlack;
        protected TextPaint textPaintForEllipsize;
        protected Bitmap infoBitmapYellow;
        protected Bitmap infoBitmapBlack;
        protected Bitmap favouriteBitmap;

        protected float viewAngleHorizontal;
        protected float viewAngleVertical;
        protected float adjustedViewAngleHorizontal;
        protected float adjustedViewAngleVertical;
        protected float multiplier;
        protected IPoiCategoryBitmapProvider poiCategoryBitmapProvider;

        public CompassViewDrawer(IPoiCategoryBitmapProvider poiCategoryBitmapProvider)
        {
            Typeface normal = Typeface.Create("Arial", TypefaceStyle.Normal);

            this.poiCategoryBitmapProvider = poiCategoryBitmapProvider;

            paintTapArea = new Paint();
            paintTapArea.SetARGB(100, 0, 0, 0);
            paintTapArea.SetStyle(Paint.Style.Fill);

            paintTapFavourite = new Paint();
            paintTapFavourite.SetARGB(150, 255, 195, 0);
            paintTapFavourite.SetStyle(Paint.Style.Fill);

            //pozadi bodu
            paintRect = new Paint();
            paintRect.SetARGB(100, 200, 200, 200);
            paintRect.SetStyle(Paint.Style.FillAndStroke);
            paintRect.StrokeWidth = 4;

            paintRectSelectedItem = new Paint();
            paintRectSelectedItem.SetARGB(255, 255, 255, 255);
            paintRectSelectedItem.SetStyle(Paint.Style.FillAndStroke);
            paintRectSelectedItem.StrokeWidth = 4;

            //text bodu
            textpaint = new Paint();
            textpaint.SetARGB(255, 0, 0, 0);
            textpaint.TextSize = 36;
            textpaint.AntiAlias = true;
            textpaint.SetTypeface(normal);

            textpaintSelected = new Paint();
            textpaintSelected.SetARGB(255, 0, 0, 0);
            textpaintSelected.TextSize = 36;
            textpaintSelected.AntiAlias = true;
            textpaintSelected.SetTypeface(normal);

            textPaintForEllipsize = new TextPaint();
            textPaintForEllipsize.SetARGB(255, 200, 255, 0);
            textPaintForEllipsize.SetStyle(Paint.Style.Fill);
            textPaintForEllipsize.TextSize  = 36;
            textPaintForEllipsize.AntiAlias = true;
            textPaintForEllipsize.TextAlign = Paint.Align.Left;
            textPaintForEllipsize.LinearText = true;

            paintWhite = new Paint();
            paintWhite.SetARGB(255, 255, 255, 255);
            paintWhite.SetStyle(Paint.Style.Fill);

            paintGray = new Paint();
            paintGray.SetARGB(255, 99, 166, 233);
            paintGray.SetStyle(Paint.Style.Fill);

            paintBlack = new Paint();
            paintBlack.SetARGB(255, 0, 0, 0);
            paintBlack.SetStyle(Paint.Style.Fill);

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

            return textpaint;
        }

        protected Paint GetRectPaint(PoiViewItem item, bool highLightSelected)
        {
            if (!highLightSelected)
                return paintRectSelectedItem;

            if (item.Selected)
                return paintRectSelectedItem;

            return paintRect;
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

            paintRect.StrokeWidth = ToPixels(4);

            textpaint.TextSize = ToPixels(36);
            textpaintSelected.TextSize = ToPixels(36);

            poiCategoryBitmapProvider.Initialize(resources, new Size((int)ToPixels(66), (int)ToPixels(66)));

            {
                var img = BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.i_info_yellow);
                infoBitmapYellow = Bitmap.CreateScaledBitmap(img, (int)ToPixels(28), (int)ToPixels(28), false);
            }

            {
                var img = BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.i_info_black);
                infoBitmapBlack = Bitmap.CreateScaledBitmap(img, (int)ToPixels(28), (int)ToPixels(28), false);
            }

            {
                var img = BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Android.Resource.Drawable.StarOn);
                favouriteBitmap = Bitmap.CreateScaledBitmap(img, (int)ToPixels(48), (int)ToPixels(48), false); 
            }

        }

        public virtual double GetMinItemAngleDiff(int canvasWidth) { return 0; }
        /// <summary>
        /// Draws background of canvas
        /// </summary>
        /// <param name="canvas"></param>
        public virtual void OnDrawBackground(Android.Graphics.Canvas canvas) { }

        public virtual void OnDrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY, bool displayOverlapped, bool highLightSelected) { }

        public virtual void OnDrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float startX, float endY) { }

        public (float? x,float? y) GetXY(PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, float canvasWidth, float canvasHeight)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.GpsLocation.Bearing, canvasWidth, adjustedViewAngleHorizontal, offsetX);

            if (!startX.HasValue)
                return (null, null);
            
            double verticalAngleCorrection = CompassViewUtils.GetTiltCorrection(item.GpsLocation.Bearing.Value, heading, viewAngleHorizontal, leftTiltCorrector, rightTiltCorrector);
            double verticalAngle = item.VerticalViewAngle;

            float endY = CompassViewUtils.GetYLocationOnScreen(verticalAngle + verticalAngleCorrection, canvasHeight, adjustedViewAngleVertical) + offsetY;
            return (startX, endY);
        }

        public void DrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, bool displayOverlapped, double canvasWidth, bool highLightSelected)
        {
            var (x, y) = GetXY(item, heading, offsetX, offsetY, leftTiltCorrector, rightTiltCorrector, canvas.Width, canvas.Height);
            if (x != null && y != null)
            {
                OnDrawItem(canvas, item, x.Value, y.Value, displayOverlapped, highLightSelected);
            }
        }

        public void DrawItemIcon(Android.Graphics.Canvas canvas, PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, double canvasWidth)
        {
            var (x, y) = GetXY(item, heading, offsetX, offsetY, leftTiltCorrector, rightTiltCorrector, canvas.Width, canvas.Height);
            if (x != null && y != null)
            {
                OnDrawItemIcon(canvas, item, x.Value, y.Value);
            }
        }

        public virtual float GetItemWidth()
        {
            return 0;
        }

        public float GetClickDistance(PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, float canvasWidth, float canvasHeight, float clickX, float clickY)
        {
            var (itemX, itemY) = GetXY(item, heading, offsetX, offsetY, leftTiltCorrector, rightTiltCorrector, canvasWidth, canvasHeight);
            if (itemX.HasValue && itemY.HasValue)
            {
                float xDiff = itemX.Value - clickX;
                float yDiff = itemY.Value - clickY;
                return FloatMath.Sqrt(xDiff * xDiff + yDiff * yDiff);
            }

            return float.MaxValue;
        }

        public bool IsItemClicked(PoiViewItem item, float heading, float offsetX, float offsetY, double leftTiltCorrector, double rightTiltCorrector, float canvasWidth, float canvasHeight, float clickX, float clickY)
        {
            var (itemX, itemY) = GetXY(item, heading, offsetX, offsetY, leftTiltCorrector, rightTiltCorrector, canvasWidth, canvasHeight);
            if (itemX.HasValue && itemY.HasValue)
            { 
                float xDiff = itemX.Value - clickX;
                if (Math.Abs(xDiff) < GetItemWidth() && clickY < itemY)
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