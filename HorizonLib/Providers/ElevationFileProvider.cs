﻿using System;
using System.ComponentModel;
using System.IO;
using System.Net;


namespace HorizontLib.Providers
{
    public class ElevationFileProvider
    {
        private static string ELEVATION_MAPS_FOLDER = "ElevationMaps";
        private static string ELEVATION_MAPS_URL = "http://www.krvaveoleje.cz/horizont/ElevationData/";
        private static int ELEVATION_FILE_SIZE = 3;

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

        private static string GetElevationFileName(int lat, int lon)
        {
            return $"ALPSMLC30_N{lat:D3}E{lon:D3}_DSM.zip";
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

        public static int GetFileSize()
        {
            return ELEVATION_FILE_SIZE;
        }
    }
}