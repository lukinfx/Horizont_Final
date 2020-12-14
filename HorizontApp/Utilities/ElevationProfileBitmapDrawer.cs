using System;
using System.Linq;
using Android.Graphics;
using SkiaSharp;
using HorizontApp.AppContext;
using HorizontLib.Domain.Models;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;
using HorizonLib.Domain.Models;
using System.Collections.Generic;

namespace HorizontApp.Utilities
{
    public class ElevationProfileBitmapDrawer
    {
        private IAppContext _context;
        private Bitmap _elevationProfileBitmap;
        private float _adjustedViewAngleHorizontal;
        private float _adjustedViewAngleVertical;

        private SKPaint _paint = new SKPaint 
        { 
        IsAntialias = true,
        Color = SKColors.Yellow,
        StrokeWidth = 3
        };

        public ElevationProfileBitmapDrawer(IAppContext context)
        {
            _context = context;
        }

        public virtual void Initialize(float adjustedViewAngleHorizontal, float adjustedViewAngleVertical)
        {
            _adjustedViewAngleHorizontal = adjustedViewAngleHorizontal;
            _adjustedViewAngleVertical = adjustedViewAngleVertical;
        }

        public void GenerateElevationProfileBitmap(ElevationProfileData epd, double displayWidth, double displayHeight)
        {
            /*foreach(var ed in epd.GetData())
            {
                var edMaxDist = ed.GetPoints().Max(p => p.Distance.Value);
                if (edMaxDist > maxDist)
                {
                    maxDist = edMaxDist;
                }
            }*/

            List<ProfileLine> listOfLines = new List<ProfileLine>();
            for (ushort i = 0; i < 360; i++)
            {
                var thisAngle = epd.GetData(i);
                var prevAngle = epd.GetData(i - 1);

                if (thisAngle != null && prevAngle != null)
                {
                    foreach (var point in thisAngle.GetPoints())
                    {
                        foreach (var otherPoint in prevAngle.GetPoints())
                        {
                            if (point.Bearing.HasValue && otherPoint.Bearing.HasValue && point.Distance.HasValue && otherPoint.Distance.HasValue && point.VerticalViewAngle.HasValue && otherPoint.VerticalViewAngle.HasValue)
                            {
                                if (Math.Abs(point.Distance.Value - otherPoint.Distance.Value) <= point.Distance.Value / 12)
                                {
                                    var y1 = point.VerticalViewAngle.Value;
                                    var x1 = (float)point.Bearing.Value;

                                    var y2 = otherPoint.VerticalViewAngle.Value;
                                    var x2 = (float)otherPoint.Bearing.Value;
                                    listOfLines.Add(new ProfileLine { Bearing1 = x1, Bearing2 = x2, VerticalViewAngle1 = (float)y1, VerticalViewAngle2 = (float)y2, distance = point.Distance.Value });
                                }
                            }
                        }
                    }
                }
            }
            _context.ListOfProfileLines = listOfLines;
        }

        public void PaintElevationProfileBitmap(Canvas canvas, double heading, double leftTiltCorrector, double rightTiltCorrector)
        {
            if (_context.ListOfProfileLines != null)
            {
                //Paint paint = new Paint();
                Paint paint2 = new Paint();
                float offset = 5;

                foreach (var line in _context.ListOfProfileLines)
                {
                    double alpha;
                    if (line.distance / 1000 > _context.Settings.MaxDistance)
                    {
                        alpha = 0;
                    }
                    else
                    {
                        alpha = 255 - ((line.distance / 1000) / _context.Settings.MaxDistance) / 2 * 400;
                    }
                    //paint.SetARGB((int)alpha, 255, 255, 100 );
                    //paint.StrokeWidth = 5;

                    paint2.SetARGB((int)alpha, 50, 50, 0);
                    paint2.StrokeWidth = 5;
                    var x1 = CompassViewUtils.GetXLocationOnScreen((float)heading, line.Bearing1, canvas.Width, _adjustedViewAngleHorizontal);
                    var x2 = CompassViewUtils.GetXLocationOnScreen((float)heading, line.Bearing2, canvas.Width, _adjustedViewAngleHorizontal);
                    var y1 = CompassViewUtils.GetYLocationOnScreen(line.VerticalViewAngle1, canvas.Height, _adjustedViewAngleVertical);
                    var y2 = CompassViewUtils.GetYLocationOnScreen(line.VerticalViewAngle2, canvas.Height, _adjustedViewAngleVertical);
                    if (x1.HasValue && x2.HasValue)
                    {
                        if (leftTiltCorrector == 0 && rightTiltCorrector == 0)
                        {
                            //canvas.DrawLine(x1.Value, line.y1, x2.Value, line.y2, paint);
                            canvas.DrawLine(x1.Value, y1 - offset, x2.Value, y2 - offset, paint2);
                        }
                        else
                        {
                            y1 = CompassViewUtils.GetYLocationOnScreen(y1, x1.Value, leftTiltCorrector, rightTiltCorrector, canvas.Width, canvas.Height, _adjustedViewAngleVertical);
                            y2 = CompassViewUtils.GetYLocationOnScreen(y2, x2.Value, leftTiltCorrector, rightTiltCorrector, canvas.Width, canvas.Height, _adjustedViewAngleVertical);

                            //canvas.DrawLine(x1.Value, y1, x2.Value, y2, paint);
                            canvas.DrawLine(x1.Value, y1- offset, x2.Value, y2- offset, paint2);
                        }
                       
                    }

                }
            }
            
            /*
            if (_elevationProfileBitmap != null)
            {
                float offset = (float)(_elevationProfileBitmap.Width * (heading - _adjustedViewAngleHorizontal / 2) / 360);
                canvas.DrawBitmap(_elevationProfileBitmap, -offset, (float)0, null);
                if (heading > 360 - _context.Settings.ViewAngleHorizontal)
                {
                    canvas.DrawBitmap(_elevationProfileBitmap, -offset + _elevationProfileBitmap.Width, (float)0, null);
                }
                if (heading < _context.Settings.ViewAngleHorizontal)
                {
                    canvas.DrawBitmap(_elevationProfileBitmap, -offset - _elevationProfileBitmap.Width, (float)0, null);
                }
            }*/
        }

        public void SetScaledViewAngle(float scaledViewAngleHorizontal, float scaledViewAngleVertical)
        {
            _adjustedViewAngleHorizontal = scaledViewAngleHorizontal;
            _adjustedViewAngleVertical = scaledViewAngleVertical;
        }
    }
}