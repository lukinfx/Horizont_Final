﻿using System;
using System.Collections.Generic;
using System.Linq;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
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
                foreach (var point in points)
                {
                    bool display = true;
                    foreach (var otherPoint in points)
                    {

                        if (point.VerticalViewAngle < otherPoint.VerticalViewAngle)
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

                temporary.OrderByDescending(j => j.Distance);

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
    }
}