using System;
using System.Collections.Generic;
using System.Text;
using Peaks360Lib.Domain.Enums;
using SQLite;

namespace Peaks360Lib.Domain.Models
{
    public sealed class DownloadedElevationData
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        public string PlaceName { get; set; }
        public PoiCountry? Country { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }
        public int Distance { get; set; }

        public long SizeInBytes { get; set; }
    }
}
