using Peaks360Lib.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Peaks360Lib.Providers
{
    class PhohosInformationFileProvider
    {
        private static string PHOTOS_INFORMATION_FILE = "PhotosInformation";
        private static int ELEVATION_FILE_SIZE = 25;

        public static string GetFile()
        {
            try
            {
                var filePath = GetFilePath();

                //Have we already downloaded the file?
                if (File.Exists(filePath))
                {
                    return filePath;
                }
                
                Directory.CreateDirectory(GetFileFolder());
                return filePath;
            }
            catch (Exception ex)
            {
                throw new SystemException("Download error.", ex);
            }
        }

        private static string GetFileName()
        {
            return $"HorizontPhotoInformations.xml";
        }

        private static string GetFileFolder()
        {
            return Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), PHOTOS_INFORMATION_FILE);
        }

        private static string GetFilePath()
        {
            return  Path.Combine(GetFileFolder(), GetFileName());
        }


        public static bool ElevationFileExists()
        {
            var filePath = GetFilePath();
            return File.Exists(filePath);
        }

        public static void WritePhotoDataToXML(double heading, GpsLocation gpsLocation)
        {

        }
    }
}
