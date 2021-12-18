using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Graphics;
using Android.Util;

namespace Peaks360App.Views
{
    public class DistanceSeekBar : SeekBar
    {
        private Paint _paintSeekbarHandle;
        private Paint _paintGridLines;
        private Paint _paintCurrentLine;
        private Paint _paintText;
        private Bitmap _seekbarHandleBitmap;

        public DistanceSeekBar(Context context) : base(context)
        {
        }

        public DistanceSeekBar(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Typeface normal = Typeface.Create("Arial", TypefaceStyle.Normal);

            _paintText = new Paint();
            _paintText.SetARGB(255, 0, 0, 0);
            _paintText.TextSize = 32;
            _paintText.TextAlign = Paint.Align.Center;
            _paintText.AntiAlias = true;
            _paintText.SetTypeface(normal);

            _paintGridLines = new Paint();
            _paintGridLines.SetARGB(150, 50, 50, 50);
            _paintGridLines.SetStyle(Paint.Style.Stroke);
            _paintGridLines.StrokeWidth = 4;

            _paintCurrentLine = new Paint();
            _paintCurrentLine.SetStyle(Paint.Style.Stroke);
            _paintCurrentLine.StrokeWidth = 6;

            _paintSeekbarHandle = new Paint();

            _seekbarHandleBitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.seekbar_handle);
        }

        public DistanceSeekBar(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }

        protected override void OnDraw(Canvas canvas)
        {
            var xStart = Left + PaddingLeft;
            var xEnd = Right - PaddingRight;
            var xTotal = xEnd - xStart;
            var xCurrent = xStart + (xTotal / (float)Max * Progress);

            var yTotal = Height - PaddingTop - PaddingBottom;
            var yMiddle = this.Height / 2f;

            _paintCurrentLine.SetARGB(255, 01, 43, 101); 
            canvas.DrawLine(xStart, yMiddle, xCurrent, yMiddle, _paintCurrentLine);
            _paintCurrentLine.SetARGB(120, 70, 70, 70);
            canvas.DrawLine(xCurrent, yMiddle, xEnd, yMiddle, _paintCurrentLine);

            int step = xTotal > 1000 ? 2 : 5;
            for (int i = 0; i <= this.Max; i += step)
            {
                var x = xStart + (xTotal / (float) Max * i);
                
                var majorGrid = (i % 10 == 0);

                if (majorGrid)
                {
                    canvas.DrawLine(x, yMiddle - (yTotal * 0.1f), x, yMiddle - (yTotal * 0.4f), _paintGridLines);
                    canvas.DrawText(i.ToString(), x, this.Bottom - 25, _paintText);
                }
                else
                {
                    canvas.DrawLine(x, yMiddle - (yTotal * 0.1f), x, yMiddle - (yTotal * 0.2f), _paintGridLines);
                }
            }

            

            canvas.DrawBitmap(_seekbarHandleBitmap, 
                new Rect(0,0,_seekbarHandleBitmap.Width, _seekbarHandleBitmap.Height),
                new RectF(xCurrent - 35, yTotal * 0.1f, xCurrent + 35, yMiddle),
                null);

            //base.OnDraw(canvas);
        }
    }
}