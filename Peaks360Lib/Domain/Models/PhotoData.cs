using Peaks360Lib.Domain.Models;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Utilities;
using SQLite;
using System;

namespace Peaks360Lib.Domain.Models
{
    public sealed class PhotoData
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        public string PhotoFileName { get; set; }
        public DateTime Datetime { get; set; } //DatetimeSaved
        public DateTime? DatetimeTaken { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }
        public double? Heading { get; set; }
        public byte[] Thumbnail { get; set; }
        public string JsonCategories { get; set; }
        public string Tag { get; set; }
        public double ViewAngleHorizontal { get; set; }
        public double ViewAngleVertical { get; set; }
        public int PictureWidth { get; set; }
        public int PictureHeight { get; set; }
        public double MinAltitude{ get; set; }
        public double MaxDistance { get; set; }
        public double? RightTiltCorrector { get; set; }
        public double? LeftTiltCorrector { get; set; }
        public string JsonElevationProfileData { get; set; }
        public bool FavouriteFilter { get; set; }
        public bool ShowElevationProfile { get; set; }

        private bool _favourite = false;

        public bool Favourite 
        { 
            get
            {
                return _favourite;
            }
            set
            {
                _favourite = value;
            } 
        }

        public PhotoData()
        {

        }
    }
}