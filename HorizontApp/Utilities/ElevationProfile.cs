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
        private List<GpsLocation> displayedPoints = new List<GpsLocation>();

        public string ErrorMessage { get; set; }

        public ElevationProfileData(string errorMessage=null)
        {
            ErrorMessage = errorMessage;
        }

        public void Add(GpsLocation gpsLocation)
        {
            displayedPoints.Add(gpsLocation);
        }

        public void Clear()
        {
            displayedPoints.Clear();
        }

        public List<GpsLocation> GetPoints()
        {
            return displayedPoints;
        }
    }

    class ElevationProfile
    {
        private static readonly int MIN_DISTANCE = 1000;

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

            var elevationDataGrouped = elevationData
                .Where(i => i.Distance > MIN_DISTANCE && i.Distance < visibility * 1000)
                .GroupBy(i => Math.Floor(i.Bearing.Value));


            foreach (var group in elevationDataGrouped)
            {
                progress++;
                onProgressChange(progress);

                var maxPoint = group.OrderByDescending(i => i.VerticalViewAngle).FirstOrDefault();
                
                maxPoint.Distance = 0;//workaround to display all connections 
                _elevationProfileData.Add(maxPoint);
            }
        }

        public void GenerateElevationProfile2(GpsLocation myLocation, double visibility, IEnumerable<GpsLocation> elevationData, Action<int> onProgressChange)
        {
            _elevationProfileData.Clear();

            int progress = 0;

            var elevationDataGrouped = elevationData
                .Where(i => i.Distance > MIN_DISTANCE && i.Distance < visibility * 1000)
                .GroupBy(i => Math.Floor(i.Bearing.Value));

            foreach (var group in elevationDataGrouped)
            {
                progress++;
                onProgressChange(progress);

                var points = group.OrderBy(i => i.Distance);
                List<GpsLocation> temporary = new List<GpsLocation>();
                double maxViewAngle = -90;
                var x1 = points.Count();
                foreach (var point in points)
                {
                    if (point.VerticalViewAngle > maxViewAngle)
                    {
                        temporary.Add(point);
                        maxViewAngle = point.VerticalViewAngle.Value;
                    }
                }
                var x2 = temporary.Count();

                var temporary2 = temporary.OrderByDescending(j => j.Distance);

                var x3 = temporary2.Count();

                GpsLocation lastPoint = null;
                GpsLocation lastAddedPoint = null;
                foreach (var point in temporary2)
                {
                    if (lastPoint == null)
                    {
                        _elevationProfileData.Add(point);
                        lastAddedPoint = point;
                        lastPoint = point;
                        continue;
                    }


                    if (Math.Abs(point.Distance.Value - lastPoint.Distance.Value) < 500)// || lastAddedPoint.Altitude-point.Altitude < 100)
                    {
                        lastPoint = point;
                        continue;
                    }

                    _elevationProfileData.Add(point);
                    lastAddedPoint = point;
                    lastPoint = point;
                }
            }
        }
    }
}