using System;
using System.Collections.Generic;
using System.Linq;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
{
    public class ElevationProfile
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
                .Where(i => i.QuickDistance(myLocation) > MIN_DISTANCE && i.QuickDistance(myLocation) < visibility * 1000)
                .GroupBy(i => Math.Floor(i.QuickBearing(myLocation)));


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
                .Where(i => i.QuickDistance(myLocation) > MIN_DISTANCE && i.QuickDistance(myLocation) < visibility * 1000)
                .GroupBy(i => Math.Floor(i.QuickBearing(myLocation)));

            foreach (var group in elevationDataGrouped)
            {
                var x = group.Count();
                progress++;
                onProgressChange(progress);

                var points = group.OrderBy(i => i.Distance);
                List<GpsLocation> temporary = new List<GpsLocation>();
                foreach (var point in points)
                {
                    bool display = true;
                    foreach (var otherPoint in points)
                    {

                        if (point.GetVerticalViewAngle(myLocation) < otherPoint.GetVerticalViewAngle(myLocation))
                        {
                            display = false;
                            break;
                        }
                    }
                    if (display || _elevationProfileData.GetPoints().Count == 0)
                    {
                        temporary.Add(point);
                    }
                }

                temporary.Reverse();

                foreach (var point in temporary)
                {

                    bool display = true;
                    foreach (var otherPoint in temporary)
                    {
                        if (point.Altitude < otherPoint.Altitude && Math.Abs(point.Distance.Value - otherPoint.Distance.Value) < 500)
                        {
                            display = false;
                        }
                    }

                    if (display)
                    {
                        _elevationProfileData.Add(point);
                    }
                }
            }
        }

        public void GenerateElevationProfile3(GpsLocation myLocation, double visibility, ElevationProfileData elevationData, Action<int> onProgressChange)
        {
            _elevationProfileData.Clear();

            int progress = 0;

            var elevationDataGrouped = elevationData.GetPoints()
                .Where(i => i.Distance > MIN_DISTANCE && i.Distance < visibility * 1000)
                .GroupBy(i => Math.Floor(i.Bearing.Value));

            foreach (var group in elevationDataGrouped)
            {
                progress++;
                onProgressChange(progress);

                var points = group.OrderBy(i => i.Distance);

                //Select visible points 
                List<GpsLocation> tmpVisiblePoints = new List<GpsLocation>();
                double maxViewAngle = -90;
                foreach (var point in points)
                {
                    if (point.VerticalViewAngle > maxViewAngle)
                    {
                        tmpVisiblePoints.Add(point);
                        maxViewAngle = point.VerticalViewAngle.Value;
                    }
                }

                //Change order (now from the farthest to the nearest) 
                tmpVisiblePoints.Reverse();

                //... and ignore points on descending slope 
                GpsLocation lastPoint = null;
                GpsLocation lastAddedPoint = null;
                foreach (var point in tmpVisiblePoints)
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