using System;
using System.IO;
using ExifLib;
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Utilities
{
    public class ExifData
    {
        public GpsLocation location { get; set; }
        public DateTime? timeTaken { get; set; }
        public double? bearing { get; set; }
    }

    public class ExifDataReader
    {
        public static ExifData ReadExifData(string path)
        {
            var exifData = new ExifData();
            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                var exifReader = new ExifReader(fs);
                DateTime datePictureTaken;
                if (exifReader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized, out datePictureTaken))
                {
                    exifData.timeTaken = datePictureTaken;
                }

                double exifBearing;
                if (exifReader.GetTagValue<double>(ExifTags.GPSImgDirection, out exifBearing))
                {
                    exifData.bearing = exifBearing;
                }

                double[] exifGpsLongArray;
                double[] exifGpsLatArray;
                if (exifReader.GetTagValue<double[]>(ExifTags.GPSLongitude, out exifGpsLongArray)
                    && exifReader.GetTagValue<double[]>(ExifTags.GPSLatitude, out exifGpsLatArray))
                {
                    double exifGpsLongDouble = exifGpsLongArray[0] + exifGpsLongArray[1] / 60 + exifGpsLongArray[2] / 3600;
                    double exifGpsLatDouble = exifGpsLatArray[0] + exifGpsLatArray[1] / 60 + exifGpsLatArray[2] / 3600;

                    exifData.location = new GpsLocation(exifGpsLongDouble, exifGpsLatDouble, 0);
                }

                double exifAltitude;
                if (exifData.location != null && exifReader.GetTagValue<double>(ExifTags.GPSAltitude, out exifAltitude))
                {
                    exifData.location.Altitude = exifAltitude;
                }

            }

            return exifData;
        }
    }
}