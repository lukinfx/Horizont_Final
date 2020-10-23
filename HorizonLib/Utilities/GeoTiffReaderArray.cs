using System;
using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;
using HorizonLib.Utilities;
using HorizontLib.Domain.Models;

namespace HorizontLib.Utilities
{
    public class GeoTiffReaderArray : GeoTiffReader
    {
        private static readonly short VALUE_SIZE = 2;//Bytes

        public static ushort[,] ReadTiffAll(string ifn, double filterLatMin, double filterLatMax, double filterLonMin, double filterLonMax)
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

                var data = new ushort[height, width];
                for (int i = 0; i < height; i++)
                {
                    tiff.ReadScanline(scanline, i);

                    if (scanline.Length / 2 != width)
                    {
                        throw new SystemException("Invalid data");
                    }

                    Buffer.BlockCopy(scanline, 0, data, i * 7200, scanline.Length);
                }
                return data;
            }
        }

        public static ushort[,] ReadTiff_Skip2(string ifn, double filterLatMin, double filterLatMax, double filterLonMin, double filterLonMax)
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

                double endLat = startLocation.Latitude + (pixelSizeY * height);
                double endLon = startLocation.Longitude + (pixelSizeX * width);

                var data = new ushort[height/2, width/2];
                for (int i = 0; i < height; i++)
                {
                    tiff.ReadScanline(scanline, i);

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


    }
}
