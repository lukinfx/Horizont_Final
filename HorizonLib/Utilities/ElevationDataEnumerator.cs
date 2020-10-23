using System.Collections;
using System.Collections.Generic;
using HorizontLib.Domain.Models;

namespace HorizonLib.Utilities
{
    public class ElevationDataEnumerator : IEnumerator<GpsLocation>
    {
        private GpsLocation startLocation;
        private ushort[,] elevationData;
        private int width, height;

        private int? latPos;
        private int? lonPos;

        public ElevationDataEnumerator(GpsLocation startLocation, ref ushort[,] elevationData, int width, int height)
        {
            this.startLocation = startLocation;
            this.elevationData = elevationData;
            this.width = width;
            this.height = height;
        }

        GpsLocation IEnumerator<GpsLocation>.Current => GetCurrent();

        object IEnumerator.Current => GetCurrent();

        public GpsLocation GetCurrent()
        {
            var lat = startLocation.Latitude + latPos.Value / (float)height;
            var lon = startLocation.Longitude + lonPos.Value / (float)width;
            var loc = new GpsLocation(lon, lat, elevationData[latPos.Value, lonPos.Value]);
            return loc;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (latPos >= height)
                return false;

            if (!latPos.HasValue || !lonPos.HasValue)
            {
                latPos = 0;
                lonPos = 0;
                return true;
            }

            lonPos++;
            if (lonPos >= width)
            {
                latPos++;
                lonPos = 0;
            }

            if (latPos >= height)
                return false;

            return true;
        }

        public void Reset()
        {
            latPos = 0;
            lonPos = 0;
        }
    }

}
