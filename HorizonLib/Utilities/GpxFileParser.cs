using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using HorizontLib.Domain.Enums;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
{
    public class GpxFileParser
    {
        static public PoiList Parse(string xml, PoiCategory category, Guid source, Action<int> OnStart = null, Action<int> OnProgress = null)
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

                OnStart?.Invoke(nl==null ? 0 : nl.Count);

                int i = 0;
                foreach (XmlElement xelement in nl)
                {
                    OnProgress?.Invoke(i++);

                    var lat = xelement.Attributes.GetNamedItem("lat").Value;
                    var lon = xelement.Attributes.GetNamedItem("lon").Value;

                    var nameElement = xelement["name"];
                    var name = nameElement == null ? "Unnamed" : nameElement.InnerText;

                    var eleElement = xelement["ele"];
                    var ele = eleElement == null ? "0" : eleElement.InnerText;

                    var wikidataElement = xelement["wikidata"];
                    string wikidata = wikidataElement == null ? "" : wikidataElement.InnerText;

                    var wikipediaElement = xelement["wikipedia"];
                    string wikipedia = wikipediaElement == null ? "" : wikipediaElement.InnerText;

                    //TODO: Resolve problem with decimal separator
                    lat = lat.Replace(".", ",");
                    lon = lon.Replace(".", ",");
                    ele = ele.Replace(".", ",");

                    Poi poi = new Poi
                    {
                        Name = name,
                        Longitude = Convert.ToDouble(lon),
                        Latitude = Convert.ToDouble(lat),
                        Altitude = String.IsNullOrEmpty(ele) ? 0 : Convert.ToDouble(ele),
                        Category = category,
                        Source = source
                    };
                    if (!String.IsNullOrEmpty(wikipedia))
                        poi.Wikipedia = wikipedia;
                    if (!String.IsNullOrEmpty(wikidata))
                        poi.Wikidata = wikidata;

                    listOfPoi.Add(poi);

                    lastNode = name;
                }

                return listOfPoi;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when parsing GPX file (last node:{lastNode})", ex);
            }
        }
    }
}