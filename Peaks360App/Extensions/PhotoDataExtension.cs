using System;
using Peaks360Lib.Domain.Models;

namespace Peaks360App.Extensions
{
    public static class PhotoDataExtension
    {
        public static DateTime GetPhotoTakenDateTime(this PhotoData photoData)
        {
            return photoData.DatetimeTaken ?? photoData.Datetime;
        }

        public static GpsLocation GetPhotoGpsLocation(this PhotoData photoData)
        {
            return new GpsLocation(photoData.Longitude, photoData.Latitude, photoData.Altitude);
        }


    }
}