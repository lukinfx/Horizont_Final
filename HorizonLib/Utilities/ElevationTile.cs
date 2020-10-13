using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HorizontLib.Domain.Models;
using HorizontLib.Providers;

namespace HorizontLib.Utilities
{
    public class EleDataEnumerator : IEnumerator<GpsLocation>
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

    public class ElevationTile : IEnumerable<GpsLocation>
    {
        private ushort[,] _elevationData;
        private int _width, _height;
        private bool? _fileExists;
        public string ErrorMessage { get; private set; }
        public GpsLocation StartLocation { get; private set; }

        public bool IsOk { get { return string.IsNullOrEmpty(ErrorMessage); } }

        public ElevationTile(GpsLocation startLocation)
        {
            this.StartLocation = startLocation;
        }

        public bool Exists()
        {
            if (!_fileExists.HasValue)
            {
                _fileExists = ElevationFileProvider.ElevationFileExists((int) StartLocation.Latitude, (int) StartLocation.Longitude);
            }
            return _fileExists.Value;
        }

        public bool Download()
        {
            try
            {
                ElevationFileProvider.GetElevationFile((int) StartLocation.Latitude, (int) StartLocation.Longitude);
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Download error ({ex.Message})";
                return false;
            }
        }

        public bool ReadMatrix()
        {
            try
            {
                if (_elevationData == null)
                {
                    if (ElevationFileProvider.ElevationFileExists((int) StartLocation.Latitude, (int) StartLocation.Longitude))
                    {
                        var inputFileName = ElevationFileProvider.GetElevationFile((int) StartLocation.Latitude, (int) StartLocation.Longitude);
                        _elevationData = GeoTiffReader.ReadTiff3(inputFileName, 0, 999, 0, 999);
                        _width = 1800;
                        _height = 1800;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Load error ({ex.Message})";
                return false;
            }
        }

        /*public void ReadRadius(GpsLocation myLocation, GpsLocation min, GpsLocation max, List<GpsLocation> eleData)
        {
            if (_elevationData == null)
            {
                var inputFileName = ElevationFileProvider.GetElevationFile((int)_startLocation.Latitude, (int)_startLocation.Longitude);
                GeoTiffReader.ReadTiff(inputFileName, min, max, myLocation, 1, eleData);
            }
        }*/

        public IEnumerator<GpsLocation> GetEnumerator()
        {
            if (_elevationData == null)
                throw new SystemException("Elevation Tile is now loaded yet");

            return new EleDataEnumerator(StartLocation, ref _elevationData, _width, _height);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_elevationData == null)
                throw new SystemException("Elevation Tile is now loaded yet");

            return new EleDataEnumerator(StartLocation, ref _elevationData, _width, _height);
        }

        public bool TryGetElevation(GpsLocation myLocation, out double elevation)
        {
            if (!HasElevation(myLocation))
            {
                elevation = 0;
                return false;
            }

            elevation = GetElevationInternal(myLocation);
            return true;
        }

        public bool HasElevation(GpsLocation myLocation)
        {
            return (myLocation.Latitude >= StartLocation.Latitude
                    && myLocation.Latitude < StartLocation.Latitude + 1
                    && myLocation.Longitude >= StartLocation.Longitude
                    && myLocation.Longitude < StartLocation.Longitude + 1);
        }

        public bool IsLoaded()
        {
            return _elevationData != null;
        }

        public double GetElevation(GpsLocation myLocation, int size=1)
        {
            if (!HasElevation(myLocation))
            {
                throw new SystemException("No elevation data in this tile (out of bounds)");
            }

            return GetElevationInternal(myLocation, size);
        }

        private double GetElevationInternal(GpsLocation myLocation, int size = 1)
        {
            if (_elevationData == null)
                throw new SystemException("Elevation Tile is now loaded yet");

            var stepX = 1 / (double)(_width-1);
            var stepY = 1 / (double)(_height - 1);

            var py = (myLocation.Latitude - (int)myLocation.Latitude);
            var px = (myLocation.Longitude - (int)myLocation.Longitude);

            var y = (int)(py == 0 ? 0 : (1 - py) / stepY);
            var x = (int)(px / stepX);

            if (size == 1)
            {
                //4 points around
                var ele00 = _elevationData[y, x];
                var ele01 = _elevationData[y + 1, x];
                var ele10 = _elevationData[y, x + 1];
                var ele11 = _elevationData[y + 1, x + 1];

                //0.000278 dg = distance between 2 points

                //px2 and py2 is a distance between 2 x/y points expressed in percent
                var px2 = (px - (x * stepX)) / stepX;
                var py2 = ((py == 0 ? 0 : (1 - py)) - (y * stepY)) / stepY;

                //e1 = average between points x,y and x+1,y
                var e1 = ele00 + px2 * (ele10 - ele00);
                //e2 = average between points x,y+1 and x+1,y+1
                var e2 = ele01 + px2 * (ele11 - ele01);

                //average between e1 and e2
                var ele = e1 + py2 * (e2 - e1);

                return ele;

            }
            else
            {
                double maxEle = 0;
                for (int dx = 0; dx < size; dx++)
                {
                    for (int dy = 0; dy < size; dy++)
                    {
                        double ele;
                        if (y - dy >= 0 && x - dx >= 0)
                        {
                            if (_elevationData[y - dy, x - dx] > maxEle)
                                maxEle = _elevationData[y - dy, x - dx];
                        }

                        if (y + dy < _height && x + dx < _width)
                        {
                            if (_elevationData[y + dy, x + dx] > maxEle)
                                maxEle = _elevationData[y + dy, x + dx]; ;
                        }
                    }
                }

                return maxEle;
            }
        }
    }
}
