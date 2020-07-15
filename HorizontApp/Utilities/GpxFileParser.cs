using System;
using System.Collections.Generic;
using System.Xml;
using HorizontApp.Domain.Enums;
using HorizontApp.Domain.Models;

namespace HorizontApp.Utilities
{
    class GpxFileParser
    {
        static public IEnumerable<Poi> Parse(string xml, PoiCategory category, Guid source)
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
                    
                    var nameElement = xelement.GetElementsByTagName("name");
                    var name = nameElement.Count==1 ? nameElement.Item(0).InnerText : "Unnamed";

                    var eleElement = xelement.GetElementsByTagName("ele");
                    var ele = eleElement.Count==1 ? eleElement.Item(0).InnerText : "0";

                    //var name = xelement.InnerText;

                    //TODO: Resolve problem with decimal separator
                    lat = lat.Replace(".", ",");
                    lon = lon.Replace(".", ",");

                    listOfPoi.Add(new Poi
                    {
                        Name = name,
                        Longitude = Convert.ToDouble(lon), 
                        Latitude = Convert.ToDouble(lat),
                        Altitude = Convert.ToDouble(ele),
                        Category = category,
                        Source =  source,
                    });
                }

                return listOfPoi;
            }
            catch(Exception ex)
            {
                throw new Exception("Error when parsing GPX file", ex);
            }
        }
    }
}