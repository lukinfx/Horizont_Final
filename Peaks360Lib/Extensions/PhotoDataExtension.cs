using Peaks360Lib.Domain.Models;

namespace Peaks360Lib.Extensions
{
    public static class PhotoDataExtension
    {
        public static void CopyFrom(this PhotoData dst, PhotoData src)
        {
            dst.PhotoFileName = src.PhotoFileName;
            dst.Datetime = src.Datetime;
            dst.DatetimeTaken = src.DatetimeTaken;
            dst.Longitude = src.Longitude;
            dst.Latitude = src.Latitude;
            dst.Altitude = src.Altitude;
            dst.Heading = src.Heading;
            dst.Thumbnail = src.Thumbnail;
            dst.JsonCategories = src.JsonCategories;
            dst.Tag = src.Tag;
            dst.ViewAngleHorizontal = src.ViewAngleHorizontal;
            dst.ViewAngleVertical = src.ViewAngleVertical;
            dst.PictureWidth = src.PictureWidth;
            dst.PictureHeight = src.PictureHeight;
            dst.MinAltitude = src.MinAltitude;
            dst.MaxDistance = src.MaxDistance;
            dst.RightTiltCorrector = src.RightTiltCorrector;
            dst.LeftTiltCorrector = src.LeftTiltCorrector;
            dst.JsonElevationProfileData = src.JsonElevationProfileData;
            dst.FavouriteFilter = src.FavouriteFilter;
            dst.ShowElevationProfile = src.ShowElevationProfile;
        }
    }
}
