using System.Collections.Generic;

using Peaks360Lib.Domain.Models;

namespace Peaks360Lib.Domain.ViewModel
{
    public class ElevationData
    {
        public List<GpsLocation> displayedPoints = new List<GpsLocation>();
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
}