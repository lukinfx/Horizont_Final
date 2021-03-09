using System;
using System.IO;
using Android.Graphics;
using Java.Lang;
using Java.Nio;
using Android.Media;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.Models;
using Peaks360App.AppContext;
using Newtonsoft.Json;

namespace Peaks360App.Utilities
{
    public class ImageCopySaver
    {
        public static PhotoData Save(Bitmap dstBmp, Rect cropRect, PhotoData photodata, double leftTiltCorrector ,double rightTiltCorrector, int MinHeight, int MaxDistance, IAppContext Context)
        {
            var croppedBitmap = Bitmap.CreateBitmap(dstBmp, cropRect.Left, cropRect.Top, cropRect.Width(), cropRect.Height());

            var viewAngleVertical = Context.ViewAngleVertical * croppedBitmap.Height / (double)dstBmp.Height;
            var viewAngleHorizontal = Context.ViewAngleHorizontal * croppedBitmap.Width / (double)dstBmp.Width;

            //center new, minu center old
            var hdgCorrectionInPixels = ((cropRect.Left + cropRect.Right) / 2.0) - (dstBmp.Width / 2.0);
            //and now to degrees
            var hdgCorrectionInDegrees = hdgCorrectionInPixels / (float)dstBmp.Width * Context.ViewAngleHorizontal;

            //Linear function - calculation of new corrector values
            double leftCorrectionInDegrees = leftTiltCorrector + (rightTiltCorrector - leftTiltCorrector) * (cropRect.Left / (double)dstBmp.Width);
            double rightCorrectionInDegrees = leftTiltCorrector + (rightTiltCorrector - leftTiltCorrector) * (cropRect.Right / (double)dstBmp.Width);

            //New center of image can be somewhere else, so we need to reflect this in total view angle correction
            double totalCorrectionInPixels = (((cropRect.Top + cropRect.Bottom) / 2.0) - (dstBmp.Height / 2.0));
            double totalCorrectionInDegrees = (totalCorrectionInPixels / dstBmp.Height) * Context.ViewAngleVertical;

            var now = DateTime.Now;

            PhotoData newPhotodata = new PhotoData
            {
                Tag = "Copy of " + photodata.Tag,
                Datetime = now,
                PhotoFileName = ImageSaverUtils.GetPhotoFileName(now),
                Longitude = photodata.Longitude,
                Latitude = photodata.Latitude,
                Altitude = photodata.Altitude,
                Heading = Context.Heading + Context.HeadingCorrector + hdgCorrectionInDegrees,
                JsonCategories = JsonConvert.SerializeObject(Context.Settings.Categories),
                ViewAngleVertical = viewAngleVertical,
                ViewAngleHorizontal = viewAngleHorizontal,
                LeftTiltCorrector = leftCorrectionInDegrees + totalCorrectionInDegrees,
                RightTiltCorrector = rightCorrectionInDegrees + totalCorrectionInDegrees,
                PictureWidth = croppedBitmap.Width,
                PictureHeight = croppedBitmap.Height,
                MinAltitude = MinHeight,
                MaxDistance = MaxDistance,
                FavouriteFilter = Context.ShowFavoritesOnly,
                ShowElevationProfile = Context.Settings.ShowElevationProfile
            };

            var thumbnainBitmap = Bitmap.CreateScaledBitmap(croppedBitmap, 150, 100, false);
            using (MemoryStream ms = new MemoryStream())
            {
                thumbnainBitmap.Compress(Bitmap.CompressFormat.Jpeg, 70, ms);
                newPhotodata.Thumbnail = ms.ToArray();
            }

            if (Context.ElevationProfileData != null)
            {
                newPhotodata.JsonElevationProfileData = Context.ElevationProfileData.Serialize();
            }

            var filePath = System.IO.Path.Combine(ImageSaverUtils.GetPhotosFileFolder(), newPhotodata.PhotoFileName);
            var stream = new FileStream(filePath, FileMode.Create);
            croppedBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

            return newPhotodata;
        }
    }
}
