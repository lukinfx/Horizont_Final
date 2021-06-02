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
        public double? heading { get; set; }
        public int? focalLength35mm { get; set; }
    }

    public class ExifDataReader
    {
        public static ExifData ReadExifData(string path)
        {
            var exifData = new ExifData();
            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                var exifReader = new ExifReader(fs);
                if (exifReader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized, out DateTime datePictureTaken))
                {
                    exifData.timeTaken = datePictureTaken;
                }

                if (exifReader.GetTagValue<double>(ExifTags.GPSImgDirection, out double exifHeading))
                {
                    exifData.heading = exifHeading;
                }

                if (exifReader.GetTagValue<UInt16>(ExifTags.FocalLengthIn35mmFilm, out UInt16 exifFocalLength35mm))
                {
                    exifData.focalLength35mm = exifFocalLength35mm > 0 ? exifFocalLength35mm : (int?)null;
                }

                if (exifReader.GetTagValue<double[]>(ExifTags.GPSLongitude, out double[] exifGpsLongArray)
                    && exifReader.GetTagValue<double[]>(ExifTags.GPSLatitude, out double[] exifGpsLatArray))
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