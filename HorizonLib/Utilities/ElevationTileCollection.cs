﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HorizontLib.Domain.Models;
using HorizontLib.Providers;
using HorizontLib.Utilities;

namespace HorizonLib.Utilities
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

            for (var lat = (int)_boundingRectMin.Latitude; lat < ((int)_boundingRectMax.Latitude) + 1; lat++)
            {
                for (var lon = (int)_boundingRectMin.Longitude; lon < ((int)_boundingRectMax.Longitude) + 1; lon++)
                {
                    _elevationTiles.Add( new ElevationTile(new GpsLocation(lon, lat, 0)));
                }
            }
        }

        public IEnumerable<ElevationTile> AsEnumerable()
        {
            return _elevationTiles;
        }

        public int GetSizeToDownload()
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

        public void Download(Action<int> onProgressChange)
        {
            int count = 0;
            foreach (var et in _elevationTiles)
            {
                et.Download();
                onProgressChange(count++);
            }
        }

        /*public void ReadRadius(List<GpsLocation> eleData)
        {
            foreach (var et in _elevationTiles)
            {
                et.ReadRadius(_myLocation, _boundingRectMin, _boundingRectMax, eleData);
            }
        }*/

        public void Read(Action<int> onProgressChange)
        {
            int count = 0;
            foreach (var et in _elevationTiles)
            {
                et.ReadMatrix();
                onProgressChange(count++);
            }

            /*GpsUtils.BoundingRect(_myLocation, _visibility, out var tmpRectMin, out var tmpRectMax);

            for (var lat = (int)tmpRectMin.Latitude; lat < ((int)tmpRectMax.Latitude) + 1; lat++)
            {
                for (var lon = (int)tmpRectMin.Longitude; lon < ((int)tmpRectMax.Longitude) + 1; lon++)
                {
                    _elevationTiles
                        .Find(et => et.HasElevation(new GpsLocation(lon, lat, 0)))
                        .ReadMatrix();
                }
            }*/
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