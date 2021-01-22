using System;
using System.IO;
using Java.Lang;
using Java.Nio;
using Android.Media;
using HorizontApp.DataAccess;
using HorizontLib.Domain.Models;
using HorizontApp.AppContext;
using Newtonsoft.Json;

namespace HorizontApp.Utilities
{
    public class ImageSaver : Java.Lang.Object, IRunnable
    {
        private static string SAVED_PICTURES_FOLDER = "HorizonPhotos";

        // The JPEG image
        private Image _Image;
        private IAppContext _context;

        // The file we save the image into.
        //private File mFile;

        public ImageSaver(Image image, IAppContext context)
        {
            if (image == null)
                throw new System.ArgumentNullException("image");
            _Image = image;
            _context = context;
        }

        public static string GetPhotoFileName(DateTime? dt = null)
        {
            if (!dt.HasValue)
                dt = DateTime.Now;
            string mName = dt?.ToString("yyyy-MM-dd-hh-mm-ss") + "_Horizont.jpg";
            return mName;
        }

        public static string GetPhotosFileFolder()
        {
            string path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), SAVED_PICTURES_FOLDER);

            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public static string GetPublicPhotosFileFolder()
        {
            string path = System.IO.Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "Horizon");

            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public void Run()
        {
            ByteBuffer buffer = _Image.GetPlanes()[0].Buffer;
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            var filename = GetPhotoFileName();
            var filepath = System.IO.Path.Combine(GetPhotosFileFolder(), filename);

            var file = new Java.IO.File(filepath);
            byte[] thumbnail = ImageResizer.ResizeImageAndroid(bytes, 150, 100, 70 );

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
                        tag += $"{_context.Heading:F0}° direction";
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
                        PictureWidth = _Image.Width,
                        PictureHeight = _Image.Height,
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