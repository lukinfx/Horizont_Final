using Android.Graphics;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace HorizontApp.Utilities
{
    public class ImageResizer
    {
        public static byte[] ResizeImageAndroid(byte[] imageData, float width, float height, int quality)
        {
            // Load the bitmap
            Android.Graphics.Bitmap originalImage = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);

            float oldWidth = (float)originalImage.Width;
            float oldHeight = (float)originalImage.Height;
            float scaleFactor = 0;

            if (oldWidth > oldHeight)
            {
                scaleFactor = width / oldWidth;
            }
            else
            {
                scaleFactor = height / oldHeight;
            }

            float newHeight = oldHeight * scaleFactor;
            float newWidth = oldWidth * scaleFactor;

            Android.Graphics.Bitmap resizedImage = Android.Graphics.Bitmap.CreateScaledBitmap(originalImage, (int)newWidth, (int)newHeight, false);
            

            using (MemoryStream ms = new MemoryStream())
            {
                resizedImage.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, quality, ms);
                return ms.ToArray();
            }
        }


        public static Image BitmapToThumbnail(System.Drawing.Bitmap bitmap)
        {
            using (Stream BitmapStream = System.IO.File.Open("", System.IO.FileMode.Open))
            {
                Image img = Image.FromStream(BitmapStream);

                var bmp = new System.Drawing.Bitmap(img);
                var thumbnail = bmp.GetThumbnailImage(100, 100, null, IntPtr.Zero);
                return thumbnail;
            }
            
        }

            public static byte[] BitmapToByteArray(System.Drawing.Bitmap bitmap)
        {
            using (Stream BitmapStream = System.IO.File.Open("", System.IO.FileMode.Open))
            {
                Image img = Image.FromStream(BitmapStream);

                var bmp = new System.Drawing.Bitmap(img);
                //var thumbnail = bmp.GetThumbnailImage(100, 100, null);

            }

            BitmapData bmpdata = null;

            try
            {
                bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                int numbytes = bmpdata.Stride * bitmap.Height;
                byte[] bytedata = new byte[numbytes];
                IntPtr ptr = bmpdata.Scan0;

                Marshal.Copy(ptr, bytedata, 0, numbytes);

                return bytedata;
            }
            finally
            {
                if (bmpdata != null)
                    bitmap.UnlockBits(bmpdata);
            }

        }
    }
}