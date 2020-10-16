using HorizontLib.Domain.Models;
using System;

namespace HorizontLib.Domain.Models
{
    public sealed class PhotoData
    {
        public string PhotoFileName { get; set; }
        public DateTime Datetime { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }
        public double Heading { get; set; }
        public byte[] Thumbnail { get; set; }

        public PhotoData()
        {

        }
    }
}