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

namespace HorizontApp.Utilities
{
    public class GeoTiffReader
    {
        private static readonly short VALUE_SIZE = 2;//Bytes
        private static readonly short SKIP_FACTOR = 3;

        public static List<GpsLocation> ReadTiff(string filename, GpsLocation filterMin, GpsLocation filterMax)
        {
            try
            {
                var eleData = new List<GpsLocation>();

                using (Tiff tiff = Tiff.Open(filename, "r"))
                {
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
                            throw new SystemException("Invalid data");
                        }

                        if (latitude >= filterMin.Latitude && latitude <= filterMax.Latitude)
                        {
                            for (var j = 0; j < scanline.Length/VALUE_SIZE; j++)
                            {
                                var longitude = startLocation.Longitude + (pixelSizeX * j);

                                byte loByte = scanline[j * VALUE_SIZE + 0];
                                byte hiByte = scanline[j * VALUE_SIZE + 1];
                                var alt = hiByte * 256 + loByte;

                                if (longitude >= filterMin.Longitude && longitude <= filterMax.Longitude)
                                {
                                    if (i % SKIP_FACTOR == 0 && j % SKIP_FACTOR == 0)
                                    {
                                        var ep = new GpsLocation() {Latitude = latitude, Longitude = longitude, Altitude = (UInt32) alt};
                                        eleData.Add(ep);
                                    }
                                }
                            }
                        }
                    }
                }

                return eleData;
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
