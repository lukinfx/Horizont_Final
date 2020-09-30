using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using BitMiracle.LibTiff.Classic;
using HorizontApp.Domain.Models;
using PaintSkyLine;

namespace HorizontApp.Utilities
{
    public class GeoTiffReader
    {
        private static readonly short VALUE_SIZE = 2;//Bytes

        public static List<GpsLocation> ReadTiff(string filename, GpsLocation filterMin, GpsLocation filterMax, GpsLocation myLocation, int skipFactor)
        {
            try
            {
                var eleData = new List<GpsLocation>();

                using (Tiff tiff = Tiff.Open(filename, "r"))
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

                        if (latitude >= filterMin.Latitude && latitude <= filterMax.Latitude)
                        {
                            for (var j = 0; j < scanline.Length; j += 2)
                            {
                                var longitude = startLon + (pixelSizeX * j / 2);

                                byte a = scanline[j + 0];
                                byte b = scanline[j + 1];
                                var alt = b * 256 + a;

                                if (longitude >= filterMin.Longitude && longitude <= filterMax.Longitude)
                                {
                                    //if ((i % 12 == 0 && j % 6 == 0) || (i % 12 == 6 && j % 6 == 0))

                                    if ((i % 6 == 0 && j % 3 == 0) || (i % 6 == 3 && j % 3 == 0))
                                    {
                                        var ep = new GpsLocation { Altitude = alt, Latitude = latitude, Longitude = longitude };
                                        ep.QuickDistance(myLocation);
                                        ep.QuickBearing(myLocation);
                                        eleData.Add(ep);
                                    }
                                }
                            }
                        }
                    }
                    return eleData;
                }
            }
            catch (Exception ex)
            {
                throw new SystemException("Error while loading elevation data", ex);
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
