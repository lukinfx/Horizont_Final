using HorizontLib.Domain.Models;
using SQLite;
using System;

namespace HorizontLib.Domain.Models
{
    public sealed class PhotoData
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        public string PhotoFileName { get; set; }
        public DateTime Datetime { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }
        public double Heading { get; set; }
        public byte[] Thumbnail { get; set; }
        public string JsonCategories { get; set; }
        public string Tag { get; set; }
        public double ViewAngleHorizontal { get; set; }
        public double ViewAngleVertical { get; set; }
        public double MinAltitude{ get; set; }
        public double MaxDistance { get; set; }
        public bool Favourie { get; set; }

        public PhotoData()
        {

        }
    }
}