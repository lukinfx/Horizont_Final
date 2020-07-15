using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
{
    public class GpxFileParser
    {
        private static string HMS2Deg(string text)
        {
            if (text.Length != 8)
            {
                return "0";
            }

            var h = Int32.Parse(text.Substring(0, 2));
            var m = Int32.Parse(text.Substring(3, 2));
            var s = Int32.Parse(text.Substring(6, 2));

            var hms = (((( h * 60) + m) * 60) + s) / ((double)60 * 60);
            return hms.ToString();
        }


        static public IEnumerable<Poi> Parse(string xml, PoiCategory category)
        {
            try
            {
                var listOfPoi = new PoiList();

                XmlDocument gpxDoc = new XmlDocument();
                gpxDoc.LoadXml(xml);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(gpxDoc.NameTable);
                nsmgr.AddNamespace("x", "http://www.topografix.com/GPX/1/1");
                XmlNodeList nl = gpxDoc.SelectNodes("/x:gpx/x:wpt ", nsmgr);
                foreach (XmlElement xelement in nl)
                {

                    var lat = xelement.Attributes.GetNamedItem("lat").Value;
                    var lon = xelement.Attributes.GetNamedItem("lon").Value;
                    
                    var nameList = xelement.GetElementsByTagName("name");
                    var name = nameList.Count > 0 ? nameList.Item(0).InnerText : "XXXXXXXXXXXXXXXXXXXXXXXX";
                    
                    var eleList = xelement.GetElementsByTagName("ele");
                    var ele = eleList.Count > 0 ? eleList.Item(0).InnerText : "0";

                    //lon = HMS2Deg(lon);
                    //lat = HMS2Deg(lat);

                    //TODO: Resolve problem with decimal separator
                    lat = lat.Replace(".", ",");
                    lon = lon.Replace(".", ",");

                    listOfPoi.Add(new Poi
                    {
                        Name = name,
                        Longitude = Convert.ToDouble(lon),
                        Latitude = Convert.ToDouble(lat),
                        Category = category,
                        Altitude = Convert.ToDouble(ele),
                    });
                }

                return listOfPoi;
            }
            catch (Exception ex)
            {
                throw new Exception("Error when parsing GPX file", ex);
            }
        }
    }
}
