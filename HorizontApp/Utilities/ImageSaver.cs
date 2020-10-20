﻿using System;
using Android.OS;
using Android.Media;
using HorizontApp.DataAccess;
using HorizontLib.Domain.Models;
using Java.Lang;
using Java.Nio;

using System.IO;

namespace HorizontApp.Utilities
{
    public class ImageSaver : Java.Lang.Object, IRunnable
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

        public void Run()
        {
            ByteBuffer buffer = _Image.GetPlanes()[0].Buffer;
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            var filename = GetPhotoFileName();
            var filepath = System.IO.Path.Combine(GetPhotosFileFolder(), filename);

            var file = new Java.IO.File(filepath);
            byte[] thumbnail = ImageResizer.ResizeImageAndroid(bytes, 150, 100, 50 );

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
                    photoDatabase.InsertItem(new PhotoData { Datetime = DateTime.Now, PhotoFileName = filename, Longitude = _location.Longitude, Latitude = _location.Latitude, Altitude = _location.Altitude, Heading = _heading, Thumbnail = thumbnail }); ;
                }
            }
        }

    }

}