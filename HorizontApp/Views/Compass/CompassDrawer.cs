using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.ViewModel;
using HorizontApp.Utilities;
using HorizontLib.Utilities;

namespace HorizontApp.Views.Compass
{
    public class CompassViewDrawer
    {
        protected Android.Graphics.Paint paint;
        protected Android.Graphics.Paint paintRect;
        protected Android.Graphics.Paint textpaint;

        public float ViewAngleHorizontal { protected get; set; }
        public float ViewAngleVertical { protected get; set; }

        public virtual void Initialize()
        {
            paint = new Android.Graphics.Paint();
            paint.SetARGB(255, 200, 255, 0);
            paint.SetStyle(Paint.Style.FillAndStroke);
            paint.StrokeWidth = 4;

            paintRect = new Android.Graphics.Paint();
            paintRect.SetARGB(150, 0, 0, 0);
            paintRect.SetStyle(Paint.Style.FillAndStroke);
            paintRect.StrokeWidth = 4;

            textpaint = new Android.Graphics.Paint();
            textpaint.SetARGB(255, 200, 255, 0);
            textpaint.TextSize = 36;
            Typeface normal = Typeface.Create("Arial", TypefaceStyle.Normal);
            textpaint.SetTypeface(normal);
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
        public virtual void OnDrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float heading) { }

        public void PaintProfile(Android.Graphics.Canvas canvas, float heading, ElevationProfileData epd)
        {
            var points = epd.GetPoints();
            foreach (var point in points)
            {
                foreach (var otherPoint in points)
                {
                    if (point.Bearing.HasValue && otherPoint.Bearing.HasValue && point.Distance.HasValue && otherPoint.Distance.HasValue && point.VerticalViewAngle.HasValue && otherPoint.VerticalViewAngle.HasValue)
                    {
                        if (Math.Abs(point.Bearing.Value - otherPoint.Bearing.Value) < 2 && Math.Abs(point.Distance.Value - otherPoint.Distance.Value) <= point.Distance.Value / 10)
                        {
                            var y1 = CompassViewUtils.GetYLocationOnScreen(point.VerticalViewAngle.Value, canvas.Height, ViewAngleVertical);
                            var x1 = CompassViewUtils.GetXLocationOnScreen(heading, (float)point.Bearing.Value, canvas.Width, ViewAngleHorizontal);

                            var y2 = CompassViewUtils.GetYLocationOnScreen(otherPoint.VerticalViewAngle.Value, canvas.Height, ViewAngleVertical);
                            var x2 = CompassViewUtils.GetXLocationOnScreen(heading, (float)otherPoint.Bearing.Value, canvas.Width, ViewAngleHorizontal);
                            if (x1.HasValue && x2.HasValue)
                            {
                                //if (Math.Sqrt(Math.Pow(x1.Value - x2.Value, 2) + Math.Pow(y1 - y2, 2)) < 100)
                                canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, paint);
                            }

                        }
                    }
                }
            }

            /*for (int i = Heading - viewAngleVertical/2; i < Heading + 35; i++)
            {
                var dg = (i + 360) % 360;
                double x = (i - _heading + 35) * DG_WIDTH;

                //e.Graphics.DrawLine(new Pen(Brushes.Blue), (float)x, 250, (float)x, (float)(250-y));
                if (i % 10 == 0)
                {
                    e.Graphics.DrawString(i.ToString(), new Font("Arial", 10), new SolidBrush(Color.Black), (float)x, 10);
                }
            }*/
        }
    }
}