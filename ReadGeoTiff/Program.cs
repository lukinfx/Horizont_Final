using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HorizontLib.Domain.Models;
using HorizontLib.Utilities;

namespace ReadGeoTiff
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputFileName = @"c:\temp\ElevationMap\ALPSMLC30_N049E018_DSM.tif";

            //var _myLocation = new GpsLocation() {Latitude = 49.4894558, Longitude = 18.4914856}; //830
            var _myLocation = new GpsLocation() {Latitude = 49.5459858, Longitude = 18.4472864}; //1323
            //var _myLocation = new GpsLocation() { Latitude = 49.5153378, Longitude = 18.3473525 }; //711
            //var _myLocation = new GpsLocation() { Latitude = 49.5272403, Longitude = 18.1608797 }; //918
            //var _myLocation = new GpsLocation() { Latitude = 50.0, Longitude = 18.0 }; //288
            //var _myLocation = new GpsLocation() { Latitude = 49.0000001, Longitude = 18.0000001 }; //288
            //var _myLocation = new GpsLocation() { Latitude = 49.0+0.00013, Longitude = 19.0-0.00013 }; //288

            /*var data3 = HorizontLib.Utilities.GeoTiffReader.ReadTiff3(inputFileName, 0, 999, 0, 999);

            
            {
                var y = (1-(_myLocation.Latitude - (int)_myLocation.Latitude)) / (1 / 1799.0);
                var x = (_myLocation.Longitude - (int)_myLocation.Longitude) / (1 / 1799.0);
                var ele11 = data3[(int)y, (int)x];
            }*/

            var ed = new ElevationTile(new GpsLocation(18, 49, 0));
            ed.ReadMatrix();

            /*var ele = ed.GetElevation(_myLocation);
            bool ok = ed.TryGetElevation(_myLocation, out var ele2);

            foreach (var edi in ed)
            {
                var a = edi.Latitude;
                var b = edi.Longitude;
                var c = edi.Altitude;
            }*/



            Console.WriteLine(System.DateTime.Now.ToString("hh:mm:ss.fff"));
            var myLoc = new GpsLocation(18.5, 49.5, 0);
            for (int a = 0; a < 360; a++)
            {
                for (int d = 500; d < 4000; d+=50)
                {
                    var x = GpsUtils.QuickGetGeoLocation(myLoc, d, a);
                    var isOk = ed.TryGetElevation(x, out var elevation);
                }
            }
            Console.WriteLine(System.DateTime.Now.ToString("hh:mm:ss.fff"));
        }
    }
}
