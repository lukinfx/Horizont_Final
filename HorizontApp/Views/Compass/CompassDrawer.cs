using Android.Graphics;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawer
    {
        protected Paint paint;
        protected Paint paintRect;
        protected Paint textpaint;

        protected float adjustedViewAngleHorizontal;
        protected float adjustedViewAngleVertical;

        public CompassViewDrawer()
        {
            paint = new Paint();
            paint.SetARGB(255, 200, 255, 0);
            paint.SetStyle(Paint.Style.FillAndStroke);
            paint.StrokeWidth = 4;

            paintRect = new Paint();
            paintRect.SetARGB(150, 0, 0, 0);
            paintRect.SetStyle(Paint.Style.FillAndStroke);
            paintRect.StrokeWidth = 4;

            textpaint = new Paint();
            textpaint.SetARGB(255, 200, 255, 0);
            textpaint.TextSize = 36;
            Typeface normal = Typeface.Create("Arial", TypefaceStyle.Normal);
            textpaint.SetTypeface(normal);
        }

        public virtual void Initialize(float viewAngleHorizontal, float viewAngleVertical)
        {
            adjustedViewAngleHorizontal = viewAngleHorizontal;
            adjustedViewAngleVertical = viewAngleVertical;
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

        public virtual void OnDrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float heading, double leftTiltCorrector, double rightTiltCorrector, double canvasWidth) 
        { 

        }

        public void DrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float heading, double leftTiltCorrector, double rightTiltCorrector, double canvasWidth)
        {
            var startX = CompassViewUtils.GetXLocationOnScreen(heading, (float)item.Bearing, canvas.Width, adjustedViewAngleHorizontal);

            if (startX != null)
            {
                var endY = CompassViewUtils.GetYLocationOnScreen(item.Distance, item.AltitudeDifference, canvas.Height, adjustedViewAngleVertical, startX.Value, leftTiltCorrector, rightTiltCorrector, canvasWidth);

                OnDrawItem(canvas, item, startX.Value, endY);
            }
        }
    }
}