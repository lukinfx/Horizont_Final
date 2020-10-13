using Android.Media;
using HorizontApp.DataAccess;
using HorizontLib.Domain.Models;
//using Java.IO;
using Java.Lang;
using Java.Nio;
using Java.Nio.FileNio;
using System;
using System.IO;
//using System.IO;

namespace HorizontApp.Views.Camera
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public ImageAvailableListener(CameraFragment fragment)
        {
            if (fragment == null)
                throw new System.ArgumentNullException("fragment");
            
            owner = fragment;
        }

        private readonly Java.IO.File file;
        private readonly CameraFragment owner;
        private GpsLocation _location;
        private double _heading;

        //public File File { get; private set; }
        //public CameraFragment Owner { get; private set; }

        public void OnImageAvailable(ImageReader reader)
        {
            owner.mBackgroundHandler.Post(new ImageSaver(reader.AcquireNextImage(), _location, _heading));
        }

        // Saves a JPEG {@link Image} into the specified {@link File}.
        private class ImageSaver : Java.Lang.Object, IRunnable
        {
            private static string SAVED_PICTURES_FOLDER = "HorizonPhotos";

            // The JPEG image
            private Image _Image;
            private GpsLocation _location;
            private double _heading;

            // The file we save the image into.
            //private File mFile;

            public ImageSaver(Image image, GpsLocation location, double heading)
            {
                if (image == null)
                    throw new System.ArgumentNullException("image");
                _Image = image;
                _location = location;
                _heading = heading;
            }

            private static string GetPhotoFileName(DateTime? dt = null)
            {
                if (!dt.HasValue)
                    dt = DateTime.Now;

                string mName = $"{DateTime.Now.Year}_{DateTime.Now.Month}_{DateTime.Now.Day}_{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}_Horizont.jpg";
                return mName;
            }

            private static string GetPhootsFileFolder()
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), SAVED_PICTURES_FOLDER);

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

            private static string GetPhotosFilePath(DateTime? dt = null)
            {
                return System.IO.Path.Combine(GetPhootsFileFolder(), GetPhotoFileName(dt));
            }

            public void Run()
            {
                ByteBuffer buffer = _Image.GetPlanes()[0].Buffer;
                byte[] bytes = new byte[buffer.Remaining()];
                buffer.Get(bytes);

                var file = new Java.IO.File(GetPhotosFilePath());
                

                using (var output = new Java.IO.FileOutputStream(file))
                {
                    try
                    {
                        output.Write(bytes);
                    }
                    catch (Java.IO.IOException e)
                    {
                        e.PrintStackTrace();
                    }
                    finally
                    {
                        _Image.Close();
                        PoiDatabase photoDatabase = new PoiDatabase();
                        photoDatabase.InsertItem(new PhotoData { Datetime = DateTime.Now, PhotoFileName = GetPhotoFileName(), Longitude = _location.Longitude, Latitude = _location.Latitude, Altitude = _location.Altitude, Heading = _heading }); ;
                    }
                }
            }

        }

        public void SetLocation(GpsLocation location)
        {
            _location = location;
        }

        public void SetHeading(double heading)
        {
            _heading = heading;
        }
    }
}