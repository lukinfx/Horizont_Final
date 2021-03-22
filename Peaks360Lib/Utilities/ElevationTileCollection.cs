using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Providers;

namespace Peaks360Lib.Utilities
{
    public class ElevationTileCollectionEnumerator : IEnumerator<GpsLocation>
    {
        private IEnumerable<ElevationTile> _tiles;
        private IEnumerator<ElevationTile> _tileEnum;
        private IEnumerator<GpsLocation> _pointEnum;


        public ElevationTileCollectionEnumerator(IEnumerable<ElevationTile> tiles)
        {
            this._tiles = tiles;
        }

        GpsLocation IEnumerator<GpsLocation>.Current => GetCurrent();

        object IEnumerator.Current => GetCurrent();

        public GpsLocation GetCurrent()
        {
            return _pointEnum.Current;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_tileEnum == null)
            {
                _tileEnum = _tiles.GetEnumerator();
                if (!_tileEnum.MoveNext())
                    return false;

                _pointEnum = _tileEnum.Current.GetEnumerator();
                return _pointEnum.MoveNext();
            }

            var isNextPoint=_pointEnum.MoveNext();
            if (isNextPoint)
                return true;

            if (!_tileEnum.MoveNext())
                return false;

            _pointEnum = _tileEnum.Current.GetEnumerator();
            return _pointEnum.MoveNext();
        }

        public void Reset()
        {
            _pointEnum = null;
            _tileEnum = null;
        }
    }

    public class ElevationTileCollection : IEnumerable<GpsLocation>
    {
        private GpsLocation _myLocation;
        private int _visibility;
        private GpsLocation _boundingRectMin, _boundingRectMax;

        private List<ElevationTile> _elevationTiles = new List<ElevationTile>();

        public ElevationTileCollection(GpsLocation myLocation, int visibility)
        {
            _myLocation = myLocation;
            _visibility = visibility;
            GpsUtils.BoundingRect(_myLocation, _visibility, out _boundingRectMin, out _boundingRectMax);

            for (var lat = Math.Floor(_boundingRectMin.Latitude); lat < Math.Floor(_boundingRectMax.Latitude) + 1; lat++)
            {
                for (var lon = Math.Floor(_boundingRectMin.Longitude); lon < Math.Floor(_boundingRectMax.Longitude) + 1; lon++)
                {
                    _elevationTiles.Add( new ElevationTile(new GpsLocation(lon, lat, 0)));
                }
            }
        }

        public ElevationTileCollection(ElevationMap elevationMap)
        {
            foreach  (var item in elevationMap.Tiles)
            {
                _elevationTiles.Add(new ElevationTile(new GpsLocation(item.Longitude, item.Latitude, 0)));
            }
        }

        public IEnumerable<ElevationTile> AsEnumerable()
        {
            return _elevationTiles;
        }

        public long GetSizeToDownload()
        {
            return _elevationTiles.Count(i => !i.Exists()) * ElevationFileProvider.GetFileSize();
        }
        
        public int GetCountToDownload()
        {
            return _elevationTiles.Count(i => !i.Exists());
        }

        public int GetCount()
        {
            return _elevationTiles.Count();
        }

        public bool Download(Action<int> onProgressChange)
        {
            int count = 0;
            bool isOk = true;
            foreach (var et in _elevationTiles)
            {
                if (!et.Exists())
                {
                    if (!et.Download())
                    {
                        isOk = false;
                    }
                    onProgressChange(++count);
                }
            }

            return isOk;
        }

        public long GetSize()
        {
            long totalSize = 0;
            foreach (var et in _elevationTiles)
            {
                totalSize += et.GetEelevationFileSize();
            }
            return totalSize;
        }


        public bool Remove()
        {
            foreach (var et in _elevationTiles)
            {
                et.Remove();
            }

            return true;
        }
        public string GetErrorList()
        {
            string errorsAsString = "";
            foreach (var et in _elevationTiles)
            {
                if (!et.IsOk)
                {
                    if (!string.IsNullOrEmpty(errorsAsString))
                    {
                        errorsAsString += ",";
                    }

                    errorsAsString += $"Tile {et.StartLocation.Latitude:F0}/{et.StartLocation.Longitude:F0} : {et.ErrorMessage}, \r\n";
                }
            }

            if (string.IsNullOrEmpty(errorsAsString))
            {
                return null;
            }

            return $"Some tiles were not loaded. The elevation profile may not be complete. \r\n\r\nMissing tiles: \r\n{errorsAsString}";

            throw new NotImplementedException();
        }

        /*public void ReadRadius(List<GpsLocation> eleData)
        {
            foreach (var et in _elevationTiles)
            {
                et.ReadRadius(_myLocation, _boundingRectMin, _boundingRectMax, eleData);
            }
        }*/

        public bool Read(Action<int> onProgressChange)
        {
            int count = 0;
            bool isOk = true;
            foreach (var et in _elevationTiles)
            {
                if (!et.LoadFromZip())
                {
                    isOk = false;
                }
                onProgressChange(++count);
            }

            return isOk;
        }


        public bool TryGetElevation(GpsLocation location, out double elevation, int size=1)
        {
            if (!HasElevation(location))
            {
                elevation = 0;
                return false;
            }

            elevation = GetElevation(location, size);
            return true;
        }

        public bool HasElevation(GpsLocation location)
        {
            foreach (var et in _elevationTiles)
            {
                if (et.HasElevation(location))
                {
                    return et.IsLoaded();
                }
            }

            return false;
        }

        public double GetElevation(GpsLocation location, int size = 1)
        {
            foreach (var et in _elevationTiles)
            {
                if (et.HasElevation(location))
                    return et.GetElevation(location, size);
            }

            throw new SystemException("No elevation data in this tile collection (out of bounds)");
        }

        public IEnumerator<GpsLocation> GetEnumerator()
        {
            return new ElevationTileCollectionEnumerator(AsEnumerable());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ElevationTileCollectionEnumerator(AsEnumerable());
        }
    }
}
