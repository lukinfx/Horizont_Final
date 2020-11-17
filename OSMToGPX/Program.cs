using System;
using System.Collections.Generic;
using System.Xml;

namespace OSMToGPX
{
    class Program
    {
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
                var category = args[2];
                var excludeNoName = argsList.Contains("--exclude-no-name");
                var excludeNoElevation = argsList.Contains("--exclude-no-elevation");


                string file = "d:\\Development\\Horizon\\" + args[0];
                XmlDocument OSMDoc = new XmlDocument();
                OSMDoc.Load(file);
                XmlNodeList nl = OSMDoc.SelectNodes("/osm/node");
                int totalRead = 0;
                int totalWritten = 0;
                int errorNoElevation = 0;
                int errorNoName = 0;

                XmlWriter xmlWriter = XmlWriter.Create(args[1]);
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("gpx", "http://www.topografix.com/GPX/1/1");

                foreach (XmlElement xelement in nl)
                {
                    totalRead++;

                    var lat = xelement.Attributes.GetNamedItem("lat").Value;
                    var lon = xelement.Attributes.GetNamedItem("lon").Value;
                    string name="";
                    string ele="";
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

                    WriteWptElementToFile(lon, lat, name, ele, wikidata, wikipedia, xmlWriter, category, errorNoName);

                    totalWritten++;
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
                WriteStatistics(totalRead, errorNoName, errorNoElevation, totalWritten);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }    
        }

        private static void WriteWptElementToFile(string lon, string lat, string name, string ele, string wikidata, string wikipedia, XmlWriter xmlWriter, string category, int errorNoName)
        {
            xmlWriter.WriteStartElement("wpt");
            xmlWriter.WriteAttributeString("lat", lat);
            xmlWriter.WriteAttributeString("lon", lon);
            if (String.IsNullOrEmpty(name))
            {
                name = "Unnamed " + category + " " + errorNoName;
            }
            xmlWriter.WriteStartElement("name");
            xmlWriter.WriteString(name);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("ele");
            xmlWriter.WriteString(ele);
            xmlWriter.WriteEndElement();

            if (!String.IsNullOrEmpty(wikidata))
            {
                xmlWriter.WriteStartElement("wikidata");
                xmlWriter.WriteString(wikidata);
                xmlWriter.WriteEndElement();
            }
            if (!String.IsNullOrEmpty(wikipedia))
            {
                xmlWriter.WriteStartElement("wikipedia");
                xmlWriter.WriteString(wikipedia);
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }

        private static void WriteStatistics(int totalRead, int errorNoName, int errorNoElevation, int totalWritten)
        {
            Console.WriteLine($"TotalRead: {totalRead}");
            Console.WriteLine($"NoName: {errorNoName}");
            Console.WriteLine($"NoElevation: {errorNoElevation}");
            Console.WriteLine($"TotalWritten: {totalWritten}");
        }
    }
}
