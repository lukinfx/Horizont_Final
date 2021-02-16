using System;
using System.IO;

namespace Peaks360App.Utilities
{
    public class ImageSaverUtils
    {
        private static string SAVED_PICTURES_FOLDER = "Peaks360Photos";

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

            if (Directory.Exists(path))
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
            var dcimFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim);
            string photoFolder = System.IO.Path.Combine(dcimFolder.AbsolutePath, SAVED_PICTURES_FOLDER);

            if (Directory.Exists(photoFolder))
            {
                return photoFolder;
            }
            else
            {
                Directory.CreateDirectory(photoFolder);
            }
            return photoFolder;
        }
    }
}
