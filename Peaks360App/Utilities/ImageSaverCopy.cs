using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Newtonsoft.Json;
using Java.Lang;
using Java.Nio;
using Android.Media;
using Android.Graphics;
using Android.Provider;
using ExifLib;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.Models;
using Peaks360App.AppContext;
using Peaks360App.Providers;

namespace Peaks360App.Utilities
{
    public class ImageSaverCopy : Java.Lang.Object, IRunnable
    {
        private static int THUMBNAIL_WIDTH = 300;
        private static int THUMBNAIL_HEIGHT = 200;
        private static int THUMBNAIL_QUALITY = 80;

        // The JPEG image
        private Android.Net.Uri _uri;
        private IAppContext _appContext;
        private Context _context;

        private class ExifData
        {
            public GpsLocation location { get; set; }
            public DateTime? timeTaken { get; set; }
            public double? bearing { get; set; }
        }

        // The file we save the image into.
        public ImageSaverCopy(Context context, Android.Net.Uri uri, IAppContext appContext)
        {
            if (uri == null)
                throw new System.ArgumentNullException("uri");
            _uri = uri;
            _context = context;
            _appContext = appContext;
        }

        private ExifData ReadExifData(string path)
        {
            var exifData = new ExifData();
            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                var exifReader = new ExifReader(fs);
                DateTime datePictureTaken;
                if (exifReader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized, out datePictureTaken))
                {
                    exifData.timeTaken = datePictureTaken;
                }

                double exifBearing;
                if (exifReader.GetTagValue<double>(ExifTags.GPSImgDirection, out exifBearing))
                {
                    exifData.bearing = exifBearing;
                }

                double[] exifGpsLongArray;
                double[] exifGpsLatArray;
                if (exifReader.GetTagValue<double[]>(ExifTags.GPSLongitude, out exifGpsLongArray)
                    && exifReader.GetTagValue<double[]>(ExifTags.GPSLatitude, out exifGpsLatArray))
                {
                    double exifGpsLongDouble = exifGpsLongArray[0] + exifGpsLongArray[1] / 60 + exifGpsLongArray[2] / 3600;
                    double exifGpsLatDouble = exifGpsLatArray[0] + exifGpsLatArray[1] / 60 + exifGpsLatArray[2] / 3600;

                    exifData.location = new GpsLocation(exifGpsLongDouble, exifGpsLatDouble, 0);
                }

                double exifAltitude;
                if (exifData.location != null && exifReader.GetTagValue<double>(ExifTags.GPSAltitude, out exifAltitude))
                {
                    exifData.location.Altitude = exifAltitude;
                }

            }

            return exifData;
        }

        public void Run()
        {
            
            var path = PathUtil.GetPath(_context, _uri);
            if (path == null)
            {
                throw new SystemException("Unable to get file location.");
            }

            var exifData = ReadExifData(path);

            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                byte[] bytes;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    bytes = br.ReadBytes((int) fs.Length);
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
                string jsonCategories = JsonConvert.SerializeObject(_appContext.Settings.Categories);

                PhotoData photodata = new PhotoData
                {
                    Datetime = exifData?.timeTaken ?? DateTime.Now,
                    PhotoFileName = filename,
                    Longitude = exifData?.location?.Longitude ?? 0,
                    Latitude = exifData?.location?.Latitude ?? 0,
                    Altitude = exifData?.location?.Altitude ?? 0,
                    Heading = exifData?.bearing ?? 0,
                    LeftTiltCorrector = 0,
                    RightTiltCorrector = 0,
                    Thumbnail = thumbnail,
                    JsonCategories = jsonCategories,
                    ViewAngleVertical = _appContext.ViewAngleVertical,
                    ViewAngleHorizontal = _appContext.ViewAngleHorizontal,
                    PictureWidth = imgWidth,
                    PictureHeight = imgHeight,
                    MinAltitude = _appContext.Settings.MinAltitute,
                    MaxDistance = _appContext.Settings.MaxDistance,
                    FavouriteFilter = _appContext.ShowFavoritesOnly,
                    ShowElevationProfile = _appContext.Settings.ShowElevationProfile
                };

                Task.Run(async () =>
                {
                    if (GpsUtils.HasLocation(exifData.location))
                    {
                        var placeInfo = await PlaceNameProvider.AsyncGetPlaceName(exifData.location);
                        photodata.Tag = placeInfo.PlaceName + " -> ?";
                    }
                    else
                    {
                        photodata.Tag = "? -> ?";
                    }

                    _appContext.PhotosModel.InsertItem(photodata);
                });
            }
        }
    }
}