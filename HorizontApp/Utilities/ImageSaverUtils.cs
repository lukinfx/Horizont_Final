using System;
using System.IO;

namespace HorizontApp.Utilities
{
    public class ImageSaverUtils
    {
        private static string SAVED_PICTURES_FOLDER = "HorizonPhotos";

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
    }
}
