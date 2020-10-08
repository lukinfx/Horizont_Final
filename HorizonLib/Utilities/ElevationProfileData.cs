using System;
using System.Collections.Generic;
using System.Linq;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
{
    public class ElevationProfileData
    {
        private List<GpsLocation> displayedPoints = new List<GpsLocation>();

        public string ErrorMessage { get; set; }

        public ElevationProfileData(string errorMessage = null)
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
}