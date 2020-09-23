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
    public class ElevationProfileData
    {
        private double[] _data = new double[360];

        public void Set(int angle, double viewAngle)
        {
            _data[angle] = viewAngle;
        }

        public void Add(int angle, double viewAngle)
        {
            if (viewAngle > _data[angle])
            {
                _data[angle] = viewAngle;
            }
        }

        public double Get(int angle)
        {
            return _data[angle];
        }
        public void Clear()
        {
            for (int i = 0; i < 360; i++)
            {
                _data[i] = -90;
            }
        }
    }

    class ElevationProfile
    {
        private static readonly int MIN_DISTANCE = 2000;

        private ElevationProfileData _elevationProfileData = new ElevationProfileData();

        public ElevationProfile()
        {
        }

        public ElevationProfileData GetProfile()
        {
            return _elevationProfileData;
        }

        public void GenerateElevationProfile(GpsLocation myLocation, double visibility, IEnumerable<GpsLocation> elevationData, Action<int> onProgressChange)
        {
            _elevationProfileData.Clear();

            int progress = 0;
            foreach (var point in elevationData)
            {
                progress++;
                onProgressChange(progress);

                var loc = new GpsLocation()
                {
                    Latitude = point.Latitude,
                    Longitude = point.Longitude,
                    Altitude = point.Altitude
                };


                var dist = GpsUtils.QuickDistance(myLocation, loc);
                
                if (dist < MIN_DISTANCE || dist > visibility * 1000)
                    continue;

                var bearing = GpsUtils.QuickBearing(myLocation, loc);
                var verticalAngle = GpsUtils.VerticalAngle(loc.Altitude - myLocation.Altitude, dist);

                int dg = ((int)Math.Floor(bearing)+360)%360;

                _elevationProfileData.Add(dg, verticalAngle);
            }
        }
    }
}