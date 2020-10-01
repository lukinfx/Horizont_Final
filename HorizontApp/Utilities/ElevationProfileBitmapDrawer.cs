using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontLib.Utilities;
using SkiaSharp;

namespace HorizontApp.Utilities
{
    public class ElevationProfileBitmapDrawer
    {
        private SKData profileImageData;

        public ElevationProfileBitmapDrawer()
        {

        }

        public void SetElevationProfile(ElevationProfileData epd)
        {
            var imageInfo = new SKImageInfo(width: 100, height: 100, colorType: SKColorType.Rgba8888, alphaType: SKAlphaType.Premul);
            var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;

            //canvas.Clear(SKColors.White);

            var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.Color = SKColors.Red;
            paint.StrokeWidth = 3;

            canvas.DrawCircle(50, 50, 25, paint);

            using (SKImage image = surface.Snapshot())
            {
                profileImageData = image.Encode();
            }
        }

        public SKData GetElevationBitmap()
        {
            return profileImageData;
        }
    }
}