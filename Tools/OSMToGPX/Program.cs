using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;
using HorizontLib.Utilities;
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

        static void FixMissingElevation(PoiList poiList, string tiffDir)
        {
            foreach (var groupLon in poiList.GroupBy(x => (int)x.Longitude))
            {
                //
                foreach (var groupLat in groupLon.GroupBy(x => (int)x.Latitude))
                {
                    //var _data = new List<GpsLocation>();
                    string filePath = $@"{tiffDir}ALPSMLC30_N0{groupLat.Key}E0{groupLon.Key}_DSM.tif";

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

        static void FixMissingElevation(string srcFile, string tiffDir)
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
            var poiList = HorizontLib.Utilities.GpxFileParser.Parse(xmlFileContext, PoiCategory.Castles, new Guid());

            FixMissingElevation(poiList, tiffDir);
            //foreach (var group in poiList.GroupBy(x => ((int)x.Longitude*10000)+(int)x.Latitude))


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
