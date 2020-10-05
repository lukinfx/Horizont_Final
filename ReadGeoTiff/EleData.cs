using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HorizontLib.Domain.Models;

namespace ReadGeoTiff
{
    class EleDataEnumerator : IEnumerator<GpsLocation>
    {
        private GpsLocation startLocation;
        private ushort[,] elevationData;
        private int width, height;

        private int? latPos;
        private int? lonPos;

        public EleDataEnumerator(GpsLocation startLocation, ref ushort[,] elevationData, int width, int height)
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
            var lat = startLocation.Latitude + latPos.Value/(float)height;
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

    class EleData : IEnumerable<GpsLocation>
    {
        private GpsLocation startLocation;
        private ushort[,] elevationData;
        private int width, height;

        public EleData(GpsLocation startLocation, ref ushort[,] elevationData, int width, int height)
        {
            this.startLocation = startLocation;
            this.elevationData = elevationData;
            this.width = width;
            this.height = height;
        }

        public IEnumerator<GpsLocation> GetEnumerator()
        {
            return new EleDataEnumerator(startLocation, ref elevationData, width, height);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EleDataEnumerator(startLocation, ref elevationData, width, height);
        }

        public bool TryGetElevation(GpsLocation _myLocation, out double elevation)
        {
            if (!HasElevation(_myLocation))
            {
                elevation = 0;
                return false;
            }

            elevation = GetElevation(_myLocation);
            return true;
        }

        public bool HasElevation(GpsLocation _myLocation)
        {
            return (_myLocation.Latitude >= startLocation.Latitude
                    && _myLocation.Latitude < startLocation.Latitude + 1
                    && _myLocation.Longitude >= startLocation.Longitude
                    && _myLocation.Longitude < startLocation.Longitude + 1);
        }

        public double GetElevation(GpsLocation _myLocation)
        {
            if (!HasElevation(_myLocation))
            {
                throw new SystemException("No elevation data in this tile (out of bounds)");
            }

            var step = 1 / 3599.0;

            var py = (_myLocation.Latitude - (int)_myLocation.Latitude);
            var px = (_myLocation.Longitude - (int)_myLocation.Longitude);

            var y = (int)(py == 0 ? 0 : (1 - py) / step);
            var x = (int)(px / step);

            //4 points around
            var ele00 = elevationData[y, x];
            var ele01 = elevationData[y + 1, x];
            var ele10 = elevationData[y, x + 1];
            var ele11 = elevationData[y + 1, x + 1];

            //0.000278 dg = distance between 2 points

            //px2 and py2 is a distance between 2 x/y points expressed in percent
            var px2 = (px - (x * step)) / step;
            var py2 = ((py == 0 ? 0 : (1 - py)) - (y * step)) / step;

            //e1 = average between points x,y and x+1,y
            var e1 = ele00 + px2 * (ele10 - ele00);
            //e2 = average between points x,y+1 and x+1,y+1
            var e2 = ele01 + px2 * (ele11 - ele01);

            //average between e1 and e2
            var ele = e1 + py2 * (e2 - e1);

            return ele;
        }
    }
}
