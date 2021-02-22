using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peaks360Lib.Utilities;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Providers;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Peaks360Lib.Utilities
{
    
    public class ElevationTile : IEnumerable<GpsLocation>
    {
        protected ushort[,] _elevationData;
        protected int width, height;
        private bool? _fileExists;
        public string ErrorMessage { get; protected set; }
        public GpsLocation StartLocation { get; private set; }

        public bool IsOk { get { return string.IsNullOrEmpty(ErrorMessage); } }

        public ElevationTile(GpsLocation startLocation)
        {
            var etLat = Math.Floor(startLocation.Latitude);
            var etLon = Math.Floor(startLocation.Longitude);

            this.StartLocation = new GpsLocation(etLon, etLat, 0);
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

        public bool Remove()
        {
            try
            {
                ElevationFileProvider.Remove((int)StartLocation.Latitude, (int)StartLocation.Longitude);
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"File delete error ({ex.Message})";
                return false;
            }
        }
        public IEnumerator<GpsLocation> GetEnumerator()
        {
            if (_elevationData == null)
                throw new SystemException("Elevation Tile is now loaded yet");

            return new ElevationDataEnumerator(StartLocation, ref _elevationData, width, height);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_elevationData == null)
                throw new SystemException("Elevation Tile is now loaded yet");

            return new ElevationDataEnumerator(StartLocation, ref _elevationData, width, height);
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

            var stepX = 1 / (double)(width - 1);
            var stepY = 1 / (double)(height - 1);

            var py = Math.Abs(myLocation.Latitude - (int)myLocation.Latitude); //decimal part of Y coordinate
            var px = Math.Abs(myLocation.Longitude - (int)myLocation.Longitude); //decimal part of X coordinate

            //lower index in Y direction (not solving negative values)
            var y = (int)(py == 0 ? width - 1 : (1 - py) / stepY);

            //lower index in X direction
            var x = (myLocation.Longitude >= 0)
                ? (int)(px / stepX)
                : (int)(px == 0 ? height - 1 : (1 - px) / stepX);

            if (size == 1)
            {
                //4 points around
                var ele00 = _elevationData[y, x];
                var ele01 = _elevationData[y + 1, x];
                var ele10 = _elevationData[y, x + 1];
                var ele11 = _elevationData[y + 1, x + 1];

                //0.000278 dg = distance between 2 points

                //px2 and py2 is a distance between 2 x/y points expressed in percent
                var py2 = ((1 - py) - (y * stepY)) / stepY;

                var px2 = (myLocation.Longitude >= 0)
                    ? (px - (x * stepX)) / stepX
                    :((1 - px) - (x * stepX)) / stepX;

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

                        if (y + dy < height && x + dx < width)
                        {
                            if (_elevationData[y + dy, x + dx] > maxEle)
                                maxEle = _elevationData[y + dy, x + dx]; ;
                        }
                    }
                }

                return maxEle;
            }
        }

        public void SaveToZip(string outputFileName)
        {
            try
            {
                using (var fs = File.Create(outputFileName))
                using (var outStream = new ZipOutputStream(fs))
                {
                    outStream.PutNextEntry(new ZipEntry("ElevationData.bin"));

                    var x = new byte[1800 * 1800 * 2];
                    Buffer.BlockCopy(_elevationData, 0, x, 0, 1800 * 1800 * 2);

                    var buffer = new byte[4096];
                    var ms = new MemoryStream(x);
                    StreamUtils.Copy(ms, outStream, buffer);

                    outStream.CloseEntry();
                }
            }
            catch (Exception ex)
            {
                throw new SystemException("Error while saving elevation data.");
            }
        }

        public bool LoadFromZip()
        {
            try
            {
                if (_elevationData == null)
                {
                    if (ElevationFileProvider.ElevationFileExists((int)StartLocation.Latitude, (int)StartLocation.Longitude))
                    {
                        var inputFileName = ElevationFileProvider.GetElevationFile((int)StartLocation.Latitude, (int)StartLocation.Longitude);
                        LoadFromZip(inputFileName);
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

        public void LoadFromZip(string inputFileName)
        {
            using (Stream fsInput = File.OpenRead(inputFileName))
            using (var zf = new ZipFile(fsInput))
            {
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;
                    }

                    String entryFileName = zipEntry.Name;
                    if (entryFileName != "ElevationData.bin")
                    {
                        throw new SystemException("Invalid file format");
                    }

                    using (var zipStream = zf.GetInputStream(zipEntry))
                    {
                        var x = new byte[3600 * 1800];
                        StreamUtils.ReadFully(zipStream, x);

                        _elevationData = new ushort[1800, 1800];
                        Buffer.BlockCopy(x, 0, _elevationData, 0, 1800 * 1800 * 2);
                    }
                }
            }
            width = 1800;
            height = 1800;
        }
    }
}
