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
                                    var y1 = GetYLocation(point.VerticalViewAngle.Value, displayHeight, _adjustedViewAngleVertical);
                                    var x1 = (float)point.Bearing.Value;

                                    var y2 = GetYLocation(otherPoint.VerticalViewAngle.Value, displayHeight, _adjustedViewAngleVertical);
                                    var x2 = (float)otherPoint.Bearing.Value;
                                    listOfLines.Add(new ProfileLine { x1 = x1, x2 = x2, y1 = y1, y2 = y2, distance = point.Distance.Value });
                                }
                            }
                        }
                    }
                }
            }
            _context.ListOfProfileLines = listOfLines;
        }

        private float GetXLocation(double bearing, double canvasWidth)
        {
            var XCoord = (GpsLocation.Normalize360(bearing) / 360) * canvasWidth;
            return (float)XCoord;
        }

        private float GetYLocation(double verticalAngle, double canvasHeight, float viewAngleVertical)
        {
            return CompassViewUtils.GetYLocationOnScreen(verticalAngle, canvasHeight, viewAngleVertical);
        }

        public void PaintElevationProfileBitmap(Canvas canvas, double heading, double leftTiltCorrector, double rightTiltCorrector)
        {
            if (_context.ListOfProfileLines != null)
            {
                Paint paint = new Paint();

                foreach (var line in _context.ListOfProfileLines)
                {
                    paint.SetARGB((int)(255 - ((line.distance / 1000) / _context.Settings.MaxDistance) / 2 * 400), 255, 255, 100 );
                    paint.StrokeWidth = 3; 
                    var x1 = CompassViewUtils.GetXLocationOnScreen((float)heading, line.x1, canvas.Width, _adjustedViewAngleHorizontal);
                    var x2 = CompassViewUtils.GetXLocationOnScreen((float)heading, line.x2, canvas.Width, _adjustedViewAngleHorizontal);
                    if (x1.HasValue && x2.HasValue)
                    {
                        if (leftTiltCorrector == 0 && rightTiltCorrector == 0)
                        {
                            canvas.DrawLine(x1.Value, line.y1, x2.Value, line.y2, paint);
                        }
                        else
                        {
                            var y1 = CompassViewUtils.GetYLocationOnScreen(line.y1, x1.Value, leftTiltCorrector, rightTiltCorrector, canvas.Width);
                            var y2 = CompassViewUtils.GetYLocationOnScreen(line.y2, x2.Value, leftTiltCorrector, rightTiltCorrector, canvas.Width);

                            canvas.DrawLine(x1.Value, y1, x2.Value, y2, paint);
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
    }
}