using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Java.Lang;
using Java.Nio;
using Android.Media;
using Android.Graphics;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.Models;
using Peaks360App.AppContext;
using Android.Content;
using Peaks360App.Providers;
using Peaks360Lib.Utilities;

namespace Peaks360App.Utilities
{
    public class ImageSaver : Java.Lang.Object, IRunnable
    {
        private static int THUMBNAIL_WIDTH = 300;
        private static int THUMBNAIL_HEIGHT = 200;
        private static int THUMBNAIL_QUALITY = 80;

        // The JPEG image
        private Image _Image;
        private IAppContext _context;

        // The file we save the image into.
        public ImageSaver(Image image, IAppContext context)
        {
            if (image == null)
                throw new System.ArgumentNullException("image");
            _Image = image;
            _context = context;
        }

        public void Run()
        {

            var imgWidth = _context.IsPortrait ? _Image.Height : _Image.Width;
            var imgHeight = _context.IsPortrait ? _Image.Width : _Image.Height;

            var thumbWidth = _context.IsPortrait ? THUMBNAIL_HEIGHT : THUMBNAIL_WIDTH;
            var thumbHeight = _context.IsPortrait ? THUMBNAIL_WIDTH : THUMBNAIL_HEIGHT;

            ByteBuffer buffer = _Image.GetPlanes()[0].Buffer;
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            var filename = ImageSaverUtils.GetPhotoFileName();
            var filepath = System.IO.Path.Combine(ImageSaverUtils.GetPhotosFileFolder(), filename);

            var file = new Java.IO.File(filepath);
            byte[] thumbnail = ImageResizer.ResizeImageAndroid(bytes, thumbWidth, thumbHeight, THUMBNAIL_QUALITY);

            using (var output = new Java.IO.FileOutputStream(file))
            {
                try
                {
                    output.Write(bytes);
                    
                    PoiDatabase poiDatabase = new PoiDatabase();
                    string jsonCategories = JsonConvert.SerializeObject(_context.Settings.Categories);

                    var tag = _context.MyLocationPlaceInfo.PlaceName + " -> ";
                    if (_context.SelectedPoi != null)
                    {
                        tag += _context.SelectedPoi.Poi.Name;
                    }
                    else
                    {
                        tag += Android.App.Application.Context.Resources.GetText(Resource.String.Common_Heading);
                        tag += _context.HeadingX.HasValue ? $" {GpsUtils.Normalize360((_context.HeadingX.Value)):F0}°" : "?°";
                    }

                    PhotoData photodata = new PhotoData
                    {
                        Tag = tag,
                        Datetime = DateTime.Now,
                        DatetimeTaken = DateTime.Now,
                        PhotoFileName = filename,
                        Longitude = _context.MyLocation.Longitude,
                        Latitude = _context.MyLocation.Latitude,
                        Altitude = _context.MyLocation.Altitude,
                        Heading = (_context.HeadingX ?? 0) + _context.HeadingCorrector,
                        LeftTiltCorrector = _context.LeftTiltCorrector,
                        RightTiltCorrector = _context.RightTiltCorrector,
                        Thumbnail = thumbnail,
                        JsonCategories = jsonCategories,
                        ViewAngleVertical = _context.ViewAngleVertical,
                        ViewAngleHorizontal = _context.ViewAngleHorizontal,
                        PictureWidth = imgWidth,
                        PictureHeight = imgHeight,
                        MinAltitude = _context.Settings.MinAltitute,
                        MaxDistance = _context.Settings.MaxDistance,
                        FavouriteFilter = _context.ShowFavoritesOnly,
                        ShowElevationProfile = _context.Settings.ShowElevationProfile
                    };
                    if (_context.ElevationProfileData != null)
                    {
                        photodata.JsonElevationProfileData = _context.ElevationProfileData.Serialize();
                    }
                    poiDatabase.InsertItem(photodata);

                }
                catch (Java.IO.IOException e)
                {
                    e.PrintStackTrace();
                }
                finally
                {
                    _Image.Close();
                }
            }
        }


