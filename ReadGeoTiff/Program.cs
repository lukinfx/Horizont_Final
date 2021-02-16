using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace ReadGeoTiff
{
    class Program
    {
        static void ziptiff(string fileName)
        {
            Console.WriteLine(fileName);

            var inputFileName = fileName + ".tif";
            var outputFileName = fileName + ".zip";
            var ed = new ElevationTileConvertor(new GpsLocation(18, 49, 0));
            ed.ReadFromTiff(inputFileName);
            ed.SaveToZip(outputFileName);
        }

        static void Main(string[] args)
        {
            CompressElevationData();

            //var _myLocation = new GpsLocation() {Latitude = 49.4894558, Longitude = 18.4914856}; //830
            var _myLocation = new GpsLocation() {Latitude = 49.5459858, Longitude = 18.4472864}; //1323
            //var _myLocation = new GpsLocation() { Latitude = 49.5153378, Longitude = 18.3473525 }; //711
            //var _myLocation = new GpsLocation() { Latitude = 49.5272403, Longitude = 18.1608797 }; //918
            //var _myLocation = new GpsLocation() { Latitude = 50.0, Longitude = 18.0 }; //288
            //var _myLocation = new GpsLocation() { Latitude = 49.0000001, Longitude = 18.0000001 }; //288
            //var _myLocation = new GpsLocation() { Latitude = 49.0+0.00013, Longitude = 19.0-0.00013 }; //288

            /*var data3 = Peaks360Lib.Utilities.GeoTiffReader.ReadTiff_Skip2(inputFileName, 0, 999, 0, 999);

            
            {
                var y = (1-(_myLocation.Latitude - (int)_myLocation.Latitude)) / (1 / 1799.0);
                var x = (_myLocation.Longitude - (int)_myLocation.Longitude) / (1 / 1799.0);
                var ele11 = data3[(int)y, (int)x];
            }*/
            
            //var ed = new ElevationTile(new GpsLocation(18, 49, 0));
            //ed.LoadFromZip(outputFileName);

            //ed.ReadMatrix();
            //ed.SaveToZip(outputFileName);

            /*var ele = ed.GetElevation(_myLocation);
            bool ok = ed.TryGetElevation(_myLocation, out var ele2);

            foreach (var edi in ed)
            {
                var a = edi.Latitude;
                var b = edi.Longitude;
                var c = edi.Altitude;
            }*/



            /*Console.WriteLine(System.DateTime.Now.ToString("hh:mm:ss.fff"));
            var myLoc = new GpsLocation(18.5, 49.5, 0);
            for (int a = 0; a < 360; a++)
            {
                for (int d = 500; d < 4000; d+=50)
                {
                    var x = GpsUtils.QuickGetGeoLocation(myLoc, d, a);
                    var isOk = ed.TryGetElevation(x, out var elevation);
                }
            }
            Console.WriteLine(System.DateTime.Now.ToString("hh:mm:ss.fff"));*/
        }

        private static void CompressElevationData()
        {
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N048E012_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N048E013_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N048E014_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N048E015_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N048E016_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N048E017_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N048E018_DSM");

            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N049E012_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N049E013_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N049E014_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N049E015_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N049E016_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N049E017_DSM");
            ziptiff( @"c:\temp\ElevationMap\ALPSMLC30_N049E018_DSM");

            ziptiff(@"c:\temp\ElevationMap\ALPSMLC30_N050E012_DSM");
            ziptiff(@"c:\temp\ElevationMap\ALPSMLC30_N050E013_DSM");
            ziptiff(@"c:\temp\ElevationMap\ALPSMLC30_N050E014_DSM");
            ziptiff(@"c:\temp\ElevationMap\ALPSMLC30_N050E015_DSM");
            ziptiff(@"c:\temp\ElevationMap\ALPSMLC30_N050E016_DSM");
            ziptiff(@"c:\temp\ElevationMap\ALPSMLC30_N050E017_DSM");
            ziptiff(@"c:\temp\ElevationMap\ALPSMLC30_N050E018_DSM");



            //ed.LoadFromZip(outputFileName);


            //ziptiff(inputFileName, outputFileName);
            //unziptiff(outputFileName);

        }
    }
}
