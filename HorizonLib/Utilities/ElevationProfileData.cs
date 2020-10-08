using System;
using System.Collections.Generic;
using System.Linq;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
{
    public class ElevationData
    {
        private List<GpsLocation> displayedPoints = new List<GpsLocation>();
        public ushort Angle { get; private set; }

        public ElevationData(ushort angle)
        {
            Angle = angle;
        }

        public void Add(GpsLocation gpsLocation)
        {
            displayedPoints.Add(gpsLocation);
        }

        public List<GpsLocation> GetPoints()
        {
            return displayedPoints;
        }
    }

    public class ElevationProfileData
    {
        private List<ElevationData> elevationData = new List<ElevationData>();
        public string ErrorMessage { get; set; }

        public ElevationProfileData(string errMsg)
        {
            ErrorMessage = errMsg;
        }

        public void Add(ElevationData ed)
        {
            elevationData.Add(ed);
        }

        public void Clear()
        {
            elevationData.Clear();
        }

        public List<ElevationData> GetData()
        {
            return elevationData;
        }

        public ElevationData GetData(int angle)
        {
            return elevationData.Single(i => i.Angle == GpsUtils.Normalize360(angle));
        }
    }
}