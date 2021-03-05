using Peaks360Lib.Domain.Enums;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace OSMToGPX
{
    class Program
    {
        static PoiList ReadFromOsm(string srcFile, bool excludeNoName, bool excludeNoElevation)
        {
            XmlDocument OSMDoc = new XmlDocument();
            OSMDoc.Load(srcFile);
            XmlNodeList nl = OSMDoc.SelectNodes("/osm/node");
            int totalRead = 0;
            int totalWritten = 0;
            int errorNoElevation = 0;
            int errorNoName = 0;

            var poiList = new PoiList();
            foreach (XmlElement xelement in nl)
            {
                totalRead++;

                var lat = xelement.Attributes.GetNamedItem("lat").Value;
                var lon = xelement.Attributes.GetNamedItem("lon").Value;
                string name = "";
                string ele = "";
                string wikipedia = "";
                string wikidata = "";

                XmlNodeList tags = xelement.SelectNodes("tag");
                foreach (XmlElement nameElement in tags)
                {
                    if (nameElement.Attributes.GetNamedItem("k").Value == "name")
                    {
                        name = nameElement.Attributes.GetNamedItem("v").Value;
                    }

                    if (nameElement.Attributes.GetNamedItem("k").Value == "ele")
                    {
                        ele = nameElement.Attributes.GetNamedItem("v").Value;
                    }

                    if (nameElement.Attributes.GetNamedItem("k").Value == "wikidata")
                    {
                        wikidata = nameElement.Attributes.GetNamedItem("v").Value;
                    }

                    if (nameElement.Attributes.GetNamedItem("k").Value == "wikipedia")
                    {
                        wikipedia = nameElement.Attributes.GetNamedItem("v").Value;
                    }


                }

                if (String.IsNullOrEmpty(name))
                {
                    errorNoName++;
                    if (excludeNoName)
                    {
                        continue;
                    }
                }

                if (String.IsNullOrEmpty(ele) && excludeNoElevation)
                {
                    errorNoElevation++;
                    if (excludeNoElevation)
                    {
                        continue;
                    }
                }

                if (ele.EndsWith("m"))
                {
                    ele = ele.Remove(ele.Length - 1, 1);
                }

                try
                {
                    poiList.Add(
                        new Poi()
                        {
                            Latitude = double.Parse(lat, CultureInfo.InvariantCulture),
                            Longitude = double.Parse(lon, CultureInfo.InvariantCulture),
                            Altitude = string.IsNullOrEmpty(ele) ? 0 : double.Parse(ele, CultureInfo.InvariantCulture),
                            Wikidata = wikidata,
                            Wikipedia = wikipedia,
                            Name = name
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:" + ex.Message);
                    Console.WriteLine($"    Name: {name}");
                    Console.WriteLine($"    Latitude: {lat}");
                    Console.WriteLine($"    Longitude: {lon}");
                    Console.WriteLine($"    Altitude: {ele}");
                }
            }
            
            WriteStatistics(totalRead, errorNoName, errorNoElevation);

            return poiList;
        }

        private static void WriteToGpx(PoiList poiList, string outputFile, string category)
        {
            XmlWriter xmlWriter = XmlWriter.Create(outputFile);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("gpx", "http://www.topografix.com/GPX/1/1");

            int errorNoName = 0;
            foreach (var item in poiList)
            {
                WriteWptElementToFile(item, xmlWriter, category, ref errorNoName);
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        public static string GetElevationFileName(int lat, int lon)
        {
            char latNS = lat >= 0 ? 'N' : 'S';
            lat = Math.Abs(lat);

            char lonEW = lon >= 0 ? 'E' : 'W';
            lon = Math.Abs(lon);

            return $"ALPSMLC30_{latNS}{lat:D3}{lonEW}{lon:D3}_DSM.tif";
        }

        static void FixMissingElevation(PoiList poiList, string tiffDir)
        {
            foreach (var groupLon in poiList.GroupBy(x => (int)x.Longitude))
            {
                //
                foreach (var groupLat in groupLon.GroupBy(x => (int)x.Latitude))
                {
                    //var _data = new List<GpsLocation>();
                    var tiffFile = GetElevationFileName(groupLat.Key, groupLon.Key);
                    string filePath = $@"{tiffDir}{tiffFile}";

                    var ed = new ElevationTileConvertor(new GpsLocation(groupLon.Key, groupLat.Key, 0));
                    ed.ReadFromTiff(filePath);

                    {
                        foreach (var poi in groupLat)
                        {
                            var loc = new GpsLocation() { Latitude = poi.Latitude, Longitude = poi.Longitude };
                            if (ed.IsLoaded())
                            {
                                if (poi.Altitude < -0.0000001 || poi.Altitude > 0.0000001)
                                    continue;

                                if (ed.TryGetElevation(loc, out var ele))
                                {
                                    poi.Altitude = ele;
                                }
                            }
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {

            try
            {
                if (args.Length < 3)
                {
                    throw new ArgumentException("Invalid program arguments");
                }

                var argsList = new List<string>(args);

                var inputFileName = args[0];
                var outputFileName = args[1];
                var tiffDir = args[2];
                var category = args[3];
                
                var excludeNoName = argsList.Contains("--exclude-no-name");
                var excludeNoElevation = argsList.Contains("--exclude-no-elevation");
                
                var poiList = ReadFromOsm(inputFileName, excludeNoName, excludeNoElevation);

                FixMissingElevation(poiList, tiffDir);

                WriteToGpx(poiList, outputFileName, category);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void WriteWptElementToFile(Poi item, XmlWriter xmlWriter, string category, ref int errorNoName)
        {
            xmlWriter.WriteStartElement("wpt");
            xmlWriter.WriteAttributeString("lat", $"{item.Latitude:F7}");
            xmlWriter.WriteAttributeString("lon", $"{item.Longitude:F7}");
            xmlWriter.WriteStartElement("name");

            string name;
            if (String.IsNullOrEmpty(item.Name))
            {
                errorNoName++;
                name = "Unnamed " + category + " " + errorNoName;
            }
            else
            {
                name = item.Name;
            }

            xmlWriter.WriteString(name);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("ele");
            xmlWriter.WriteString($"{item.Altitude:F0}");
            xmlWriter.WriteEndElement();


            if (!String.IsNullOrEmpty(item.Wikidata))
            {
                xmlWriter.WriteStartElement("wikidata");
                xmlWriter.WriteString(item.Wikidata);
                xmlWriter.WriteEndElement();
            }
            if (!String.IsNullOrEmpty(item.Wikipedia))
            {
                xmlWriter.WriteStartElement("wikipedia");
                xmlWriter.WriteString(item.Wikipedia);
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }

        private static void WriteStatistics(int totalRead, int errorNoName, int errorNoElevation)
        {
            Console.WriteLine($"TotalRead: {totalRead}");
            Console.WriteLine($"NoName: {errorNoName}");
            Console.WriteLine($"NoElevation: {errorNoElevation}");
        }
    }
}
