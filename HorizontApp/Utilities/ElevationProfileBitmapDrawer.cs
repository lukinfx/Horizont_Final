using System;
using System.Linq;
using SkiaSharp;
using HorizontApp.AppContext;
using HorizontLib.Domain.Models;
using HorizontLib.Domain.ViewModel;
using HorizontLib.Utilities;

namespace HorizontApp.Utilities
{
    public class ElevationProfileBitmapDrawer
    {
        private IAppContext _context;
        private SKData profileImageData;
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

        public void SetElevationProfile(ElevationProfileData epd, double displayWidth, double displayHeight)
        {
            var imageInfo = new SKImageInfo(width: Convert.ToInt32(displayWidth * (360 / _adjustedViewAngleHorizontal)), height: Convert.ToInt32(displayHeight), colorType: SKColorType.Rgba8888, alphaType: SKAlphaType.Premul);
            var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;
            

            

            //canvas.Clear(SKColors.White);

            if (epd == null)
            {
                canvas.DrawLine(0, imageInfo.Height / (float)2.0, imageInfo.Width, imageInfo.Height / (float)2.0, _paint);
                return;
            }


            double maxDist = 0;
            foreach(var ed in epd.GetData())
            {
                var edMaxDist = ed.GetPoints().Max(p => p.Distance.Value);
                if (edMaxDist > maxDist)
                {
                    maxDist = edMaxDist;
                }
            }

            var data = epd.GetData();
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
                                    var y1 = GetYLocation(point.VerticalViewAngle.Value, imageInfo.Height, _adjustedViewAngleVertical);
                                    var x1 = GetXLocation(point.Bearing.Value, imageInfo.Width);

                                    var y2 = GetYLocation(otherPoint.VerticalViewAngle.Value, imageInfo.Height, _adjustedViewAngleVertical);
                                    var x2 = GetXLocation(otherPoint.Bearing.Value, imageInfo.Width);


                                    _paint.Color = SKColor.FromHsl(60, 100, (float) (50.0 - (point.Distance.Value / maxDist) / 2 * 50));
                                    if (Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)) < 100)
                                        canvas.DrawLine(x1, y1, x2, y2, _paint);

                                }
                            }
                        }
                    }
                }
            }

            canvas.DrawCircle(50, 50, 25, _paint);

            using (SKImage image = surface.Snapshot())
            {
                profileImageData = image.Encode();
            }
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

        public SKData GetElevationBitmap()
        {
            return profileImageData;
        }
    }
}