using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using HorizontApp.Domain.Models;

namespace HorizontApp.Utilities
{
    class ElevationProfile
    {
        private static readonly int MIN_DISTANCE = 2000;

        double[] _elevationProfile = new double[360];

        public ElevationProfile()
        {
        }

        public double[] GetProfile()
        {
            return _elevationProfile;
        }

        public void Load(GpsLocation myLocation, double visibility)
        {
            var downloadPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            var elevationDataFile = downloadPath.Path + "/ALPSMLC30_N049E018_DSM.tif";

            GpsUtils.BoundingRect(myLocation, visibility, out var min, out var max);

            var elevationData = GeoTiffReader.ReadTiff(elevationDataFile, min, max);

            GenerateElevationProfile(myLocation, visibility, elevationData);
        }

        private void GenerateElevationProfile(GpsLocation myLocation, double visibility, IEnumerable<GpsLocation> elevationData)
        {
            for (int i = 0; i < 360; i++)
            {
                _elevationProfile[i] = -90;
            }

            int progress = 0;
            foreach (var point in elevationData)
            {
                progress++;

                var loc = new GpsLocation()
                {
                    Latitude = point.Latitude,
                    Longitude = point.Longitude,
                    Altitude = point.Altitude
                };


                var dist = GpsUtils.Distance(myLocation, loc);

                if (dist < MIN_DISTANCE || dist > visibility * 1000)
                    continue;

                var bearing = CompassViewUtils.GetBearing(myLocation, loc);
                var verticalAngle = GpsUtils.VerticalAngle(myLocation, loc);

                int dg = ((int)Math.Floor(bearing)+360)%360;

                if (verticalAngle > _elevationProfile[dg])
                {
                    _elevationProfile[dg] = verticalAngle;
                }

            }
        }
    }
}