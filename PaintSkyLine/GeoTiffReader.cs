using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;
using PaintSkyLine;

namespace ElevationData
{
    public class GeoTiffReader
    {
        /*public static ElevationList ReadTiff(string ifn, double filterLatMin, double filterLatMax, double filterLonMin, double filterLonMax)
        {
            var eleData = new List<GeoPoint>();

            using (Tiff tiff = Tiff.Open(ifn, "r"))
            {
                int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                FieldValue[] modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
                FieldValue[] modelTiepointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

                byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
                double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
                double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;

                double dpiX = tiff.GetField(TiffTag.XRESOLUTION)[0].ToDouble();
                double dpiY = tiff.GetField(TiffTag.YRESOLUTION)[0].ToDouble();


                byte[] modelTransformation = modelTiepointTag[1].GetBytes();
                double startLon = BitConverter.ToDouble(modelTransformation, 24);
                double startLat = BitConverter.ToDouble(modelTransformation, 32);


                var scanline = new byte[tiff.ScanlineSize()];

                //TODO: Check if band is stored in 1 byte or 2 bytes. 
                //If 2, the following code would be required
                //var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
                //Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                double endLat = startLat + (pixelSizeY * height);
                double endLon = startLon + (pixelSizeX * width);


                int minAlt = 10000;
                double minLat = 0;
                double minLon = 0;
                int maxAlt = 0;
                double maxLat = 0;
                double maxLon = 0;

                for (int i = 0; i < height; i++)
                {
                    tiff.ReadScanline(scanline, i); //Loading ith Line            

                    var latitude = startLat + (pixelSizeY * i);
                    if (scanline.Length / 2 != width)
                    {
                        throw new SystemException("Invalid data");
                    }

                    if (latitude >= filterLatMin && latitude <= filterLatMax)
                    {
                        for (var j = 0; j < scanline.Length; j += 2)
                        {
                            var longitude = startLon + (pixelSizeX * j / 2);

                            string s = $"{latitude:F6}N, {longitude:F6}E";

                            byte a = scanline[j + 0];
                            byte b = scanline[j + 1];
                            var alt = b * 256 + a;
                            if (longitude >= filterLonMin && longitude <= filterLonMax)
                            {
                                var ep = new ElevationPoint() { latitude = latitude, longitude = longitude, altitude = (UInt32)alt };
                                eleData.List.Add(ep);
                            }

                            if (alt > maxAlt)
                            {
                                maxAlt = alt;
                                maxLat = latitude;
                                maxLon = longitude;
                            }

                            if (alt < minAlt)
                            {
                                minAlt = alt;
                                minLat = latitude;
                                minLon = longitude;
                            }
                            //geodata.Points[0] = new[] { new PointXY(longitude, latitude) };


                            //... process each data item

                            //yield return dataItem;
                        }
                    }
                }

                Console.WriteLine($"Start: {startLat:F6}N, {startLon:F6}E ");
                Console.WriteLine($"End:   {endLat:F6}N, {endLon:F6}E ");
                Console.WriteLine($"MinLat {minLat:F6}N, {minLon:F6}E - {minAlt}");
                Console.WriteLine($"MinLat {maxLat:F6}N, {maxLon:F6}E - {maxAlt}");
            }

            //sw.Close();
            return eleData;
        }
*/
        public static List<GeoPoint> QuickReadTiff(string ifn, GeoPoint myLocation, double filterLatMin, double filterLatMax, double filterLonMin, double filterLonMax)
        {
            var eleData = new List<GeoPoint>();

            using (Tiff tiff = Tiff.Open(ifn, "r"))
            {
                int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                FieldValue[] modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
                FieldValue[] modelTiepointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

                byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
                double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
                double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;

                double dpiX = tiff.GetField(TiffTag.XRESOLUTION)[0].ToDouble();
                double dpiY = tiff.GetField(TiffTag.YRESOLUTION)[0].ToDouble();


                byte[] modelTransformation = modelTiepointTag[1].GetBytes();
                double startLon = BitConverter.ToDouble(modelTransformation, 24);
                double startLat = BitConverter.ToDouble(modelTransformation, 32);


                var scanline = new byte[tiff.ScanlineSize()];

                //TODO: Check if band is stored in 1 byte or 2 bytes. 
                //If 2, the following code would be required
                //var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
                //Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                for (int i = 0; i < height; i++)
                {
                    tiff.ReadScanline(scanline, i); //Loading ith Line            

                    var latitude = startLat + (pixelSizeY * i);
                    if (scanline.Length / 2 != width)
                    {
                        throw new SystemException("Invalid data");
                    }

                    if (latitude >= filterLatMin && latitude <= filterLatMax)
                    {
                        for (var j = 0; j < scanline.Length; j += 2)
                        {
                            var longitude = startLon + (pixelSizeX * j / 2);

                            byte a = scanline[j + 0];
                            byte b = scanline[j + 1];
                            var alt = b * 256 + a;

                            if (longitude >= filterLonMin && longitude <= filterLonMax)
                            {
                                //if ((i % 12 == 0 && j % 6 == 0) || (i % 12 == 6 && j % 6 == 0))

                                if ((i % 6 == 0 && j % 3 == 0) || (i % 6 == 3 && j % 3 == 0))
                                {
                                    var ep = new GeoPoint(latitude, longitude, alt, myLocation);
                                    eleData.Add(ep);
                                }
                            }
                        }
                    }
                }
            }
            return eleData;
        }

        public static ushort[,] ReadTiff2(string ifn, double filterLatMin, double filterLatMax, double filterLonMin, double filterLonMax)
        {
            using (Tiff tiff = Tiff.Open(ifn, "r"))
            {
                int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                FieldValue[] modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
                FieldValue[] modelTiepointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

                byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
                double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
                double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;

                double dpiX = tiff.GetField(TiffTag.XRESOLUTION)[0].ToDouble();
                double dpiY = tiff.GetField(TiffTag.YRESOLUTION)[0].ToDouble();


                byte[] modelTransformation = modelTiepointTag[1].GetBytes();
                double startLon = BitConverter.ToDouble(modelTransformation, 24);
                double startLat = BitConverter.ToDouble(modelTransformation, 32);


                var scanline = new byte[tiff.ScanlineSize()];

                //TODO: Check if band is stored in 1 byte or 2 bytes. 
                //If 2, the following code would be required
                //var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
                //Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                double endLat = startLat + (pixelSizeY * height);
                double endLon = startLon + (pixelSizeX * width);


                int minAlt = 10000;
                double minLat = 0;
                double minLon = 0;
                int maxAlt = 0;
                double maxLat = 0;
                double maxLon = 0;

                var data = new ushort[height, width];
                for (int i = 0; i < height; i++)
                {
                    tiff.ReadScanline(scanline, i); //Loading ith Line            

                    var latitude = startLat + (pixelSizeY * i);
                    if (scanline.Length / 2 != width)
                    {
                        throw new SystemException("Invalid data");
                    }

                    var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
                    Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);
                    Buffer.BlockCopy(scanline, 0, data, i*7200, scanline.Length);
                }
                return data;
            }
        }
    }
}