        public static PhotoData SaveCopy(Bitmap dstBmp, Rect cropRect, PhotoData photodata, int maxDistance, IAppContext context)
        {
            var croppedBitmap = Bitmap.CreateBitmap(dstBmp, cropRect.Left, cropRect.Top, cropRect.Width(), cropRect.Height());

            var viewAngleVertical = context.ViewAngleVertical * croppedBitmap.Height / (double)dstBmp.Height;
            var viewAngleHorizontal = context.ViewAngleHorizontal * croppedBitmap.Width / (double)dstBmp.Width;

            //center new, minu center old
            var hdgCorrectionInPixels = ((cropRect.Left + cropRect.Right) / 2.0) - (dstBmp.Width / 2.0);
            //and now to degrees
            var hdgCorrectionInDegrees = hdgCorrectionInPixels / (float)dstBmp.Width * context.ViewAngleHorizontal;

            //Linear function - calculation of new corrector values
            double leftCorrectionInDegrees = context.LeftTiltCorrector + (context.RightTiltCorrector - context.LeftTiltCorrector) * (cropRect.Left / (double)dstBmp.Width);
            double rightCorrectionInDegrees = context.LeftTiltCorrector + (context.RightTiltCorrector - context.LeftTiltCorrector) * (cropRect.Right / (double)dstBmp.Width);

            //New center of image can be somewhere else, so we need to reflect this in total view angle correction
            double totalCorrectionInPixels = (((cropRect.Top + cropRect.Bottom) / 2.0) - (dstBmp.Height / 2.0));
            double totalCorrectionInDegrees = (totalCorrectionInPixels / dstBmp.Height) * context.ViewAngleVertical;

            var now = DateTime.Now;

            PhotoData newPhotodata = new PhotoData
            {
                Tag = "Copy of " + photodata.Tag,
                Datetime = DateTime.Now,
                DatetimeTaken = photodata.DatetimeTaken,
                PhotoFileName = ImageSaverUtils.GetPhotoFileName(now),
                Longitude = photodata.Longitude,
                Latitude = photodata.Latitude,
                Altitude = photodata.Altitude,
                Heading = context.HeadingX.HasValue? context.HeadingX.Value + context.HeadingCorrector + hdgCorrectionInDegrees : (double?)null,
                JsonCategories = JsonConvert.SerializeObject(context.Settings.Categories),
                ViewAngleVertical = viewAngleVertical,
                ViewAngleHorizontal = viewAngleHorizontal,
                LeftTiltCorrector = leftCorrectionInDegrees + totalCorrectionInDegrees,
                RightTiltCorrector = rightCorrectionInDegrees + totalCorrectionInDegrees,
                PictureWidth = croppedBitmap.Width,
                PictureHeight = croppedBitmap.Height,
                MinAltitude = 0,
                MaxDistance = maxDistance,
                FavouriteFilter = context.ShowFavoritesOnly,
                ShowElevationProfile = context.Settings.ShowElevationProfile
            };

            var thumbnainBitmap = Bitmap.CreateScaledBitmap(croppedBitmap, THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT, false);
            using (MemoryStream ms = new MemoryStream())
            {
                thumbnainBitmap.Compress(Bitmap.CompressFormat.Jpeg, THUMBNAIL_QUALITY, ms);
                newPhotodata.Thumbnail = ms.ToArray();
            }

            if (context.ElevationProfileData != null)
            {
                newPhotodata.JsonElevationProfileData = context.ElevationProfileData.Serialize();
            }

            var filePath = System.IO.Path.Combine(ImageSaverUtils.GetPhotosFileFolder(), newPhotodata.PhotoFileName);
            var stream = new FileStream(filePath, FileMode.Create);
            croppedBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

            return newPhotodata;
        }

        public static async Task<PhotoData> Import(string path, ExifData exifData, IAppContext appContext)
        {
            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                byte[] bytes;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    bytes = br.ReadBytes((int)fs.Length);
                }

                var bmp = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);

                var imgWidth = bmp.Width;
                var imgHeight = bmp.Height;
                var thumbWidth = THUMBNAIL_WIDTH;
                var thumbHeight = THUMBNAIL_HEIGHT;

                var filename = ImageSaverUtils.GetPhotoFileName();
                var filepath = System.IO.Path.Combine(ImageSaverUtils.GetPhotosFileFolder(), filename);

                var file = new Java.IO.File(filepath);
                byte[] thumbnail = ImageResizer.ResizeImageAndroid(bytes, thumbWidth, thumbHeight, THUMBNAIL_QUALITY);

                using (var output = new Java.IO.FileOutputStream(file))
                {
                    output.Write(bytes);
                }

                PoiDatabase poiDatabase = new PoiDatabase();
                string jsonCategories = JsonConvert.SerializeObject(appContext.Settings.Categories);

                PhotoData photodata = new PhotoData
                {
                    Datetime = DateTime.Now,
                    DatetimeTaken = exifData.timeTaken ?? DateTime.Now,
                    PhotoFileName = filename,
                    Longitude = exifData.location?.Longitude ?? 0,
                    Latitude = exifData.location?.Latitude ?? 0,
                    Altitude = exifData.location?.Altitude?? 0,
                    Heading = exifData.heading,
                    LeftTiltCorrector = 0,
                    RightTiltCorrector = 0,
                    Thumbnail = thumbnail,
                    JsonCategories = jsonCategories,
                    PictureWidth = imgWidth,
                    PictureHeight = imgHeight,
                    MinAltitude = appContext.Settings.MinAltitute,
                    MaxDistance = appContext.Settings.MaxDistance,
                    FavouriteFilter = appContext.ShowFavoritesOnly,
                    ShowElevationProfile = appContext.Settings.ShowElevationProfile
                };

                //calculate view angle from focal length equivalent on 35mm camera, or use default viev angle 60dg
                var viewAngle = exifData.focalLength35mm.HasValue ? 2 * System.Math.Tan(35d / 2d / (double)exifData.focalLength35mm.Value) / System.Math.PI * 180 : 60;
                if (imgWidth > imgHeight)
                {
                    photodata.ViewAngleHorizontal = viewAngle;
                    photodata.ViewAngleVertical = viewAngle / imgWidth * imgHeight;
                }
                else
                {
                    photodata.ViewAngleHorizontal = viewAngle / imgHeight * imgWidth;
                    photodata.ViewAngleVertical = viewAngle;
                }

                if (GpsUtils.HasLocation(exifData.location))
                {
                    var placeInfo = await PlaceNameProvider.AsyncGetPlaceName(exifData.location);
                    photodata.Tag = placeInfo.PlaceName + " -> ?";
                }
                else
                {
                    photodata.Tag = "? -> ?";
                }

                appContext.PhotosModel.InsertItem(photodata);
                return photodata;
            }
        }
    }
}