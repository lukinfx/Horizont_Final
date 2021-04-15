using System;
using System.ComponentModel;
using System.IO;
using System.Net;


namespace Peaks360Lib.Providers
{
    public class ElevationFileProvider
    {
        private static string ELEVATION_MAPS_FOLDER = "ElevationMaps";
        private static string ELEVATION_MAPS_URL = "http://peaks360.hys.cz/horizont/ElevationData/";
        private static float ELEVATION_FILE_SIZE = 2.8f;

        public static string GetElevationFile(int lat, int lon)
        {
            var filePath = GetElevationFilePath(lat, lon);

            //Have we already downloaded the file?
            if (File.Exists(filePath))
            {
                return filePath;
            }

            Directory.CreateDirectory(GetElevationFileFolder());

            WebClient webClient = new WebClient();
            webClient.DownloadFile(new Uri(GetElevationFileUrl(lat, lon)), filePath);
            return filePath;
        }

        public static long GetElevationFileSize(int lat, int lon)
        {
            var filePath = GetElevationFilePath(lat, lon);

            //Have we already downloaded the file?
            if (!File.Exists(filePath))
            {
                return 0;
            }

            var fi = new FileInfo(filePath);
            return fi.Length;
        }

        private static string GetElevationFileName(int lat, int lon)
        {
            char latNS = lat >= 0 ? 'N' : 'S';
            lat = Math.Abs(lat);

            char lonEW = lon >= 0 ? 'E' : 'W';
            lon = Math.Abs(lon);

            return $"ALPSMLC30_{latNS}{lat:D3}{lonEW}{lon:D3}_DSM.zip";
        }

        private static string GetElevationFileFolder()
        {
            return Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), ELEVATION_MAPS_FOLDER);
        }

        private static string GetElevationFilePath(int lat, int lon)
        {
            return  Path.Combine(GetElevationFileFolder(), GetElevationFileName(lat,lon));
        }

        private static string GetElevationFileUrl(int lat, int lon)
        {
            return ELEVATION_MAPS_URL + GetElevationFileName(lat, lon);
        }

        public static bool ElevationFileExists(int lat, int lon)
        {
            var filePath = GetElevationFilePath(lat, lon);
            return File.Exists(filePath);
        }

        internal static void Remove(int lat, int lon)
        {
            var filePath = GetElevationFilePath(lat, lon);
            File.Delete(filePath);
        }
        public static float GetFileSize()
        {
            return ELEVATION_FILE_SIZE;
        }

        public static long GetTotalElevationFileSize()
        {
            DirectoryInfo d = new DirectoryInfo(GetElevationFileFolder());
            if (!d.Exists)
            {
                return 0;
            }

            long totalFileSize = 0;
            foreach (var file in d.GetFiles("*_DSM.zip"))
            {
                totalFileSize += file.Length;
            }

            return totalFileSize;
        }

        public static void ClearElevationData()
        {
            DirectoryInfo d = new DirectoryInfo(GetElevationFileFolder());
            if (!d.Exists)
            {
                return;
            }

            foreach (var file in d.GetFiles("*_DSM.zip"))
            {
                file.Delete();
            }
        }
    }
}