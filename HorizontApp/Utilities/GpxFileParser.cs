using System;
using System.Collections.Generic;
using System.Xml;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;

namespace HorizontApp.Utilities
{
    class GpxFileParser
    {
        static public IEnumerable<Poi> Parse(string xml, PoiCategory category, Guid source)
        {
            var lastNode = "";
            try
            {
                var listOfPoi = new PoiList();

                XmlDocument gpxDoc = new XmlDocument();
                gpxDoc.LoadXml(xml);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(gpxDoc.NameTable);
                nsmgr.AddNamespace("x", "http://www.topografix.com/GPX/1/1");
                nsmgr.AddNamespace("ogr", "http://osgeo.org/gdal");
                XmlNodeList nl = gpxDoc.SelectNodes("/x:gpx/x:wpt ", nsmgr);
                foreach (XmlElement xelement in nl)
                {
                    
                    var lat = xelement.Attributes.GetNamedItem("lat").Value;
                    var lon = xelement.Attributes.GetNamedItem("lon").Value;
                    
                    var nameElement = xelement.GetElementsByTagName("name");
                    var name = nameElement.Count==1 ? nameElement.Item(0).InnerText : "Unnamed";

                    var eleElement = xelement.GetElementsByTagName("ele");
                    var ele = eleElement.Count==1 ? eleElement.Item(0).InnerText : "0";

                    XmlNodeList wikidataElement;
                    string wikidata = "";
                    try
                    {
                        wikidataElement = xelement.GetElementsByTagName("wikidata");
                        if (wikidataElement != null)
                        {
                            wikidata = wikidataElement.Item(0).InnerText;
                        }
                    }
                    catch { }

                    XmlNodeList wikipediaElement;
                    string wikipedia = "";
                    try
                    {
                        wikipediaElement = xelement.GetElementsByTagName("wikipedia");

                        if (wikipediaElement != null)
                        {
                            wikipedia = wikipediaElement.Item(0).InnerText;
                        }
                    }
                    catch { }

                    //var name = xelement.InnerText;

                    //TODO: Resolve problem with decimal separator
                    lat = lat.Replace(".", ",");
                    lon = lon.Replace(".", ",");
                    ele = ele.Replace(".", ",");

                    Poi poi = new Poi
                    {
                        Name = name,
                        Longitude = Convert.ToDouble(lon),
                        Latitude = Convert.ToDouble(lat),
                        Altitude = Convert.ToDouble(ele),
                        Category = category,
                        Source = source
                    };
                    if (wikipedia != "")
                        poi.Wikipedia = wikipedia;
                    if (wikidata != "")
                        poi.Wikidata = wikidata;

                    listOfPoi.Add(poi);

                    lastNode = name;
                }

                return listOfPoi;
            }
            catch(Exception ex)
            {
                throw new Exception($"Error when parsing GPX file (last node:{lastNode})", ex);
            }
        }
    }
}