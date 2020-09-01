﻿using System;
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

        public virtual double GetMinItemRightAngleDiff(int canvasWidth) { return 0; }
        public virtual double GetMinItemLeftAngleDiff(int canvasWidth) { return 0; }
        public virtual void OnDrawBackground(Android.Graphics.Canvas canvas) { }
        public virtual void OnDrawItem(Android.Graphics.Canvas canvas, PoiViewItem item, float heading) { }
    }
}