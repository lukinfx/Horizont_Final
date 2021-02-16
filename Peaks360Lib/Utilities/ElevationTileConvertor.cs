using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;

namespace Peaks360Lib.Utilities
{
    public class ElevationTileConvertor : ElevationTile
    {
        public ElevationTileConvertor(GpsLocation startLocation) : base(startLocation)
        {
        }

        public bool ReadFromTiff(string fileName)
        {
            try
            {
                _elevationData = GeoTiffReaderArray.ReadTiff_Skip2(fileName, 0, 999, 0, 999);
                width = 1800;
                height = 1800;
                return true;
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
    }
}
