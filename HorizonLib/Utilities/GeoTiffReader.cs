using System;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
{
    public class GeoTiffReader
    {
        private static readonly short VALUE_SIZE = 2;//Bytes

        public static void ReadTiff(string filename, GpsLocation filterMin, GpsLocation filterMax, GpsLocation myLocation, int skipFactor, List<GpsLocation> eleData)
        {
            try
            {
                using (Tiff tiff = Tiff.Open(filename, "r"))
                {
                    if (tiff == null)
                    {
                        throw new SystemException("File open error.");
                    }

                    var (width, height) = GetImageSize(tiff);

                    var (pixelSizeX, pixelSizeY) = GetPixelSize(tiff);
                    var startLocation = GetStartLocation(tiff);

                    var scanline = new byte[tiff.ScanlineSize()];
                    for (int i = 0; i < height; i++)
                    {
                        tiff.ReadScanline(scanline, i);

                        var latitude = startLocation.Latitude + (pixelSizeY * i);
                        if (scanline.Length / 2 != width)
                        {
                            throw new SystemException("Invalid GeoTiff data");
                        }

                        if (latitude >= filterMin.Latitude && latitude <= filterMax.Latitude)
                        {
                            for (var j = 0; j < scanline.Length / VALUE_SIZE; j++)
                            {
                                var longitude = startLocation.Longitude + (pixelSizeX * j);

                                byte loByte = scanline[j * VALUE_SIZE + 0];
                                byte hiByte = scanline[j * VALUE_SIZE + 1];
                                var alt = hiByte * 256 + loByte;

                                if (longitude >= filterMin.Longitude && longitude <= filterMax.Longitude)
                                {
                                    if (i % skipFactor == 0 && j % skipFactor == 0)
                                    {
                                        var ep = new GpsLocation { Altitude = alt, Latitude = latitude, Longitude = longitude };
                                        ep.QuickDistance(myLocation);
                                        ep.QuickBearing(myLocation);
                                        ep.GetVerticalViewAngle(myLocation);
                                        eleData.Add(ep);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SystemException("GeoTiff parsing error", ex);
            }
        }

        public static ushort[,] ReadTiff2(string ifn, double filterLatMin, double filterLatMax, double filterLonMin, double filterLonMax)
        {
            using (Tiff tiff = Tiff.Open(ifn, "r"))
            {
                if (tiff == null)
                {
                    throw new SystemException("File open error.");
                }

                var (width, height) = GetImageSize(tiff);

                var (pixelSizeX, pixelSizeY) = GetPixelSize(tiff);
                var startLocation = GetStartLocation(tiff);

                var scanline = new byte[tiff.ScanlineSize()];

                //TODO: Check if band is stored in 1 byte or 2 bytes. 
                //If 2, the following code would be required
                //var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
                //Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                double endLat = startLocation.Latitude + (pixelSizeY * height);
                double endLon = startLocation.Longitude + (pixelSizeX * width);


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

                    var latitude = startLocation.Latitude + (pixelSizeY * i);
                    if (scanline.Length / 2 != width)
                    {
                        throw new SystemException("Invalid data");
                    }

                    var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
                    Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);
                    Buffer.BlockCopy(scanline, 0, data, i * 7200, scanline.Length);
                }
                return data;
            }
        }

        public static ushort[,] ReadTiff3(string ifn, double filterLatMin, double filterLatMax, double filterLonMin, double filterLonMax)
        {
            using (Tiff tiff = Tiff.Open(ifn, "r"))
            {
                if (tiff == null)
                {
                    throw new SystemException("File open error.");
                }

                var (width, height) = GetImageSize(tiff);

                var (pixelSizeX, pixelSizeY) = GetPixelSize(tiff);
                var startLocation = GetStartLocation(tiff);

                var scanline = new byte[tiff.ScanlineSize()];

                //TODO: Check if band is stored in 1 byte or 2 bytes. 
                //If 2, the following code would be required
                //var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
                //Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                double endLat = startLocation.Latitude + (pixelSizeY * height);
                double endLon = startLocation.Longitude + (pixelSizeX * width);


                int minAlt = 10000;
                double minLat = 0;
                double minLon = 0;
                int maxAlt = 0;
                double maxLat = 0;
                double maxLon = 0;

                var data = new ushort[height/2, width/2];
                for (int i = 0; i < height; i++)
                {
                    tiff.ReadScanline(scanline, i); //Loading ith Line            

                    if (i % 2 == 1)
                        continue;

                    var latitude = startLocation.Latitude + (pixelSizeY * i);
                    if (scanline.Length / 2 != width)
                    {
                        throw new SystemException("Invalid data");
                    }

                    int scanline16BitLength = tiff.ScanlineSize() / 2;
                    var scanline16Bit = new ushort[scanline16BitLength / 2];

                    for (int j = 0; j < scanline16BitLength; j++)
                    {
                        if (j % 2 == 1)
                            continue;

                        Buffer.BlockCopy(scanline, j*2, scanline16Bit, j, 2);
                    }
                    Buffer.BlockCopy(scanline16Bit, 0, data, i * 1800, scanline16BitLength);
                }
                return data;
            }
        }

        private static (double, double) GetPixelSize(Tiff tiff)
        {
            FieldValue[] modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);

            byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
            double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
            double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;

            return (pixelSizeX, pixelSizeY);
        }

        private static GpsLocation GetStartLocation(Tiff tiff)
        {
            FieldValue[] modelTiepointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

            byte[] modelTransformation = modelTiepointTag[1].GetBytes();
            double startLon = BitConverter.ToDouble(modelTransformation, 24);
            double startLat = BitConverter.ToDouble(modelTransformation, 32);

            return new GpsLocation() {Latitude = startLat, Longitude = startLon};
        }

        private static (int, int) GetImageSize(Tiff tiff)
        {
            int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            return (width, height);
        }

    }
}
