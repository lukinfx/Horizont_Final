using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;

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
        static void Main(string[] args)
        {
            string xmlFileContext = File.ReadAllText(@"c:\Src\Xamarin\Horizon\Data\Gpx\svk-mountains.gpx");
            var poiList = HorizontLib.Utilities.GpxFileParser.Parse(xmlFileContext, PoiCategory.Castles, new Guid());

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

                var ele= doc.CreateElement("ele", gpxNamespace);
                ele.AppendChild(doc.CreateTextNode(poi.Altitude.ToString()));
                wpt.AppendChild(ele);


            }

            doc.Save(@"c:\Src\Xamarin\Horizon\Data\Gpx\svk-mountains2.gpx");
        }
    }
}
