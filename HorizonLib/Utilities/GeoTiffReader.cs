using System;
using BitMiracle.LibTiff.Classic;
using HorizontLib.Domain.Models;

namespace HorizonLib.Utilities
{
    public class GeoTiffReader
    {
        protected static (double, double) GetPixelSize(Tiff tiff)
        {
            FieldValue[] modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);

            byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
            double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
            double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;

            return (pixelSizeX, pixelSizeY);
        }

        protected static GpsLocation GetStartLocation(Tiff tiff)
        {
            FieldValue[] modelTiepointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

            byte[] modelTransformation = modelTiepointTag[1].GetBytes();
            double startLon = BitConverter.ToDouble(modelTransformation, 24);
            double startLat = BitConverter.ToDouble(modelTransformation, 32);

            return new GpsLocation() { Latitude = startLat, Longitude = startLon };
        }

        protected static (int, int) GetImageSize(Tiff tiff)
        {
            int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            return (width, height);
        }
    }

}
