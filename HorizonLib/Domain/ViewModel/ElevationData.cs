using System.Collections.Generic;

using HorizontLib.Domain.Models;

namespace HorizontLib.Domain.ViewModel
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
}