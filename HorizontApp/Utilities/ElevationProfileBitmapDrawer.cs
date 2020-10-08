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
using HorizontLib.Domain.Models;
using HorizontLib.Utilities;
using SkiaSharp;
using Xamarin.Essentials;

namespace HorizontApp.Utilities
{
    public class ElevationProfileBitmapDrawer
    {
        private SKData profileImageData;
        private SKPaint _paint = new SKPaint 
        { 
        IsAntialias = true,
        Color = SKColors.Yellow,
        StrokeWidth = 3
        };
        

        public ElevationProfileBitmapDrawer()
        {

        }

        public void SetElevationProfile(ElevationProfileData epd, double displayWidth, double displayHeight)
        {
            var viewAngleHorizontal = CompassViewSettings.Instance().ViewAngleHorizontal;
            var viewAngleVertical = CompassViewSettings.Instance().ViewAngleVertical;

            var imageInfo = new SKImageInfo(width: Convert.ToInt32(displayWidth * (360 / viewAngleHorizontal)), height: Convert.ToInt32(displayHeight), colorType: SKColorType.Rgba8888, alphaType: SKAlphaType.Premul);
            var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;
            

            

            //canvas.Clear(SKColors.White);

            if (epd == null)
            {
                canvas.DrawLine(0, imageInfo.Height / (float)2.0, imageInfo.Width, imageInfo.Height / (float)2.0, _paint);
                return;
            }

            var points = epd.GetPoints();

            var maxDist = points.Max(p => p.Distance.Value);

            foreach (var point in points)
            {
                foreach (var otherPoint in points)
                {
                    if (point.Bearing.HasValue && otherPoint.Bearing.HasValue && point.Distance.HasValue && otherPoint.Distance.HasValue && point.VerticalViewAngle.HasValue && otherPoint.VerticalViewAngle.HasValue)
                    {
                        if (Math.Abs(point.Bearing.Value - otherPoint.Bearing.Value) < 2 && Math.Abs(point.Distance.Value - otherPoint.Distance.Value) <= point.Distance.Value / 10)
                        {
                            var y1 = GetYLocation(point.VerticalViewAngle.Value, imageInfo.Height, viewAngleVertical);
                            var x1 = GetXLocation(point.Bearing.Value, imageInfo.Width);

                            var y2 = GetYLocation(otherPoint.VerticalViewAngle.Value, imageInfo.Height, viewAngleVertical);
                            var x2 = GetXLocation(otherPoint.Bearing.Value, imageInfo.Width);

                            
                            _paint.Color = SKColor.FromHsl(60, 100, (float)(50.0 - (point.Distance.Value / maxDist) / 2 * 50));
                            if (Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)) < 100)
                                canvas.DrawLine(x1, y1, x2, y2, _paint);
                            
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
            var YCoord = (canvasHeight / 2) - ((verticalAngle / (viewAngleVertical / 2)) * canvasHeight / 2);
            var YCoordFloat = (float)YCoord;
            return YCoordFloat;
        }

        public SKData GetElevationBitmap()
        {
            return profileImageData;
        }
    }
}