using System;
using System.IO;
using Java.Lang;
using Java.Nio;
using Android.Media;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.Models;
using Peaks360App.AppContext;
using Newtonsoft.Json;

namespace Peaks360App.Utilities
{
    public class ImageSaver : Java.Lang.Object, IRunnable
    {
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

            var thumbWidth = _context.IsPortrait ? 100 : 150;
            var thumbHeight = _context.IsPortrait ? 150 : 100;

            ByteBuffer buffer = _Image.GetPlanes()[0].Buffer;
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            var filename = ImageSaverUtils.GetPhotoFileName();
            var filepath = System.IO.Path.Combine(ImageSaverUtils.GetPhotosFileFolder(), filename);

            var file = new Java.IO.File(filepath);
            byte[] thumbnail = ImageResizer.ResizeImageAndroid(bytes, thumbWidth, thumbHeight, 70 );

            using (var output = new Java.IO.FileOutputStream(file))
            {
                try
                {
                    output.Write(bytes);
                    
                    PoiDatabase poiDatabase = new PoiDatabase();
                    string jsonCategories = JsonConvert.SerializeObject(_context.Settings.Categories);

                    var tag = _context.MyLocationName + " -> ";
                    if (_context.SelectedPoi != null)
                    {
                        tag += _context.SelectedPoi.Poi.Name;
                    }
                    else
                    {
                        tag += Android.App.Application.Context.Resources.GetText(Resource.String.Heading);
                        tag += $" {GpsUtils.Normalize360((_context.Heading)):F0}°";
                    }

                    PhotoData photodata = new PhotoData
                    {
                        Tag = tag,
                        Datetime = DateTime.Now,
                        PhotoFileName = filename,
                        Longitude = _context.MyLocation.Longitude,
                        Latitude = _context.MyLocation.Latitude,
                        Altitude = _context.MyLocation.Altitude,
                        Heading = _context.Heading + _context.HeadingCorrector,
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
    }
}