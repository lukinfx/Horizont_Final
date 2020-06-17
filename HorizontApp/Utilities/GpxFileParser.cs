﻿using System;
using System.Collections.Generic;
using System.Xml;
using HorizontApp.Domain.Enums;
using HorizontApp.Domain.Models;

namespace HorizontApp.Utilities
{
    class GpxFileParser
    {
        static public List<Poi> Parse(string xml, PoiCategory category)
        {
            try
            {
                var listOfPoi = new List<Poi>();

                XmlDocument gpxDoc = new XmlDocument();
                gpxDoc.LoadXml(xml);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(gpxDoc.NameTable);
                nsmgr.AddNamespace("x", "http://www.topografix.com/GPX/1/1");
                XmlNodeList nl = gpxDoc.SelectNodes("/x:gpx/x:wpt ", nsmgr);
                foreach (XmlElement xelement in nl)
                {
                    
                    var lat = xelement.Attributes.GetNamedItem("lat").Value;
                    var lon = xelement.Attributes.GetNamedItem("lon").Value;
                    var name = xelement.InnerText;

                    //TODO: Resolve problem with decimal separator
                    lat = lat.Replace(".", ",");
                    lon = lon.Replace(".", ",");

                    listOfPoi.Add(new Poi
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        GpsLocation = new GpsLocation 
                        { 
                            Longitude = Convert.ToDouble(lon), 
                            Latitude = Convert.ToDouble(lat) 
                        },
                        Category = category,
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