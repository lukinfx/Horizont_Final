using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Peaks360Lib.Utilities;
using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;

namespace GpxAltitudeFixer
{
    class Program
    {
        static void SplitNameAndAltitude(Poi poi)
        {
            string regexpPattern = "(.*)\\(([0 - 9]{ 1,4}) m\\.n\\.m\\.\\)(.*)";
            string regexpPattern2 = "(.*)\\(([0-9]{1,4}).*\\).*";

            Regex regex = new Regex(regexpPattern2);
            var matches = regex.Matches(poi.Name);
            if (matches.Count == 1)
            {
                var match = matches[0];
                if (match.Groups.Count == 3)
                {
                    if (Int32.TryParse(match.Groups[2].Value, out var alt))
                    {
                        poi.Name = match.Groups[1].Value;
                        poi.Altitude = alt;
                    }
                    else
                    {
                        Console.WriteLine($"Wrong altitude - {poi.Name}");
                    }
                }
                else
                {
                    Console.WriteLine($"Wrong name - {poi.Name}");
                }
            }
            else
            {
                Console.WriteLine($"No match - {poi.Name}");
            }
        }

        static void FixElevationInGpxFromPoiCz()
        {
            string xmlFileContext = File.ReadAllText(@"c:\Src\Xamarin\Horizon\Data\Gpx\svk-mountains.gpx");
            var poiList = Peaks360Lib.Utilities.GpxFileParser.Parse(xmlFileContext, PoiCategory.Castles, new Guid());

            /*foreach (var poi in poiList)
            {
                SplitNameAndAltitude(poi);
            }*/

            var gpxNamespace = "http://www.topografix.com/GPX/1/1";
            var doc = new XmlDocument();
            XmlElement xmlRoot = doc.CreateElement("gpx", gpxNamespace);
            doc.CreateElement("Payload");
            doc.AppendChild(xmlRoot);
            foreach (var poi in poiList)
            {
                var wpt = doc.CreateElement("wpt", gpxNamespace);
                /*< wpt lat = "49.894517" lon = "15.778636" >
   
                < name > Zabity kopec(662 m.n.m.) </ name >
      
                </ wpt >*/

                wpt.SetAttribute("lat", poi.Latitude.ToString());
                wpt.SetAttribute("lon", poi.Longitude.ToString());
                xmlRoot.AppendChild(wpt);

                var name = doc.CreateElement("name", gpxNamespace);
                name.AppendChild(doc.CreateTextNode(poi.Name.Trim()));
                wpt.AppendChild(name);

                var ele = doc.CreateElement("ele", gpxNamespace);
                ele.AppendChild(doc.CreateTextNode(poi.Altitude.ToString()));
                wpt.AppendChild(ele);


            }

            doc.Save(@"c:\Src\Xamarin\Horizon\Data\Gpx\svk-mountains2.gpx");
        }

        static void FixMissingElevation(string srcFile)
        {
            /*var etc = new ElevationTileCollection(_myLocation, (int)_visibility);
            var d = etc.GetSizeToDownload();
            etc.Download(progress => { });
            etc.Read(progress => { });
            profileGenerator.Generate(_myLocation, etc, progress => { });*/

            var dstFile = srcFile.ToLower().Replace(".gpx", "_fe.gpx");
            Console.WriteLine($"Source File: {srcFile}");
            Console.WriteLine($"Destination File: {dstFile}");

            string xmlFileContext = File.ReadAllText(srcFile);
            var poiList = Peaks360Lib.Utilities.GpxFileParser.Parse(xmlFileContext, PoiCategory.Castles, new Guid());

            //foreach (var group in poiList.GroupBy(x => ((int)x.Longitude*10000)+(int)x.Latitude))
            foreach (var groupLon in poiList.GroupBy(x => (int)x.Longitude))
            {
                //
                foreach (var groupLat in groupLon.GroupBy(x => (int)x.Latitude))
                {
                    //var _data = new List<GpsLocation>();
                    string filePath = $@"c:\Temp\ElevationMap\TIFF\ALPSMLC30_N0{groupLat.Key}E0{groupLon.Key}_DSM.tif";

                    var ed = new ElevationTileConvertor(new GpsLocation(groupLon.Key, groupLat.Key, 0));
                    ed.ReadFromTiff(filePath);

                    {
                        foreach (var poi in groupLat)
                        {
                            var loc = new GpsLocation() {Latitude = poi.Latitude, Longitude = poi.Longitude};
                            if (ed.IsLoaded())
                            {
                                if (ed.TryGetElevation(loc, out var ele))
                                {
                                    poi.Altitude = ele;
                                }
                            }
                        }
                    }

                }
                
            }

            /*foreach (var poi in poiList)
            {
                SplitNameAndAltitude(poi);
            }*/

            var gpxNamespace = "http://www.topografix.com/GPX/1/1";
            var doc = new XmlDocument();
            XmlElement xmlRoot = doc.CreateElement("gpx", gpxNamespace);
            doc.CreateElement("Payload");
            doc.AppendChild(xmlRoot);
            foreach (var poi in poiList)
            {
                var wpt = doc.CreateElement("wpt", gpxNamespace);
                /*< wpt lat = "49.894517" lon = "15.778636" >
   
                < name > Zabity kopec(662 m.n.m.) </ name >
      
                </ wpt >*/

                wpt.SetAttribute("lat", poi.Latitude.ToString());
                wpt.SetAttribute("lon", poi.Longitude.ToString());
                xmlRoot.AppendChild(wpt);

                var name = doc.CreateElement("name", gpxNamespace);
                name.AppendChild(doc.CreateTextNode(poi.Name.Trim()));
                wpt.AppendChild(name);

                var ele = doc.CreateElement("ele", gpxNamespace);
                ele.AppendChild(doc.CreateTextNode($"{poi.Altitude:F0}"));
                wpt.AppendChild(ele);

                if (!string.IsNullOrEmpty(poi.Wikidata))
                {
                    var wikidata = doc.CreateElement("wikidata", gpxNamespace);
                    wikidata.AppendChild(doc.CreateTextNode($"{poi.Wikidata}"));
                    wpt.AppendChild(wikidata);
                }

                if (!string.IsNullOrEmpty(poi.Wikipedia))
                {
                    var wikipedia = doc.CreateElement("wikipedia", gpxNamespace);
                    wikipedia.AppendChild(doc.CreateTextNode($"{poi.Wikipedia}"));
                    wpt.AppendChild(wikipedia);
                }
            }

            doc.Save(dstFile);
        }
        static void Main(string[] args)
        {
            FixMissingElevation(args[0]); 
            //FixMissingElevation("cze-churches");
            //FixMissingElevation("cze-lakes");
            //FixMissingElevation("cze-palaces");
            //FixMissingElevation("cze-ruins");
            //FixMissingElevation("cze-castles");
            
            //FixElevationInGpxFromPoiCz();

        }
    }
}
