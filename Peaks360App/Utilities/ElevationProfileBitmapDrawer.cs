using System;
using System.Linq;
using Android.Graphics;
using Peaks360App.AppContext;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using System.Collections.Generic;

namespace Peaks360App.Utilities
{
    public class ElevationProfileBitmapDrawer
    {
        private static int LINE_WIDTH = 4;
        private static int LINE_BACK_WIDTH = 6; 

        private IAppContext _context;
        private Bitmap _elevationProfileBitmap;
        private float _viewAngleHorizontal;
        private float _viewAngleVertical;
        private float _adjustedViewAngleHorizontal;
        private float _adjustedViewAngleVertical;
        private Paint _linePaint = new Paint();
        private Paint _lineBackPaint = new Paint();

        public ElevationProfileBitmapDrawer(IAppContext context)
        {
            _context = context;
        }

        public virtual void Initialize(float viewAngleHorizontal, float viewAngleVertical, float multiplier)
        {
            _adjustedViewAngleHorizontal = _viewAngleHorizontal = viewAngleHorizontal;
            _adjustedViewAngleVertical = _viewAngleVertical = viewAngleVertical;
            
            _linePaint.StrokeWidth = LINE_WIDTH * multiplier;
            _lineBackPaint.StrokeWidth = LINE_BACK_WIDTH * multiplier;
        }

        public void GenerateElevationProfileLines(ElevationProfileData epd, double displayWidth, double displayHeight)
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
                        var otherPoint = prevAngle.GetPoints()
                            .Where(x => Math.Abs(point.Distance.Value - x.Distance.Value) <= point.Distance.Value / 12)
                            .OrderBy(x => Math.Abs(point.Distance.Value - x.Distance.Value))
                            .FirstOrDefault();

                        if (otherPoint != null)
                        {
                            var y1 = point.VerticalViewAngle.Value;
                            var x1 = (float) point.Bearing.Value;

                            var y2 = otherPoint.VerticalViewAngle.Value;
                            var x2 = (float) otherPoint.Bearing.Value;
                            listOfLines.Add(new ProfileLine {Bearing1 = x1, Bearing2 = x2, VerticalViewAngle1 = (float) y1, VerticalViewAngle2 = (float) y2, distance = point.Distance.Value});
                        }
                    }
                }
            }
            _context.ListOfProfileLines = listOfLines;
        }

        public void PaintElevationProfileLines(Canvas canvas, double heading, double leftTiltCorrector, double rightTiltCorrector, float offsetX, float offsetY)
        {
            if (_context.ListOfProfileLines != null)
            {
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
                    
                    _linePaint.SetARGB((int)alpha, 0x30, 0x30, 0x30);
                    _linePaint.AntiAlias = true;
                    _lineBackPaint.SetARGB((int)alpha, 0xE0, 0xE0, 0xE0); 
                    _lineBackPaint.AntiAlias = true;

                    var x1 = CompassViewUtils.GetXLocationOnScreen((float)heading, line.Bearing1, canvas.Width, _adjustedViewAngleHorizontal, offsetX);
                    var x2 = CompassViewUtils.GetXLocationOnScreen((float)heading, line.Bearing2, canvas.Width, _adjustedViewAngleHorizontal, offsetX);
                    if (x1.HasValue && x2.HasValue)
                    {
                        double verticalAngleCorrection1 = CompassViewUtils.GetTiltCorrection(line.Bearing1, heading, _viewAngleHorizontal, leftTiltCorrector, rightTiltCorrector);
                        double verticalAngleCorrection2 = CompassViewUtils.GetTiltCorrection(line.Bearing2, heading, _viewAngleHorizontal, leftTiltCorrector, rightTiltCorrector);

                        var y1 = CompassViewUtils.GetYLocationOnScreen(line.VerticalViewAngle1 + verticalAngleCorrection1, canvas.Height, _adjustedViewAngleVertical);
                        var y2 = CompassViewUtils.GetYLocationOnScreen(line.VerticalViewAngle2 + verticalAngleCorrection2, canvas.Height, _adjustedViewAngleVertical);

                        //canvas.DrawLine(x1.Value, line.y1, x2.Value, line.y2, paint);
                        canvas.DrawLine(x1.Value, y1 + offsetY, x2.Value, y2 + offsetY, _lineBackPaint);
                        canvas.DrawLine(x1.Value, y1 + offsetY, x2.Value, y2 + offsetY, _linePaint);
                    }
                }
            }
            
            /*
            if (_elevationProfileBitmap != null)
            {
                float offset = (float)(_elevationProfileBitmap.Width * (heading - ViewAngleHorizontal / 2) / 360);
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