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
        protected Paint paintGreenLine;
        protected Paint paintText;

        public DistanceSeekBar(Context context) : base(context)
        {
        }

        public DistanceSeekBar(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Typeface normal = Typeface.Create("Arial", TypefaceStyle.Normal);

            paintText = new Paint();
            paintText.SetARGB(255, 200, 255, 0);
            paintText.TextSize = 32;
            paintText.TextAlign = Paint.Align.Center;
            paintText.AntiAlias = true;
            paintText.SetTypeface(normal);

            paintGreenLine = new Paint();
            paintGreenLine.SetARGB(150, 200, 255, 0);
            paintGreenLine.SetStyle(Paint.Style.Stroke);
            paintGreenLine.StrokeWidth = 4;
        }

        public DistanceSeekBar(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }

        protected override void OnDraw(Canvas canvas)
        {
            var xStart = Left + PaddingLeft;
            var xEnd = Right - PaddingRight;
            var xTotal = xEnd - xStart;

            var yTotal = Height - PaddingTop - PaddingBottom;
            var yMiddle = this.Height / 2f;

            int step = xTotal > 1000 ? 2 : 5;
            for (int i = 0; i <= this.Max; i += step)
            {
                var x = xStart + (xTotal / (float) Max * i);
                
                var majorGrid = (i % 10 == 0);

                if (majorGrid)
                {
                    canvas.DrawLine(x, yMiddle - (yTotal * 0.1f), x, yMiddle - (yTotal * 0.4f), paintGreenLine);
                    canvas.DrawText(i.ToString(), x, this.Bottom - 15, paintText);
                }
                else
                {
                    canvas.DrawLine(x, yMiddle - (yTotal * 0.1f), x, yMiddle - (yTotal * 0.2f), paintGreenLine);
                }
            }

            base.OnDraw(canvas);
        }
    }
}